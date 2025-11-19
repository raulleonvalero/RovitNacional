using System.Collections;             
using System.Collections.Generic;
using UnityEngine;
using RovitNacional;
using UnityEngine.SceneManagement;

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

    // Parámetros del experimento
    private int current_trial = 0;
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

        Logging.WriteLog((int)Activity.GoStopGo, -1, "Excena GoStopGo Iniciada");

        StartCoroutine(ExperimentRoutine());
        Variables.experimentRunning = true;
    }

    public void OnStartExperimentButtonPressed()
    {

    }

    void Update()
    {
        if (!Variables.experimentRunning)
        {
            StopAllCoroutines();
            SceneManager.LoadScene("MainScene");
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
                // Colocación directa si no hay SimpleRespawn
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

    private string iteracionResult(int current_trial, int num_trials, string turno, string ult_turno, string resultado, string ult_resultado)
    {
        string result = "Iteración " + current_trial + " de " + num_trials + ". ";
        result += "Turno: " + turno + ". ";
        result += "Último Turno: " + ult_turno + ". ";
        result += "Resultado: " + resultado + ". ";
        result += "Último Resultado: " + ult_resultado + ". ";
        return result;
    }

    IEnumerator ExperimentRoutine()
    {
        Debug.Log("Experimento iniciado.");
        Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Experimento Iniciado");

        var ch = avatar.GetComponent<Character>();
        var api = apiController.GetComponent<OpenAIAvatarDirector>();

        int result = 0;

        string turno = "";
        string ult_turno = "";

        string resultado = "";
        string ult_resultado = "";
        int last_piece_id = -1;

        bool new_clip = false;

        new_clip = false;
        api.GeneratePiecesResponseAndAudio(user_type, PieceName(piece_id), turno, ult_turno, resultado, ult_resultado, "EXPLICAR_EXPERIMENTO", onDone: (texto, clip) =>
        {
            Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Explicación del experimento: " + texto);

            if (clip != null)
            {
                new_clip = true;
                ch.Speak(clip);
            }
        });

        yield return new WaitUntil(() => new_clip);
        yield return new WaitUntil(() => !ch.isSpeaking());

        while (current_trial < num_trials) // bucle de trials; si quieres número fijo, reemplaza por for (int t=0; t<T; t++)
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
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Indicación de turno: " + texto);

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

                bool use_right = pieces[piece_id].GetComponent<SimpleRespawn>().getSpawnPoint() != spawnPoints[2]; // Si la pieza está en el spawn derecho

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

                    yield return null; // ¡No bloquear! Espera al siguiente frame
                }
            }

            // Paso 5: Registrar resultados

            switch (result)
            {
                case 0:
                    Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Resultado: turno avatar interrumpido.");
                    resultado = "ERROR_TURN";
                    break;
                case 1:
                    Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Resultado: turno avatar completado.");
                    resultado = "WAIT";
                    break;
                case 2:
                    Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Resultado: Tiempo agotado.");
                    resultado = "OUT_OF_TIME";
                    break;
                case 3:
                    Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Resultado: Pieza correcta.");
                    resultado = "CORRECT";
                    break;
                case 4:
                    Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Resultado: Pieza incorrecta.");
                    resultado = "WRONG_PIECE";
                    break;
                default:
                    Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Resultado: Error desconocido.");
                    resultado = "DESCONOCIDO";
                    break;
            }

            new_clip = false;
            api.GeneratePiecesResponseAndAudio(user_type, PieceName(piece_id), turno, ult_turno, resultado, ult_resultado, "FEEDBACK_RESULTADO", onDone: (texto, clip) =>
            {
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Feedback del resultado: " + texto);

                if (clip != null)
                {
                    new_clip = true;
                    ch.Speak(clip);
                }
            });

            yield return new WaitUntil(() => new_clip);
            yield return new WaitUntil(() => !ch.isSpeaking());

            Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Resumen: " + iteracionResult(current_trial + 1, num_trials, turno, ult_turno, resultado, ult_resultado));

            // Preparar para el siguiente trial
            ult_turno = turno;
            ult_resultado = resultado;
            current_trial++;
            last_piece_id = piece_id;
        }
        Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Experimento Finalizado");
        Debug.Log("Experimento finalizado.");
        Variables.experimentRunning = false;
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

        // Salida inmediata si ya se cumplió la condición antes de empezar
        if (shouldStop != null && shouldStop())
            yield break;

        while (ch.isMoving())
        {
            if (shouldStop != null && shouldStop())
                yield break;

            t += Time.deltaTime;
            if (t >= hardTimeout)
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

        // Salida inmediata si ya se cumplió la condición antes de empezar
        if (shouldStop != null && shouldStop())
            yield break;

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