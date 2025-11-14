using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

public class OpenAIAvatarDirector : MonoBehaviour
{

    private string apiKey;
    private const string chatEndpoint = "https://api.openai.com/v1/chat/completions";
    private const string ttsEndpoint = "https://api.openai.com/v1/audio/speech";

    void Awake()
    {
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    /// <summary>
    /// Llama al agente (modelo de texto) y luego convierte su respuesta a audio.
    /// </summary>
    /// <param name="altura">Altura de la torre (0-8)</param>
    /// <param name="turno">"avatar" o "usuario"</param>
    /// <param name="usuario">"TEA", "DOWN", "AC"</param>
    /// <param name="resultado">"OUT_OF_TIME", "ERROR_TURN", "CORRECT", "WAIT"</param>
    /// <param name="onDone">Callback con (textoRespuesta, audioClip)</param>
    public void GenerarRespuestaYAudio(
        int altura,
        string turno,
        string usuario,
        string resultado,
        Action<string, AudioClip> onDone)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key vacía. Configúrala en el inspector.");
            onDone?.Invoke(null, null);
            return;
        }

        StartCoroutine(AgentThenTTSCoroutine(altura, turno, usuario, resultado, onDone));
    }

    private IEnumerator AgentThenTTSCoroutine(
        int altura,
        string turno,
        string usuario,
        string resultado,
        Action<string, AudioClip> onDone)
    {
        // 1) Llamamos al modelo de texto (agente)
        yield return SendAgentRequest(altura, turno, usuario, resultado, (agentText) =>
        {
            if (string.IsNullOrEmpty(agentText))
            {
                onDone?.Invoke(null, null);
                return;
            }

            // 2) Con el texto del agente, llamamos a TTS
            StartCoroutine(TextToSpeech(agentText, (clip) =>
            {
                onDone?.Invoke(agentText, clip);
            }));
        });
    }

    /// <summary>
    /// Llamada al modelo de texto (gpt-4o-mini) con el prompt de agente.
    /// </summary>
    private IEnumerator SendAgentRequest(
        int altura,
        string turno,
        string usuario,
        string resultado,
        Action<string> onAgentText)
    {
        // 💡 Prompt modelo: el sistema es el director de experimento
        string systemPrompt =
            "Eres un avatar que dirige un experimento donde se construye una torre de cubos entre un avatar y un niño o niña. " +
            "El experimento tiene estas reglas:\n" +
            "1) Solo se puede colocar un cubo en el turno correspondiente (avatar o usuario).\n" +
            "2) La altura de la torre va de 0 a 8 cubos.\n" +
            "3) El niño puede pertenecer a uno de estos grupos: TEA (trastornos del espectro autista), " +
            "DOWN (síndrome de Down) o AC (altas capacidades).\n\n" +
            "Recibes como parámetros de entrada:\n" +
            "- turno_actual: 'avatar' o 'usuario'\n" +
            "- altura: número entero entre 0 y 8\n" +
            "- tipo_usuario: 'TEA', 'DOWN' o 'AC'\n" +
            "- resultado: uno de 'OUT_OF_TIME', 'ERROR_TURN', 'CORRECT', 'WAIT'\n\n" +
            "Significado de los resultados:\n" +
            "- OUT_OF_TIME: el usuario se queda sin tiempo para actuar.\n" +
            "- ERROR_TURN: el usuario actúa cuando no es su turno.\n" +
            "- CORRECT: el usuario coloca un cubo correctamente en su turno.\n" +
            "- WAIT: el usuario espera adecuadamente durante el turno del avatar.\n\n" +
            "Tu objetivo es generar una frase corta y motivadora adaptada al estado del experimento, " +
            "al resultado y al tipo de usuario:\n" +
            "- Para TEA: respuestas muy claras, frases cortas, lenguaje sencillo, refuerzos positivos directos. Evita metáforas.\n" +
            "- Para DOWN: lenguaje sencillo, tono muy cariñoso y positivo, frases de 1 a 2 oraciones como máximo.\n" +
            "- Para AC: puedes usar un lenguaje un poco más elaborado, reforzar la reflexión y el reto.\n\n" +
            "No describas reglas del experimento ni hables de parámetros internos. Solo da la frase que diría el avatar directamente al niño.\n" +
            "Longitud recomendada: entre 1 y 25 palabras, máximo 2 frases cortas.\n\n" +
            "Ejemplo:\n" +
            "altura: 7, turno: avatar, usuario: DOWN, resultado: WAIT => \"¡Muy bien, has esperado tu turno! La torre está quedando muy alta, ¡qué chula!\"";

        // Mensaje de usuario con parámetros del experimento (formato simple tipo JSON)
        string parametrosJson =
            "{ \"altura\": " + altura +
            ", \"turno\": \"" + turno + "\"" +
            ", \"usuario\": \"" + usuario + "\"" +
            ", \"resultado\": \"" + resultado + "\" }";

        string userContent =
            "PARAMETROS_EXPERIMENTO_JSON:\n" + parametrosJson;

        string jsonBody = @"
        {
          ""model"": ""gpt-4o-mini"",
          ""messages"": [
            {
              ""role"": ""system"",
              ""content"": """ + EscapeJson(systemPrompt) + @"""
            },
            {
              ""role"": ""user"",
              ""content"": """ + EscapeJson(userContent) + @"""
            }
          ]
        }";

        var request = new UnityWebRequest(chatEndpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error OpenAI (chat): " + request.error + "\n" + request.downloadHandler.text);
            onAgentText?.Invoke(null);
            yield break;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log("Respuesta cruda del modelo de texto:\n" + responseText);

        ChatCompletionResponse resp = JsonUtility.FromJson<ChatCompletionResponse>(responseText);
        if (resp != null && resp.choices != null && resp.choices.Length > 0)
        {
            string assistantText = resp.choices[0].message.content;
            Debug.Log("Texto del avatar:\n" + assistantText);
            onAgentText?.Invoke(assistantText);
        }
        else
        {
            Debug.LogWarning("No se pudo parsear la respuesta del modelo de texto.");
            onAgentText?.Invoke(null);
        }
    }

    /// <summary>
    /// Convierte texto a audio (WAV) usando el endpoint /v1/audio/speech.
    /// </summary>
    private IEnumerator TextToSpeech(string text, Action<AudioClip> onClipReady)
    {
        // Pedimos PCM crudo en lugar de WAV
        string jsonBody = @"
        {
          ""model"": ""gpt-4o-mini-tts"",
          ""input"": """ + EscapeJson(text) + @""",
          ""voice"": ""ballad"",
          ""response_format"": ""pcm""
        }";

        var request = new UnityWebRequest(ttsEndpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);

        // Importante: buffer simple, sin AudioClip automático
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        // Recomendado: indicamos que queremos binario
        request.SetRequestHeader("Accept", "application/octet-stream");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error OpenAI (audio): " + request.error + "\n" + request.downloadHandler.text);
            onClipReady?.Invoke(null);
            yield break;
        }

        byte[] pcmBytes = request.downloadHandler.data;
        if (pcmBytes == null || pcmBytes.Length < 2)
        {
            Debug.LogError("Respuesta PCM vacía o demasiado corta.");
            onClipReady?.Invoke(null);
            yield break;
        }

        // --- Conversión PCM16 LE -> float[] ---
        int sampleCount = pcmBytes.Length / 2; // 16-bit => 2 bytes por muestra
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            // PCM 16-bit little-endian
            short sample = BitConverter.ToInt16(pcmBytes, i * 2);
            samples[i] = sample / 32768f; // normalizamos a [-1, 1]
        }

        // La API devuelve PCM 16-bit a 24kHz, mono
        int sampleRate = 24000;
        int channels = 1;

        AudioClip clip = AudioClip.Create("avatar-tts", sampleCount, channels, sampleRate, false);
        clip.SetData(samples, 0);

        Debug.Log($"AudioClip generado: length={clip.length:F2}s, freq={clip.frequency}, samples={sampleCount}");

        onClipReady?.Invoke(clip);
    }

    // ==== Utils ====

    private string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    // ==== Clases auxiliares para parsear la respuesta de chat ====
    [Serializable]
    public class ChatCompletionResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    public class Choice
    {
        public Message message;
    }

    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    // ==== Helper para convertir WAV (16-bit PCM) a AudioClip ====
    public static class WavHelper
    {
        public static AudioClip ToAudioClip(byte[] wavData, string name = "wav")
        {
            if (wavData == null || wavData.Length < 44)
            {
                Debug.LogError("WavHelper: datos WAV demasiado pequeños.");
                return null;
            }

            // RIFF/WAVE
            string riff = Encoding.ASCII.GetString(wavData, 0, 4);
            string wave = Encoding.ASCII.GetString(wavData, 8, 4);
            if (riff != "RIFF" || wave != "WAVE")
            {
                Debug.LogError("WavHelper: no es un archivo WAV válido.");
                return null;
            }

            int channels = 0;
            int sampleRate = 0;
            int bitsPerSample = 0;
            int dataStart = 0;
            int dataSize = 0;

            int pos = 12; // empezamos tras "RIFF....WAVE"
            while (pos + 8 <= wavData.Length)
            {
                string chunkId = Encoding.ASCII.GetString(wavData, pos, 4);
                int chunkSize = BitConverter.ToInt32(wavData, pos + 4);

                if (chunkSize < 0 || pos + 8 + chunkSize > wavData.Length)
                {
                    Debug.LogError("WavHelper: chunk inválido o corrupto.");
                    return null;
                }

                if (chunkId == "fmt ")
                {
                    int audioFormat = BitConverter.ToInt16(wavData, pos + 8);
                    channels = BitConverter.ToInt16(wavData, pos + 10);
                    sampleRate = BitConverter.ToInt32(wavData, pos + 12);
                    bitsPerSample = BitConverter.ToInt16(wavData, pos + 22);
                    // audioFormat suele ser 1 = PCM
                }
                else if (chunkId == "data")
                {
                    dataStart = pos + 8;
                    dataSize = chunkSize;
                    break;
                }

                pos += 8 + chunkSize;
            }

            if (dataStart == 0 || dataSize == 0)
            {
                Debug.LogError("WavHelper: no se encontró chunk 'data' válido.");
                return null;
            }

            if (channels <= 0 || sampleRate <= 0 || bitsPerSample <= 0)
            {
                Debug.LogError($"WavHelper: metadatos inválidos. channels={channels}, sampleRate={sampleRate}, bits={bitsPerSample}");
                return null;
            }

            int bytesPerSample = bitsPerSample / 8;
            if (bytesPerSample == 0)
            {
                Debug.LogError("WavHelper: bytesPerSample = 0.");
                return null;
            }

            int sampleCount = dataSize / bytesPerSample;
            if (sampleCount <= 0)
            {
                Debug.LogError("WavHelper: sampleCount <= 0. dataSize=" + dataSize);
                return null;
            }

            int samplesPerChannel = sampleCount / channels;
            if (samplesPerChannel <= 0)
            {
                Debug.LogError("WavHelper: samplesPerChannel <= 0.");
                return null;
            }

            float[] floatData = new float[sampleCount];
            int offset = dataStart;

            if (bitsPerSample == 16)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    short sample = BitConverter.ToInt16(wavData, offset);
                    floatData[i] = sample / 32768f; // [-1, 1]
                    offset += 2;
                }
            }
            else
            {
                Debug.LogError("WavHelper: solo implementado para 16-bit PCM.");
                return null;
            }

            AudioClip clip = AudioClip.Create(name, samplesPerChannel, channels, sampleRate, false);
            clip.SetData(floatData, 0);
            return clip;
        }
    }
}
