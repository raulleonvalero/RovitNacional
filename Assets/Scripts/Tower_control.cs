using UnityEngine;
using System.Collections;             // <- Necesario para IEnumerator/Coroutines
using System.Collections.Generic;
using static Unity.Mathematics.math;
using RovitNacional;

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
    [SerializeField] private float timeLimit = 15f;
    [SerializeField] bool easy_mode = true;
    [SerializeField] string user_type = "TEA";
    private bool experimentRunning = false;
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


        //disable next piece at start
        next_piece.SetActive(false);
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
            if (easy_mode)
            {
                next_piece.SetActive(true);
            }
            StartCoroutine(ExperimentRoutine());
            experimentRunning = true;
        }
    }

    void Update()
    {
        
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

    public IEnumerator ExperimentRoutine()
    {
        Debug.Log("Experimento iniciado.");

        var ch = avatar.GetComponent<Character>();
        var api = apiController.GetComponent<OpenAIAvatarDirector>();
        int numPieces = avatar_pieces.Count;
        int result = 0;
        int altura = 0;
        string turno = "";
        string ult_turno = "";
        string resultado = string.Empty;
        bool new_clip = false;

        // Paso 1: Explicación del experimento
        new_clip = false;
        api.GenerarRespuestaYAudio(altura, turno, ult_turno, user_type, resultado, "EXPLICAR_EXPERIMENTO", onDone: (texto, clip) =>
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

        // Bucle principal: hasta tener 9 piezas colocadas
        while (pieces_id.Count < 9)
        {
            bool piezaColocada = false;
            bool turnoUsuario = Random.Range(0, 2) == 1;

            altura = pieces_id.Count + 1;
            turno = turnoUsuario ? "user" : "avatar";

            new_clip = false;
            api.GenerarRespuestaYAudio(altura, turno, ult_turno, user_type, resultado, "INDICAR_TURNO_ACTUAL", onDone: (texto, clip) =>
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

            if (!turnoUsuario)
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
                pieces_id.Add(randId + numPieces);

                // Volver a la pose de reposo
                if (usarDerecha)
                    ch.MoveRightHand(right_rest.transform.position, 0.6f);
                else
                    ch.MoveLeftHand(left_rest.transform.position, 0.6f);

                if (piezaColocada) 
                {
                    result = 0;
                    var piece_control = avatar_pieces[randId].GetComponent<Tower_Piece>();
                    piece_control.ClearAvatarHandPose();
                    piece_control.Respawn();

                    if (usarDerecha) ch.SetRightHand(right_rest.transform);
                    else ch.SetLeftHand(left_rest.transform);
                }
                else 
                {
                    result = 1;
                    PlaceAvatarPiece(randId);
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
                        Debug.Log("Tiempo límite alcanzado. Tarea no completada.");
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

            if (result == 1 && !turnoUsuario)
            {
                Debug.Log("Resultado: Turno avatar esperado.");
                resultado = "WAIT";
            }
            else if (result == 2 && turnoUsuario)
            {
                Debug.Log("Resultado: Tiempo límite alcanzado.");
                resultado = "OUT_OF_TIME";
            }
            else if (result == 3)
            {
                Debug.Log("Resultado: Tarea completada correctamente.");
                resultado = "CORRECT";
            }
            else if (result == 0)
            {
                Debug.Log("Resultado: Turno incorrecto.");
                resultado = "ERROR_TURN";
            }

            new_clip = false;
            api.GenerarRespuestaYAudio(altura, turno, ult_turno, user_type, resultado, "FEEDBACK_RESULTADO", onDone: (texto, clip) =>
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

            ult_turno = turno;
        }
    }

    // ----------------- Helpers -----------------

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