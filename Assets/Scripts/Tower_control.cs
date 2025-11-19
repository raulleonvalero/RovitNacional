using UnityEngine;
using System.Collections;             // <- Necesario para IEnumerator/Coroutines
using System.Collections.Generic;
using static Unity.Mathematics.math;
using RovitNacional;
using UnityEngine.SceneManagement;

public class Tower_control : MonoBehaviour
{

    [Header("Piezas y SpawnPoints")]
    [SerializeField] private List<GameObject> pieces = new List<GameObject>();
    [SerializeField] private List<GameObject> avatar_pieces = new List<GameObject>();

    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private List<Transform> avatar_spawnPoints = new List<Transform>();

    [SerializeField] private GameObject tower_base;
    [SerializeField] private GameObject next_piece;
    [SerializeField] private GameObject base_piece;

    [SerializeField] private Material placed;

    [Header("Avatar")]
    [SerializeField] private GameObject avatar;
    [SerializeField] private GameObject left_rest;
    [SerializeField] private GameObject right_rest;

    [Header("GPTClient")]
    [SerializeField] private GameObject apiController;
    [SerializeField] private AudioSource audioSource;

    // Parámetros del experimento 
    [Header("Experiment")]
    [SerializeField] private int AlturaMaxima = 8;
    [SerializeField] private float timeLimit = 15f;
    [SerializeField] bool easy_mode = true;
    [SerializeField] string user_type = "TEA";

    private bool complete = false;
    private List<int> pieces_id = new List<int>();
    private int result = 0;

    // Parametros auxiliares
    private Vector3 piece_height_offset = new Vector3(0, 0.1f, 0);
    private Vector3 piece_hand_offset = new Vector3(0.0f, 0.05f, 0.1f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var ch = avatar.GetComponent<Character>();

        ch.MoveLeftHand(left_rest.transform.position, 0.3f);
        ch.MoveRightHand(right_rest.transform.position, 0.3f);

        ch.setMode(Activity.BuldingTower, Mode.TEA);

        if (easy_mode)
        {
            next_piece.SetActive(true);
        }
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

    private bool OnNextPiece(Transform piece_transform)
    {
        bool pos_correct = IsPosCorrect(piece_transform.position);
        bool ori_correct = IsOriCorrect(piece_transform.rotation);
        return pos_correct && ori_correct;
    }

    private bool IsOriCorrect(Quaternion piece_rotation)
    {
        Vector3 euler_angles = piece_rotation.eulerAngles;

        float tolerance;

        if (easy_mode)
        {
            tolerance = 8.0f; // tolerancia en grados
        }
        else
        {
            tolerance = 3.0f; // tolerancia en grados
        }

        bool xOK = IsNearMultipleOf90(euler_angles.x, tolerance);
        bool zOK = IsNearMultipleOf90(euler_angles.z, tolerance);

        return xOK && zOK;
    }

    private bool IsNearMultipleOf90(float angle, float tolerance)
    {
        float rem = Mathf.Repeat(angle, 90f);          // resto respecto a 90
        float dist = Mathf.Min(rem, 90f - rem);        // distancia al múltiplo más cercano
        return dist <= tolerance;
    }

    private bool IsPosCorrect(Vector3 pos)
    {
        float x = pos.x;
        float y = pos.y;
        float z = pos.z;

        float next_x = next_piece.transform.position.x;
        float next_y = next_piece.transform.position.y;
        float next_z = next_piece.transform.position.z;

        if (pieces_id.Count == 0 && !easy_mode)
        {
            return abs(x - next_x) < 0.15f &&
                   abs(z - next_z) < 0.15f &&
                   abs(y - next_y) < 0.002f;
        }
        else if (easy_mode)
        {
            return abs(x - next_x) < 0.05f &&
               abs(z - next_z) < 0.05f &&
               abs(y - next_y) < 0.005f;
        }
        else
        {
            return abs(x - next_x) < 0.03f &&
               abs(z - next_z) < 0.03f &&
               abs(y - next_y) < 0.003f;
        }
    }

    private void PlacePiece(int i) 
    {
        pieces_id.Add(i);

        //Create a new base piece on the tower in the position of the placed piece
        GameObject new_base_piece = Instantiate(base_piece);
        new_base_piece.transform.position = new Vector3(pieces[i].transform.position.x, next_piece.transform.position.y, pieces[i].transform.position.z);
        Quaternion ori = pieces[i].transform.rotation;
        float y_value = ori.eulerAngles.y;
        new_base_piece.transform.rotation = Quaternion.Euler(0, y_value, 0);
        new_base_piece.GetComponent<Renderer>().material = placed;

        // mover la siguiente pieza encima
        next_piece.transform.position = pieces[i].transform.position + piece_height_offset;

        // desactivar la pieza colocada
        pieces[i].GetComponent<Tower_Piece>().Disable();
    }

    private void PlaceAvatarPiece(int i)
    {
        pieces_id.Add(i + avatar_pieces.Count); // ajustar el id para diferenciar de las piezas del usuario
        //Create a new base piece on the tower in the position of the placed piece
        GameObject new_base_piece = Instantiate(base_piece);
        new_base_piece.transform.position = avatar_pieces[i].transform.position;
        Quaternion ori = avatar_pieces[i].transform.rotation;
        float y_value = ori.eulerAngles.y;
        new_base_piece.transform.rotation = Quaternion.Euler(0, y_value, 0);
        new_base_piece.GetComponent<Renderer>().material = placed;

        // mover la siguiente pieza encima
        next_piece.transform.position = avatar_pieces[i].transform.position + piece_height_offset;

        // desactivar la pieza colocada
        avatar_pieces[i].GetComponent<Tower_Piece>().Disable();
    }

    private bool CheckForPieces()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null) continue;
            if (!pieces_id.Contains(i) && OnNextPiece(pieces[i].transform))
            {
                PlacePiece(i);
                return true;
            }
        }
        return false;
    }

    private string iteracionResult(int altura, int AlturaMaxima, string turno, string ult_turno, string resultado, string ult_resultado)
    {
        string result = "Altura " + altura + " de " + AlturaMaxima + ". ";
        result += "Turno: " + turno + ". ";
        result += "Último Turno: " + ult_turno + ". ";
        result += "Resultado: " + resultado + ". ";
        result += "Último Resultado: " + ult_resultado + ". ";
        return result;
    }

    public IEnumerator ExperimentRoutine()
    {
        Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Inicio Experimento Torre");

        var ch = avatar.GetComponent<Character>();
        var api = apiController.GetComponent<OpenAIAvatarDirector>();
        int numPieces = avatar_pieces.Count;
        int result = 0;
        int altura = 0;
        string turno = "";
        string ult_turno = "";
        string resultado = string.Empty;
        string ult_resultado = string.Empty;
        bool new_clip = false;

        // Paso 1: Explicación del experimento
        new_clip = false;
        api.GenerateTowerResponseAndAudio(altura, turno, ult_turno, user_type, resultado, ult_resultado, "EXPLICAR_EXPERIMENTO", onDone: (texto, clip) =>
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

        // Bucle principal: hasta tener 8 piezas colocadas
        while (pieces_id.Count < AlturaMaxima)
        {

            bool piezaColocada = false;
            bool User_turn;

            if (ult_resultado == "ERROR_TURN")
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
            api.GenerateTowerResponseAndAudio(altura, turno, ult_turno, user_type, resultado, ult_resultado, "INDICAR_TURNO_ACTUAL", onDone: (texto, clip) =>
            {
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Indicación de turno: " + texto);

                if (clip != null)
                {
                    new_clip = true;
                    ch.Speak(clip);
                }
            });
            
            yield return new WaitUntil(() => new_clip);
            yield return new WaitUntil(() => !ch.isSpeaking());

            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] != null)
                {
                    var piece_control = pieces[i].GetComponent<Tower_Piece>();
                    piece_control.Respawn();
                }
            }

            if (!User_turn)
            {
                // Turno del avatar (robot)
                yield return new WaitForSeconds(3f);

                // Elegir pieza aleatoria que no se haya usado
                int randId = PickUnusedPieceIndex(numPieces);
                if (randId == -1)
                {
                    Debug.LogWarning("No hay piezas disponibles.");
                    yield break;
                }

                bool usarDerecha = randId < 4; // si tu layout lo requiere

                // 1) Mano hacia la pieza
                if (usarDerecha)
                    ch.MoveRightHand(avatar_pieces[randId].transform.position + piece_hand_offset, 0.3f);
                else
                    ch.MoveLeftHand(avatar_pieces[randId].transform.position + piece_hand_offset, 0.3f);

                yield return MoveWaitWithPlacement(ch, () =>
                {
                    if (piezaColocada) return true;           // ya colocada antes
                    piezaColocada = CheckForPieces();         // comprobar ahora
                    return piezaColocada;                      // parar si se acaba de colocar
                });

                // 2) Mover pieza a destino
                if (usarDerecha)
                    ch.MovePieceRightHand(avatar_pieces[randId], next_piece.transform.position + piece_hand_offset, 0.3f);
                else
                    ch.MovePieceLeftHand(avatar_pieces[randId], next_piece.transform.position + piece_hand_offset, 0.3f);

                yield return MoveWaitWithPlacement(ch, () =>
                {
                    if (piezaColocada) return true;           // ya colocada antes
                    piezaColocada = CheckForPieces();         // comprobar ahora
                    return piezaColocada;                      // parar si se acaba de colocar
                });

                // Registrar pieza usada (ajuste si necesitas sumarle numPieces)
                if (!piezaColocada)
                {
                    PlaceAvatarPiece(randId);
                    result = 1;

                    // Volver a la pose de reposo
                    if (usarDerecha)
                        ch.MoveRightHand(right_rest.transform.position, 0.6f);
                    else
                        ch.MoveLeftHand(left_rest.transform.position, 0.6f);
                }
                else
                {
                    result = 0;
                    var piece_control = avatar_pieces[randId].GetComponent<Tower_Piece>();
                    piece_control.ClearAvatarHandPose();
                    piece_control.Respawn();

                    if (usarDerecha) ch.SetRightHand(right_rest.transform);
                    else ch.SetLeftHand(left_rest.transform);
                }
            }
            else
            {
                // Turno del usuario
                complete = false;
                float timer = 0f;

                while (!complete)
                {
                    timer += Time.deltaTime;

                    if (timer >= timeLimit)
                    {
                        result = 2; // si necesitas guardar el resultado
                        complete = true;
                        break;
                    }

                    if (CheckForPieces())
                    {
                        result = 3;
                        piezaColocada = true;
                        complete = true;
                        break;
                    }

                    yield return null;
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
                    Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Resultado: Cubo colocado correctamente.");
                    resultado = "CORRECT";
                    break;
                default:
                    Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Resultado: Desconocido.");
                    resultado = "DESCONOCIDO";
                    break;
            }

            altura = pieces_id.Count;

            new_clip = false;
            api.GenerateTowerResponseAndAudio(altura, turno, ult_turno, user_type, resultado, ult_resultado, "FEEDBACK_RESULTADO", onDone: (texto, clip) =>
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

            Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, iteracionResult(altura, AlturaMaxima, turno, ult_turno, resultado, ult_resultado));

            ult_turno = turno;
            ult_resultado = resultado;
        }

        // Fin del experimento
        Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Fin del experimento. Torre completada.");
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

    // Espera a que el personaje termine de moverse, comprobando colocación durante el trayecto
    private IEnumerator MoveWaitWithPlacement(Character ch, System.Func<bool> shouldStop)
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

    private IEnumerator SpeakWaitWithPlacement(Character ch, System.Func<bool> shouldStop)
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

    // Devuelve un índice de pieza no usada o -1 si no hay
    private int PickUnusedPieceIndex(int numPieces)
    {
        // Evita bucles infinitos con un número limitado de intentos
        const int maxAttempts = 100;
        for (int i = 0; i < maxAttempts; i++)
        {
            int candidate = Random.Range(0, avatar_pieces.Count);
            // Ajusta la condición según tu convención de ids
            if (!pieces_id.Contains(candidate + numPieces))
                return candidate;
        }
        return -1;
    }
}