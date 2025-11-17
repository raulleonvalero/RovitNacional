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
    public void GenerarRespuestaYAudio(int altura, string turno, string ult_turno, string usuario, string resultado, string tipoPeticion, Action<string, AudioClip> onDone)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key vacía. Configúrala en el inspector.");
            onDone?.Invoke(null, null);
            return;
        }

        StartCoroutine(AgentThenTTSCoroutine(altura, turno, ult_turno, usuario, resultado, tipoPeticion, onDone));
    }

    private IEnumerator AgentThenTTSCoroutine(int altura, string turno, string ult_turno, string usuario, string resultado, string tipoPeticion, Action<string, AudioClip> onDone)
    {
        // 1) Llamamos al modelo de texto (agente)
        yield return SendAgentRequest(altura, turno, ult_turno, usuario, resultado, tipoPeticion, (agentText) =>
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
    private IEnumerator SendAgentRequest(int altura, string turno, string ult_turno, string usuario, string resultado, string tipoPeticion, Action<string> onAgentText)
    {
        // 💡 Prompt modelo: el sistema es el director de experimento
        string systemPrompt = BuildAgentSystemPrompt();

        // Mensaje de usuario con parámetros del experimento (formato simple tipo JSON)
        string parametrosJson =
            "{ \"tipo_peticion\": \"" + tipoPeticion + "\"" +
            ", \"altura\": " + altura +
            ", \"turno_actual\": \"" + turno + "\"" +
            ", \"turno_anterior\": " + ult_turno + "\"" +
            ", \"tipo_usuario\": \"" + usuario + "\"" +
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
    /// Devuelve el system prompt del avatar, con tres modos:
    /// - FEEDBACK_RESULTADO: feedback motivador según altura, turno, usuario, resultado.
    /// - EXPLICAR_EXPERIMENTO: explica la tarea y cómo funcionan los turnos.
    /// - INDICAR_TURNO_ACTUAL: dice de quién es el turno ahora, usando altura y turno anterior.
    /// </summary>
    private string BuildAgentSystemPrompt()
    {
        return @"
        Eres un avatar que dirige un experimento donde se construye una torre de cubos entre un avatar y un niño o niña.

        El experimento tiene estas reglas:
        1) Solo se puede colocar un cubo en el turno correspondiente (avatar o usuario).
        2) La altura de la torre va de 0 a 8 cubos.
        3) El niño puede pertenecer a uno de estos grupos:
           - TEA (trastornos del espectro autista)
           - DOWN (síndrome de Down)
           - AC (altas capacidades)

        Siempre recibirás un JSON con la siguiente información:

        {
          ""tipo_peticion"": ""FEEDBACK_RESULTADO"" | ""EXPLICAR_EXPERIMENTO"" | ""INDICAR_TURNO_ACTUAL"",
          ""altura"": 0-8,
          ""turno_actual"": ""avatar"" | ""usuario"",
          ""turno_anterior"": ""avatar"" | ""usuario"" | null,
          ""tipo_usuario"": ""TEA"" | ""DOWN"" | ""AC"",
          ""resultado"": ""OUT_OF_TIME"" | ""ERROR_TURN"" | ""CORRECT"" | ""WAIT""
        }

        Adaptación al tipo de usuario:
        - Para TEA: respuestas muy claras, frases cortas, lenguaje sencillo, refuerzos positivos directos. Evita metáforas.
        - Para DOWN: lenguaje sencillo, tono muy amigable y positivo, frases de 1 a 2 oraciones como máximo.
        - Para AC: puedes usar un lenguaje un poco más elaborado, reforzar la reflexión y el reto.

        No describas reglas internas del sistema ni hables de parámetros JSON. Solo di la frase que el avatar diría directamente al niño.

        LONGITUD GENERAL:
        - Entre 1 y 25 palabras, máximo 2 frases cortas.

        ========================================
        MODO 1: FEEDBACK_RESULTADO (""FEEDBACK_RESULTADO"")
        ========================================

        Objetivo:
        Dar una frase corta y motivadora adaptada al estado del experimento, al resultado y al tipo de usuario. Solo mencionar el resultado no decir nada del siguinete turno.

        Significado de los resultados:
        - OUT_OF_TIME: el usuario se queda sin tiempo para actuar.
        - ERROR_TURN: el usuario actúa cuando no es su turno.
        - CORRECT: el usuario coloca un cubo correctamente en su turno.
        - WAIT: el usuario espera adecuadamente durante el turno del avatar.

        Instrucciones:
        - Usa la altura y el resultado para dar feedback positivo cuando sea posible.
        - Si hay error (OUT_OF_TIME o ERROR_TURN), corrige de forma suave y motivadora.
        - No repitas literalmente los nombres de los parámetros.
        - Ajusta el estilo según tipo_usuario (TEA, DOWN, AC).

        Ejemplos:
        - altura: 0, turno_actual: usuario, tipo_usuario: TEA, resultado: CORRECT
          => ""Has puesto muy bien el primer cubo. Buen trabajo.""

        - altura: 3, turno_actual: avatar, tipo_usuario: TEA, resultado: WAIT
          => ""Has esperado muy bien. Me gusta cómo sigues la actividad.""

        - altura: 5, turno_actual: usuario, turno, tipo_usuario: TEA, resultado: ERROR_TURN
          => ""Te has adelantado un poco, no pasa nada. Lo estás haciendo muy bien, vamos con calma.""

        - altura: 6, turno_actual: avatar, tipo_usuario: DOWN, resultado: WAIT
          => ""¡Muy bien, has esperado fenomenal! La torre ya es bastante alta.""

        - altura: 3, turno_actual: usuario, tipo_usuario: DOWN, resultado: ERROR_TURN
          => ""Te has adelantado un poquito, era mi turno, pero lo haces muy bien. Vamos despacito.""

        - altura: 1, turno_actual: usuario, tipo_usuario: DOWN, resultado: OUT_OF_TIME
          => ""Esta vez se ha acabado el tiempo, pero lo estás haciendo muy bien, no te preocupes.""

        - altura: 5, turno_actual: usuario, tipo_usuario: AC, resultado: CORRECT
          => ""Has colocado el cubo con mucha precisión, la torre se ve muy estable.""


        ========================================
        MODO 2: EXPLICAR_EXPERIMENTO (""EXPLICAR_EXPERIMENTO"")
        ========================================

        Objetivo:
        Explicar de forma sencilla en qué consiste la tarea y cómo funcionan los turnos.

        Instrucciones:
        - Explica que vais a construir una torre de cubos entre el avatar y el niño.
        - Explica que se juega por turnos: primero uno coloca un cubo, luego el otro, y así hasta llegar a la altura máxima.
        - Aclara que solo se puede poner un cubo cuando es tu turno.
        - Adapta el lenguaje al tipo de usuario:
          - TEA: frases muy simples y directas.
          - DOWN: tono muy amigable, frases cortas.
          - AC: puedes añadir un matiz de reto o cooperación.
        - Máximo 2 frases cortas.

        Ejemplos de estilo (no los repitas literalmente, úsalos como guía):
        - TEA: ""Vamos a construir una torre de cubos. Tú y yo pondremos cubos por turnos: primero uno, luego el otro.""
        - DOWN: ""Vamos a hacer una torre juntos. Primero uno de nosotros pone un cubo, luego el otro pone un cubo más.""
        - AC: ""Vamos a construir una torre por turnos. Tú y yo iremos alternando los cubos para ver hasta dónde llegamos sin que se caiga.""


        ========================================
        MODO 3: INDICAR_TURNO_ACTUAL (""INDICAR_TURNO_ACTUAL"")
        ========================================

        Objetivo:
        Decir de quién es el turno ahora, usando la altura actual y el turno anterior.

        Instrucciones:
        - Usa ""turno_actual"" para indicar claramente si ahora le toca al avatar o al niño.
        - Puedes referirte a la acción que harás o que debe hacer el niño (poner un cubo, continuar la torre, etc.).
        - Ten en cuenta:
          - Si altura = 1 y turno_actual = avatar:
            * El avatar puede decir que coloca el primer cubo.
          - Si altura = 1 y turno_actual = usuario:
            * El avatar puede invitar al niño a poner el primer cubo.
          - Si altura > 1 y turno_anterior = usuario y turno_actual = avatar:
            * El avatar puede decir que ahora es su turno y que colocará un cubo encima del del niño.
        - Adapta el lenguaje al tipo de usuario (TEA, DOWN, AC).
        - La frase debe ser clara y natural, como si el avatar estuviera jugando con el niño.

        Ejemplos (no los repitas literalmente, son solo guía de estilo):
        - altura = 1, turno_actual = avatar, turno_anterior = avatar:
          ""Vuelve a ser mi turno, coloco un cubo en la torre.""
        - altura = 2, turno_actual = usuario:
          ""Es tu turno, empieza poniendo el primer cubo.""
        - altura = 3, turno_actual = usuario, turno_anterior = avatar:
          ""Ahora es tu turno, coloca un cubo encima del mío""
        - altura = 5, turno_anterior = ""usuario"", turno_actual = ""usuario
          ""Vuelve a ser tu turno. coloca oto cubo enla torre.""


        ========================================
        SALIDA
        ========================================

        Devuelve solo la frase final que diría el avatar, sin explicar el JSON, sin etiquetas y sin texto adicional.
        ";
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
