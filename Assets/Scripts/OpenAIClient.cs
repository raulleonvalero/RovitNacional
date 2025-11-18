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

    // ============================================================
    // TOWER EXPERIMENT
    // ============================================================

    /// <summary>
    /// Call the text model (agent) and then convert its response to audio (tower experiment).
    /// </summary>
    /// <param name="height">Tower height (0-8)</param>
    /// <param name="currentTurn">"avatar" or "usuario"</param>
    /// <param name="previousTurn">"avatar", "usuario" or null</param>
    /// <param name="userType">"TEA", "DOWN", "AC"</param>
    /// <param name="result">"OUT_OF_TIME", "ERROR_TURN", "CORRECT", "WAIT"</param>
    /// <param name="requestType">"FEEDBACK_RESULTADO", "EXPLICAR_EXPERIMENTO", "INDICAR_TURNO_ACTUAL"</param>
    /// <param name="onDone">Callback with (textResponse, audioClip)</param>
    public void GenerateTowerResponseAndAudio(int height, string currentTurn, string previousTurn, string userType, string result, string last_result, string requestType, Action<string, AudioClip> onDone)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key empty. Set it as environment variable OPENAI_API_KEY.");
            onDone?.Invoke(null, null);
            return;
        }

        StartCoroutine(AgentThenTTSCoroutine_Tower(
            height,
            currentTurn,
            previousTurn,
            userType,
            result,
            last_result,
            requestType,
            onDone
        ));
    }

    private IEnumerator AgentThenTTSCoroutine_Tower(int height, string currentTurn, string previousTurn, string userType, string result, string last_result, string requestType, Action<string, AudioClip> onDone)
    {
        // 1) Call text model (agent)
        yield return SendAgentRequest_Tower(
            height,
            currentTurn,
            previousTurn,
            userType,
            result,
            last_result,
            requestType,
            (agentText) =>
            {
                if (string.IsNullOrEmpty(agentText))
                {
                    onDone?.Invoke(null, null);
                    return;
                }

                // 2) With the agent text, call TTS
                StartCoroutine(TextToSpeech(agentText, (clip) =>
                {
                    onDone?.Invoke(agentText, clip);
                }));
            }
        );
    }

    /// <summary>
    /// Tower experiment - call text model (gpt-4o-mini) with tower prompt.
    /// </summary>
    private IEnumerator SendAgentRequest_Tower(int height, string currentTurn, string previousTurn, string userType, string result, string last_result, string requestType, Action<string> onAgentText)
    {
        string systemPrompt = Prompt_Tower();

        // Handle possible nulls for JSON
        string previousTurnField = previousTurn == null
            ? "null"
            : "\"" + previousTurn + "\"";

        string parametersJson =
            "{ \"tipo_peticion\": \"" + requestType + "\"" +
            ", \"altura\": " + height +
            ", \"turno_actual\": \"" + currentTurn + "\"" +
            ", \"turno_anterior\": " + previousTurnField +
            ", \"tipo_usuario\": \"" + userType + "\"" +
            ", \"resultado\": \"" + result + "\" }";

        string userContent =
            "PARAMETROS_EXPERIMENTO_JSON:\n" + parametersJson;

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
            Debug.LogError("Error OpenAI (chat tower): " + request.error + "\n" + request.downloadHandler.text);
            onAgentText?.Invoke(null);
            yield break;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log("Raw response (tower):\n" + responseText);

        ChatCompletionResponse resp = JsonUtility.FromJson<ChatCompletionResponse>(responseText);
        if (resp != null && resp.choices != null && resp.choices.Length > 0)
        {
            string assistantText = resp.choices[0].message.content;
            Debug.Log("Avatar text (tower):\n" + assistantText);
            onAgentText?.Invoke(assistantText);
        }
        else
        {
            Debug.LogWarning("Could not parse tower response from text model.");
            onAgentText?.Invoke(null);
        }
    }

    /// <summary>
    /// System prompt for tower experiment.
    /// </summary>
    private string Prompt_Tower()
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
          ""resultado"": ""OUT_OF_TIME"" | ""ERROR_TURN"" | ""CORRECT"" | ""WAIT"",
          ""resultado_anterior"": ""OUT_OF_TIME"" | ""ERROR_TURN"" | ""CORRECT"" | ""WAIT"" | null
        }

        Adaptación al tipo de usuario:
        - Para TEA: respuestas muy claras, frases cortas, lenguaje sencillo, refuerzos positivos directos. Evita metáforas.
        - Para DOWN: lenguaje sencillo, tono muy amigable y positivo, frases de 1 a 2 oraciones como máximo.
        - Para AC: puedes usar un lenguaje un poco más elaborado, reforzar la reflexión y el reto.

        No describas reglas internas del sistema ni hables de parámetros JSON. Solo di la frase que el avatar diría directamente al niño.

        LONGITUD GENERAL:
        - Entre 1 y 25 palabras, máximo 2 frases cortas.

        ========================================
        MODO 1: EXPLICAR_EXPERIMENTO (""EXPLICAR_EXPERIMENTO"")
        ========================================

        Objetivo:
        Explicar de forma sencilla en qué consiste la tarea y cómo funcionan los turnos.

        Instrucciones:
        - Explica que vais a construir una torre de cubos entre el avatar y el niño.
        - Explica que se juega por turnos: cada vez que hay que colocar un cubo puede ser el turno del usuario o el turno del avatar.
        - Aclara que solo se puede poner un cubo cuando es tu turno.
        - Adapta el lenguaje al tipo de usuario (TEA; DOWN, AC)
        - Máximo 2 frases cortas.

        Ejemplos de estilo (no los repitas literalmente, úsalos como guía):
        - TEA: ""Vamos a construir una torre de cubos. Tú y yo pondremos cubos por turnos: primero uno, luego el otro.""
        - DOWN: ""Vamos a hacer una torre juntos. Primero uno de nosotros pone un cubo, luego el otro pone un cubo más.""
        - AC: ""Vamos a construir una torre por turnos. Tú y yo iremos alternando los cubos para ver hasta dónde llegamos sin que se caiga.""

        ========================================
        MODO 2: INDICAR_TURNO_ACTUAL (""INDICAR_TURNO_ACTUAL"")
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

        Regla especial con resultado_anterior = ERROR_TURN:
        - Si resultado_anterior = ""ERROR_TURN"" y turno_actual = ""avatar"":
          * Debes recordar que ahora es el turno del avatar.
          * Ejemplo de estilo  ** NO los repitas literalmente, úsalos como guía **:
            ""Recuerda, ahora es mi turno, voy a colocar un cubo en la torre.""

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
        MODO 3: FEEDBACK_RESULTADO (""FEEDBACK_RESULTADO"")
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
        - No menciones los parámetros JSON ni las reglas del experimento.   
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
        SALIDA
        ========================================

        Devuelve solo la frase final que diría el avatar, sin explicar el JSON, sin etiquetas y sin texto adicional.

        ";
    }

    // ============================================================
    // PIECES EXPERIMENT
    // ============================================================

    /// <summary>
    /// Call the text model (agent) and then convert its response to audio (pieces experiment).
    /// </summary>
    /// <param name="userType">"TEA", "DOWN", "AC"</param>
    /// <param name="chosenPiece">"cubo_rojo", "esfera_azul", "cilindro_verde"</param>
    /// <param name="currentTurn">"avatar" or "usuario"</param>
    /// <param name="previousTurn">"avatar", "usuario" or null</param>
    /// <param name="result">"OUT_OF_TIME", "ERROR_TURN", "CORRECT", "WAIT", "WRONG_PIECE"</param>
    /// <param name="previousResult">previous result or null</param>
    /// <param name="requestType">"FEEDBACK_RESULTADO", "EXPLICAR_EXPERIMENTO", "INDICAR_TURNO_ACTUAL"</param>
    /// <param name="onDone">Callback with (textResponse, audioClip)</param>
    public void GeneratePiecesResponseAndAudio(string userType, string chosenPiece, string currentTurn, string previousTurn, string result, string previousResult, string requestType, Action<string, AudioClip> onDone)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key empty. Set it as environment variable OPENAI_API_KEY.");
            onDone?.Invoke(null, null);
            return;
        }

        StartCoroutine(AgentThenTTSCoroutine_Pieces(
            userType,
            chosenPiece,
            currentTurn,
            previousTurn,
            result,
            previousResult,
            requestType,
            onDone
        ));
    }

    private IEnumerator AgentThenTTSCoroutine_Pieces(string userType, string chosenPiece, string currentTurn, string previousTurn, string result, string previousResult, string requestType, Action<string, AudioClip> onDone)
    {
        yield return SendAgentRequest_Pieces(
            userType,
            chosenPiece,
            currentTurn,
            previousTurn,
            result,
            previousResult,
            requestType,
            (agentText) =>
            {
                if (string.IsNullOrEmpty(agentText))
                {
                    onDone?.Invoke(null, null);
                    return;
                }

                StartCoroutine(TextToSpeech(agentText, (clip) =>
                {
                    onDone?.Invoke(agentText, clip);
                }));
            }
        );
    }

    /// <summary>
    /// Pieces experiment - call text model (gpt-4o-mini) with pieces prompt.
    /// </summary>
    private IEnumerator SendAgentRequest_Pieces(string userType, string chosenPiece, string currentTurn, string previousTurn, string result, string previousResult, string requestType, Action<string> onAgentText)
    {
        string systemPrompt = Prompt_Pieces();

        string previousTurnField = previousTurn == null
            ? "null"
            : "\"" + previousTurn + "\"";

        string previousResultField = previousResult == null
            ? "null"
            : "\"" + previousResult + "\"";

        string parametersJson =
            "{ \"tipo_peticion\": \"" + requestType + "\"" +
            ", \"tipo_usuario\": \"" + userType + "\"" +
            ", \"turno_actual\": \"" + currentTurn + "\"" +
            ", \"turno_anterior\": " + previousTurnField +
            ", \"pieza_elegida\": \"" + chosenPiece + "\"" +
            ", \"resultado\": \"" + result + "\"" +
            ", \"resultado_anterior\": " + previousResultField +
            " }";

        string userContent =
            "PARAMETROS_EXPERIMENTO_JSON:\n" + parametersJson;

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
            Debug.LogError("Error OpenAI (chat pieces): " + request.error + "\n" + request.downloadHandler.text);
            onAgentText?.Invoke(null);
            yield break;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log("Raw response (pieces):\n" + responseText);

        ChatCompletionResponse resp = JsonUtility.FromJson<ChatCompletionResponse>(responseText);
        if (resp != null && resp.choices != null && resp.choices.Length > 0)
        {
            string assistantText = resp.choices[0].message.content;
            Debug.Log("Avatar text (pieces):\n" + assistantText);
            onAgentText?.Invoke(assistantText);
        }
        else
        {
            Debug.LogWarning("Could not parse pieces response from text model.");
            onAgentText?.Invoke(null);
        }
    }

    private string Prompt_Pieces()
    {
        return @"
        Eres un avatar que dirige un experimento donde el niño o la niña tiene que tocar una pieza concreta.

        En la mesa hay siempre tres piezas:
        - Un cubo de color rojo.
        - Una esfera de color azul.
        - Un cilindro de color verde.

        El experimento tiene estas reglas:
        1) En cada turno solo actúa una persona: el avatar o el usuario.
        2) Si es el turno del usuario, debe tocar la pieza indicada por el avatar.
        3) Las piezas posibles son siempre: cubo rojo, esfera azul o cilindro verde.

        Siempre recibirás un JSON con la siguiente información:

        {
          ""tipo_peticion"": ""FEEDBACK_RESULTADO"" | ""EXPLICAR_EXPERIMENTO"" | ""INDICAR_TURNO_ACTUAL"",
          ""tipo_usuario"": ""TEA"" | ""DOWN"" | ""AC"",
          ""turno_actual"": ""avatar"" | ""usuario"",
          ""turno_anterior"": ""avatar"" | ""usuario"" | null,
          ""pieza_elegida"": ""cubo_rojo"" | ""esfera_azul"" | ""cilindro_verde"",
          ""resultado"": ""OUT_OF_TIME"" | ""ERROR_TURN"" | ""CORRECT"" | ""WAIT"" | ""WRONG_PIECE"",
          ""resultado_anterior"": ""OUT_OF_TIME"" | ""ERROR_TURN"" | ""CORRECT"" | ""WAIT"" | ""WRONG_PIECE"" | null
        }

        Donde:
        - ""pieza_elegida"" es la pieza que el avatar quiere que toque el niño o la niña.
        - ""resultado"" describe lo que ha pasado en el turno actual.
        - ""resultado_anterior"" describe lo que ocurrió en el turno anterior (si existe).

        Posibles resultados:
        - OUT_OF_TIME: el usuario se queda sin tiempo para tocar la pieza.
        - ERROR_TURN: el usuario intenta tocar cuando no es su turno.
        - CORRECT: el usuario toca la pieza correcta (color y forma coinciden con ""pieza_elegida"").
        - WAIT: el usuario espera adecuadamente mientras actúa el avatar.
        - WRONG_PIECE: el usuario toca una pieza que no corresponde con color y forma de ""pieza_elegida"".


        Adaptación al tipo de usuario:
        - Para TEA: respuestas muy claras, frases cortas, lenguaje sencillo, refuerzos positivos directos. Evita metáforas.
        - Para DOWN: lenguaje sencillo, tono muy amigable y positivo, frases de 1 a 2 oraciones como máximo.
        - Para AC: puedes usar un lenguaje un poco más elaborado, reforzar la reflexión y el reto.

        No describas reglas internas del sistema ni hables de parámetros JSON. Solo di la frase que el avatar diría directamente al niño.

        LONGITUD GENERAL:
        - Entre 10 y 25 palabras, máximo 2 frases cortas.

        ========================================
        MODO 1: EXPLICAR_EXPERIMENTO (""EXPLICAR_EXPERIMENTO"")
        ========================================

        Objetivo:
        Explicar de forma sencilla en qué consiste la tarea y cómo funcionan los turnos.

        Instrucciones:
        - Explica que hay tres piezas: cubo rojo, esfera azul y cilindro verde.
        - Explica que el avatar dirá qué pieza hay que tocar.
        - Explica que se juega por turnos: a veces toca el avatar y otras el niño.
        - Aclara que el niño debe fijarse en el color y la forma para elegir la pieza correcta.
        - Adapta el lenguaje al tipo de usuario (TEA, DOWN, AC).
        - Máximo 2 frases cortas.

        Ejemplos de estilo ** NO los repitas literalmente, úsalos como guía **:
        - TEA:  ""Vamos a jugar con tres piezas: cubo rojo, esfera azul y cilindro verde. Yo diré que pieza hay que tocar y quien debe tocarla.""

        - DOWN:  ""Tenemos un cubo rojo, una esfera azul y un cilindro verde. Yo te digo una pieza y tú la tocas cuando sea tu turno.""

        - AC:  ""Jugaremos a un juego de atención con un cubo rojo, una esfera azul y un cilindro verde. Cuando yo diga una pieza y sea tu turno, intenta encontrar y tocar la correcta.""

        ========================================
        MODO 2: INDICAR_TURNO_ACTUAL (""INDICAR_TURNO_ACTUAL"")
        ========================================

        Objetivo:
        Decir de quién es el turno ahora y qué debe ocurrir, usando el turno actual,
        el turno anterior, la pieza elegida y, si hace falta, el resultado anterior.

        Instrucciones generales:
        - Usa ""turno_actual"" para indicar claramente si ahora le toca al avatar o al niño.
        - Si turno_actual = usuario:
          * Indica claramente qué pieza debe tocar el niño:
            - Si pieza_elegida = cubo_rojo, nómbralo como ""cubo rojo"".
            - Si pieza_elegida = esfera_azul, nómbralo como ""esfera azul"".
            - Si pieza_elegida = cilindro_verde, nómbralo como ""cilindro verde"".
          * Recuerda siempre mencionar forma y color.
        - Si turno_actual = avatar:
          * Indica que ahora actúa el avatar (por ejemplo, para mostrar o tocar una pieza).
        - Ajusta el lenguaje al tipo de usuario (TEA, DOWN, AC).

        Regla especial con resultado_anterior = WRONG_PIECE:
        - Si resultado_anterior = ""WRONG_PIECE"" y turno_actual = ""usuario"":
          * Debes decir que la pieza a tocar es la misma que el turno anterior. 
          * Debes repetir claramente la misma pieza_elegida, insistiendo en color y forma.
          * Ejemplo de estilo  ** NO los repitas literalmente, úsalos como guía **:
            ""Vuelve a ser tu turno. Recuerda, la pieza correcta es el cubo de color rojo.""

        Regla especial con resultado_anterior = ERROR_TURN:
        - Si resultado_anterior = ""ERROR_TURN"" y turno_actual = ""avatar"":
          * Debes recordar que ahora es el turno del avatar.
          * Ejemplo de estilo  ** NO los repitas literalmente, úsalos como guía **:
            ""Recuerda, ahora es mi turno, voy a tocar la esfera azul.""

        Ejemplos orientativos ** NO los repitas literalmente, úsalos como guía **:
        - turno_actual = usuario, turno_anterior = avatar, pieza_elegida = cubo_rojo, tipo_usuario = TEA:
          ""Ahora es tu turno, toca el cubo de color rojo.""

        - turno_actual = usuario, turno_anterior = usuario, pieza_elegida = esfera_azul, tipo_usuario = DOWN:
          ""Otra vez te toca a ti, busca y toca la esfera azul.""

        - turno_actual = avatar, turno_anterior = usuario, pieza_elegida = cilindro_verde, tipo_usuario = AC:
          ""Ahora me toca a mí, voy a tocar el cilindro verde.""

        - - turno_actual = avatar, turno_anterior = avatar, pieza_elegida = esfera_azul, tipo_usuario = DOWN:
          ""Es mi turno otra vez, esta vez voy a tocar la esfera azul.""

        - resultado_anterior = WRONG_PIECE, turno_actual = usuario, pieza_elegida = cilindro_verde:
          ""Es tu turno. Recuerda tienes que tocar el cilindro verde.""

        ========================================
        MODO 3: FEEDBACK_RESULTADO (""FEEDBACK_RESULTADO"")
        ========================================

        Objetivo:
        Dar una frase corta y motivadora adaptada al resultado, a la pieza objetivo y al tipo de usuario.
        Solo mencionar el resultado, no decir nada del siguiente turno.

        Instrucciones:
        - Si resultado = CORRECT:
          * Refuerza que ha tocado bien la pieza.
        - Si resultado = WAIT:
          * Refuerza que ha esperado bien mientras actuaba el avatar.
        - Si resultado = OUT_OF_TIME o ERROR_TURN:
          * Corrige de forma suave y motivadora, sin regañar.
        - Si resultado = WRONG_PIECE:
          * Di que esa pieza no era la correcta.
          * Menciona que el usuario tiene otra oportunidad..
        - Ajusta el estilo según tipo_usuario (TEA, DOWN, AC).
        - No repitas literalmente los nombres de los parámetros.

        Ejemplos orientativos ** NO los repitas literalmente, úsalos como guía **:
        - tipo_usuario: TEA, resultado: CORRECT, pieza_elegida: cubo_rojo
          => ""Muy bien, has tocado el cubo rojo.""

        - tipo_usuario: DOWN, resultado: WAIT
          => ""¡Genial, has esperado muy bien mientras yo jugaba!""

        - tipo_usuario: DOWN, resultado: OUT_OF_TIME
          => ""Esta vez se ha acabado el tiempo, pero lo estás haciendo muy bien.""

        - tipo_usuario: DOWN, resultado: ERROR_TURN
          => ""Te has adelantado un poquito, ahora no era tu turno, pero vas muy bien.""

        - tipo_usuario: AC, resultado: CORRECT, pieza_elegida: cilindro_verde
          => ""Has elegido muy bien el cilindro verde, buena atención al detalle.""

        - tipo_usuario: TEA, resultado: WRONG_PIECE, pieza_elegida: esfera_azul
          => ""Esa pieza no era la correcta. No pasa nada, vamos a volver a intentarlo.""

        ========================================
        SALIDA
        ========================================

        Devuelve solo la frase final que diría el avatar, sin explicar el JSON, sin etiquetas y sin texto adicional.
        ";
    }

    // ============================================================
    // TTS
    // ============================================================

    /// <summary>
    /// Convert text to audio (PCM) using /v1/audio/speech.
    /// </summary>
    private IEnumerator TextToSpeech(string text, Action<AudioClip> onClipReady)
    {
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
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
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
            Debug.LogError("PCM response is empty or too short.");
            onClipReady?.Invoke(null);
            yield break;
        }

        int sampleCount = pcmBytes.Length / 2;
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = BitConverter.ToInt16(pcmBytes, i * 2);
            samples[i] = sample / 32768f;
        }

        int sampleRate = 24000;
        int channels = 1;

        AudioClip clip = AudioClip.Create("avatar-tts", sampleCount, channels, sampleRate, false);
        clip.SetData(samples, 0);

        Debug.Log($"AudioClip generated: length={clip.length:F2}s, freq={clip.frequency}, samples={sampleCount}");
        onClipReady?.Invoke(clip);
    }

    // ============================================================
    // Utils & DTOs
    // ============================================================

    private string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

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

    // WAV helper kept for reference (not used with PCM format)
    public static class WavHelper
    {
        public static AudioClip ToAudioClip(byte[] wavData, string name = "wav")
        {
            if (wavData == null || wavData.Length < 44)
            {
                Debug.LogError("WavHelper: WAV data too small.");
                return null;
            }

            string riff = Encoding.ASCII.GetString(wavData, 0, 4);
            string wave = Encoding.ASCII.GetString(wavData, 8, 4);
            if (riff != "RIFF" || wave != "WAVE")
            {
                Debug.LogError("WavHelper: not a valid WAV file.");
                return null;
            }

            int channels = 0;
            int sampleRate = 0;
            int bitsPerSample = 0;
            int dataStart = 0;
            int dataSize = 0;

            int pos = 12;
            while (pos + 8 <= wavData.Length)
            {
                string chunkId = Encoding.ASCII.GetString(wavData, pos, 4);
                int chunkSize = BitConverter.ToInt32(wavData, pos + 4);

                if (chunkSize < 0 || pos + 8 + chunkSize > wavData.Length)
                {
                    Debug.LogError("WavHelper: invalid or corrupt chunk.");
                    return null;
                }

                if (chunkId == "fmt ")
                {
                    int audioFormat = BitConverter.ToInt16(wavData, pos + 8);
                    channels = BitConverter.ToInt16(wavData, pos + 10);
                    sampleRate = BitConverter.ToInt32(wavData, pos + 12);
                    bitsPerSample = BitConverter.ToInt16(wavData, pos + 22);
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
                Debug.LogError("WavHelper: no valid 'data' chunk found.");
                return null;
            }

            if (channels <= 0 || sampleRate <= 0 || bitsPerSample <= 0)
            {
                Debug.LogError($"WavHelper: invalid metadata. channels={channels}, sampleRate={sampleRate}, bits={bitsPerSample}");
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
                    floatData[i] = sample / 32768f;
                    offset += 2;
                }
            }
            else
            {
                Debug.LogError("WavHelper: only 16-bit PCM implemented.");
                return null;
            }

            AudioClip clip = AudioClip.Create(name, samplesPerChannel, channels, sampleRate, false);
            clip.SetData(floatData, 0);
            return clip;
        }
    }
}
