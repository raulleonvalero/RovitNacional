using System.Collections;             // <- Necesario para IEnumerator/Coroutines
using System.Collections.Generic;
using UnityEngine;
using RovitNacional;

public class Controller : MonoBehaviour
{
    [Header("Piezas y SpawnPoints")]
    [SerializeField] private List<GameObject> pieces = new List<GameObject>();
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Avatar")]
    [SerializeField] private GameObject avatar;
    [SerializeField] private GameObject left_rest;
    [SerializeField] private GameObject right_rest;

    [Header("GPTClient")]
    [SerializeField] private GameObject apiController;
    [SerializeField] private AudioSource audioSource;

    [Header("Experimento")]
    [SerializeField] private float timeLimit = 15f;
    [SerializeField] string user_type = "TEA";
    [SerializeField] int num_trials = 10;

    // Par�metros del experimento
    private int current_trial = 0;
    private bool experimentRunning = false;
    private bool user = false;
    private bool complete = false;
    private int piece_id = 0;

    // Parametros auxiliares
    private Vector3 piece_height_offset = new Vector3(0, 0.1f, 0);

    void Start()
    {
        var ch = avatar.GetComponent<Character>();

        ch.MoveLeftHand(left_rest.transform.position, 0.3f);
        ch.MoveRightHand(right_rest.transform.position, 0.3f);

        ch.setMode(Activity.GoStopGo, Mode.TEA);

        lookAt(look_target);

        Logging.WriteLog((int)Activity.GoStopGo, -1, "Excena GoStopGo Iniciada");
    }

    public void OnStartExperimentButtonPressed()
    {
        if (experimentRunning)
        {
            StopAllCoroutines();
            experimentRunning = false;
        }
        else
        {
            StartCoroutine(ExperimentRoutine());
            experimentRunning = true;
        }
    }

    void SetRandomSpawns()
    {
        if (pieces.Count == 0 || spawnPoints.Count == 0)
        {
            Debug.LogWarning("Faltan piezas o spawn points.");
            return;
        }

        // Copia barajada de los spawn points
        var shuffled = new List<Transform>(spawnPoints);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Colocar/respawnear piezas
        for (int i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            if (piece == null) continue;

            var spawnPoint = shuffled[i % shuffled.Count];
            piece.SetActive(true);

            var sr = piece.GetComponent<SimpleRespawn>();
            if (sr != null)
            {
                sr.SetSpawnPoint(spawnPoint);
                sr.Respawn();
            }
            else
            {
                // Colocaci�n directa si no hay SimpleRespawn
                piece.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }
        }
    }

    static string PieceName(int id)
    {
        switch (id)
        {
            case 0: return "cubo_rojo";
            case 1: return "esfera_azul";
            case 2: return "cilindro_verde";
            default: return "desconocido";
        }
    }

    private bool CheckForPieces()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null) continue;
            if (pieces[i].GetComponent<SimpleRespawn>().IsBeingTouched())
            {
                return true;
            }
        }
        return false;
    }

    IEnumerator ExperimentRoutine()
    {
        Debug.Log("Experimento iniciado.");
        Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Experimento Iniciado");

        var ch = avatar.GetComponent<Character>();

        // Paso 1: Explicaci�n del experimento
        ch.Speak("greeting_hello");

        Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Explicacion del experimento");

        while (ch.isSpeaking()) yield return null;

        yield return new WaitForSeconds(0.5f);

        ch.Speak("explain");

        while (ch.isSpeaking()) yield return null;

        yield return new WaitForSeconds(0.5f);

        while (true) // bucle de trials; si quieres n�mero fijo, reemplaza por for (int t=0; t<T; t++)
        {
            SetRandomSpawns();

            result = -1;
            resultado = "";

            bool touchedPiece = false;
            bool User_turn;
            piece_id = Random.Range(0, Mathf.Max(1, pieces.Count));

            if (ult_resultado == "WRONG_PIECE")
            {
                User_turn = true;
                piece_id = last_piece_id;
            }

            else if (ult_resultado == "ERROR_TURN")
            {
                User_turn = false;
            Logging.WriteLog((int)Experimento.Actividad, -1, "Spawn de Figuras listo");

            if (!user) 
            { ch.Speak("turn_my_turn");
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Turno del Avatar");
            }
            else
            {
                ch.Speak("turn_your_turn");
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Turno del Usuario");
            } 

            while (ch.isSpeaking()) yield return null;

            yield return new WaitForSeconds(0.5f);

            if (user)
            {
                if (piece_id == 0) ch.Speak("action_touch_cube_red_user");

                else if (piece_id == 1) ch.Speak("action_touch_sphere_blue_user");

                else if (piece_id == 2) ch.Speak("action_touch_cylinder_green_user");
            }

            else
            {
                if (ult_turno == "usuario")
                    User_turn = (Random.Range(0, 4) <= 2) ? false : true;
                else
                    User_turn = (Random.Range(0, 4) > 2) ? false : true;
            }

            turno = User_turn ? "usuario" : "avatar";

            new_clip = false;
            api.GeneratePiecesResponseAndAudio(user_type, PieceName(piece_id), turno, ult_turno, resultado, ult_resultado, "INDICAR_TURNO_ACTUAL", onDone: (texto, clip) =>
            {
                Debug.Log("Texto del avatar: " + texto);

                if (clip != null)
                {
                    new_clip = true;
                    ch.Speak(clip);
                }
            });

            yield return new WaitUntil(() => new_clip);
            yield return SpeakWaitWithTouch(ch, () =>
            {
                if (touchedPiece) return true;           // ya colocada antes
                touchedPiece = CheckForPieces();         // comprobar ahora
                return touchedPiece;                      // parar si se acaba de colocar
            });

            if (touchedPiece)
            {
                ch.StopSpeaking();
                complete = true;
            }

            if (!User_turn)
            {

                bool use_right = pieces[piece_id].GetComponent<SimpleRespawn>().getSpawnPoint() != spawnPoints[2]; // Si la pieza est� en el spawn derecho

                if (!touchedPiece)
                {
                    yield return WaitWithTouch(3.0f, () =>
                    {
                        if (touchedPiece) return true;
                        touchedPiece = CheckForPieces();
                        return touchedPiece;
                    });

                    // 1) Mano hacia la pieza
                    if (use_right)
                        ch.MoveRightHand(pieces[piece_id].transform.position + piece_height_offset, 0.6f);
                    else
                        ch.MoveLeftHand(pieces[piece_id].transform.position + piece_height_offset, 0.6f);

                    yield return MoveWaitWithTouch(ch, () =>
                    {
                        if (touchedPiece) return true;
                        touchedPiece = CheckForPieces();
                        return touchedPiece;
                    });

                    yield return WaitWithTouch(1.5f, () =>
                    {
                        if (touchedPiece) return true;
                        touchedPiece = CheckForPieces();
                        return touchedPiece;
                    });
                }

                if (touchedPiece)
                {
                    ch.SetLeftHand(left_rest.transform);
                    ch.SetRightHand(right_rest.transform);
                    result = 0;
                }
                else
                {
                    if (use_right)
                        ch.MoveRightHand(right_rest.transform.position, 0.6f);
                    else
                        ch.MoveLeftHand(left_rest.transform.position, 0.6f);

                    result = 1;
                }
            }
            else
            {
                complete = false;
                float timer = 0f;

                while (!complete)
                {
                    timer += Time.deltaTime;

                    if (timer >= timeLimit)
                    {
                        result = 2;
                        complete = true;
                        break;
                    }
                if (timer >= timeLimit)
                {
                    result = 1; // Tiempo l�mite alcanzado
                    Debug.Log("Tiempo l�mite alcanzado. Tarea no completada.");
                    Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Tiempo Limite El Usuario no ha tocado la Pieza a tiempo");
                    complete = true;
                    break;
                }

                    for (int i = 0; i < pieces.Count; i++)
                    {
                        var piece = pieces[i];
                        if (piece == null) continue;

                        var simpleRespawn = piece.GetComponent<SimpleRespawn>();
                        if (simpleRespawn != null && simpleRespawn.IsBeingTouched())
                        {
                            complete = true;
                            result = (i == piece_id) ? 3 : 4;
                            break;
                        }
                    }
                            result = 2; // Tarea completada correctamente
                            Debug.Log("Tarea completada correctamente.");
                            Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Tarea completada correctamente");
                        }
                        else
                        {
                            complete = true;
                            result = 3; // Pieza incorrecta seleccionada
                            Debug.Log("Pieza incorrecta seleccionada.");
                            Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Pieza incorrecta Seleccionada");
                        }
                        break;
                    }
                }

                    yield return null; // �No bloquear! Espera al siguiente frame
                }
            }

            // Paso 5: Registrar resultados

            switch (result)
            {
                case 0:
                    Debug.Log("Resultado: Turno avatar interrumpido por el usuario.");
                    resultado = "ERROR_TURN";
                    break;
                case 1:
                    Debug.Log("Resultado: Esperando turno del usuario.");
                    resultado = "WAIT";
                    break;
                case 2:
                    Debug.Log("Resultado: Tiempo agotado.");
                    resultado = "OUT_OF_TIME";
                    break;
                case 3:
                    Debug.Log("Resultado: Correcto.");
                    resultado = "CORRECT";
                    break;
                case 4:
                    Debug.Log("Resultado: Pieza incorrecta.");
                    resultado = "WRONG_PIECE";
                    break;
                default:
                    Debug.Log("Resultado: Desconocido.");
                    resultado = "DESCONOCIDO";
                    break;
            }

            new_clip = false;
            api.GeneratePiecesResponseAndAudio(user_type, PieceName(piece_id), turno, ult_turno, resultado, ult_resultado, "FEEDBACK_RESULTADO", onDone: (texto, clip) =>
            if (result == 1 && user)
            {
                Debug.Log("Resultado: Tiempo l�mite alcanzado.");
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Tiempo Limite Alcanzado");
                ch.Speak("notice_objects_move");
            }
            else if (result == 1 && !user)
            {
                Debug.Log("Resultado: Tarea completada correctamente.");
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Tarea completada Exitosamente");
                ch.Speak("praise_wait_turn");
            }
            else if (!user && (result == 2 || result == 3))
            {
                Debug.Log("Texto del avatar: " + texto);

                if (clip != null)
                {
                    new_clip = true;
                    ch.Speak(clip);
                }
            });

            yield return new WaitUntil(() => new_clip);
            yield return new WaitUntil(() => !ch.isSpeaking());

            // Preparar para el siguiente trial
            ult_turno = turno;
            ult_resultado = resultado;
            current_trial++;
            last_piece_id = piece_id;
        }
    }

    // ----------------- Helpers -----------------

    private IEnumerator WaitWithTouch(float time, System.Func<bool> shouldStop)
    {
        float t = 0f;
        while (t < time)
        {
            if (shouldStop != null && shouldStop())
                yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }

    // Espera a que el personaje termine de moverse, comprobando si se ha tocado algo
    private IEnumerator MoveWaitWithTouch(Character ch, System.Func<bool> shouldStop)
    {
        const float hardTimeout = 10f;
        float t = 0f;

        // Salida inmediata si ya se cumpli� la condici�n antes de empezar
        if (shouldStop != null && shouldStop())
            yield break;

        while (ch.isMoving())
        {
            if (shouldStop != null && shouldStop())
                yield break;

            t += Time.deltaTime;
            if (t >= hardTimeout)
                Debug.Log("Resultado: Turno incorrecto.");
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Turno Incorrecto");
                ch.Speak("error_wait_turn");
            }
            else if (user && result == 2)
            {
                Debug.Log("Resultado: Tarea completada correctamente.");
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Tarea completada Exitosamente");
                ch.Speak("praise_good_job");
            }
            else if (user && result == 3)
            {
                Debug.LogWarning("Timeout de movimiento alcanzado.");
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator SpeakWaitWithTouch(Character ch, System.Func<bool> shouldStop)
    {
        const float hardTimeout = 10f;
        float t = 0f;

        // Salida inmediata si ya se cumpli� la condici�n antes de empezar
        if (shouldStop != null && shouldStop())
            yield break;
                Debug.Log("Resultado: Pieza incorrecta seleccionada.");
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Pieza Incorrecta");
                ch.Speak("error_wrong_object");
            }

        while (ch.isSpeaking())
        {
            if (shouldStop != null && shouldStop())
                yield break;

            t += Time.deltaTime;
            if (t >= hardTimeout)
            {
                Debug.LogWarning("Timeout de habla alcanzado.");
                yield break;
            }
            yield return null;
        }
    }
}