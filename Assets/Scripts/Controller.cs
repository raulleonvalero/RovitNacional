using System.Collections;             // <- Necesario para IEnumerator/Coroutines
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [Header("Piezas y SpawnPoints")]
    [SerializeField] private List<GameObject> pieces = new List<GameObject>();
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Avatar")]
    [SerializeField] private GameObject avatar;
    [SerializeField] private GameObject left_rest;
    [SerializeField] private GameObject right_rest;

    [SerializeField] private GameObject head_look;
    [SerializeField] private Transform look_target;

    [Header("Experimento")]
    [SerializeField] private float timeLimit = 15f;

    // Parámetros del experimento 
    private bool experimentRunning = false;
    private bool user = false;
    private bool complete = false;
    private int piece_id = 0;
    private int result = 0;

    // Parametros auxiliares
    private Vector3 piece_height_offset = new Vector3(0, 0.1f, 0);

    void Start()
    {
        avatar.GetComponent<Character>().MoveLeftHand(left_rest.transform.position, 0.3f);
        avatar.GetComponent<Character>().MoveRightHand(right_rest.transform.position, 0.3f);

        lookAt(look_target);
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
                // Colocación directa si no hay SimpleRespawn
                piece.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }
        }
    }

    private void lookAt(Transform target)
    {
        //head_look.GetComponent<IKControl>().lookObj = target;
    }

    IEnumerator ExperimentRoutine()
    {
        Debug.Log("Experimento iniciado.");

        // Paso 1: Explicación del experimento
        avatar.GetComponent<Character>().Speak(0);

        while (avatar.GetComponent<Character>().isSpeaking())
        {
            yield return null; // Espera hasta que termine de hablar
        }

        yield return new WaitForSeconds(2f);

        while (true) // bucle de trials; si quieres número fijo, reemplaza por for (int t=0; t<T; t++)
        {
            SetRandomSpawns();

            // Paso 2: seleccionar usuario o avatar y una pieza aleatoria
            user = Random.Range(0, 2) == 1;                  // true = user, false = avatar
            piece_id = Random.Range(0, Mathf.Max(1, pieces.Count));


            if (user)
            {
                if (piece_id == 0)
                {
                    avatar.GetComponent<Character>().Speak(4); 
                }
                else if (piece_id == 1)
                {
                    avatar.GetComponent<Character>().Speak(5); 
                }
                else if (piece_id == 2)
                {
                    avatar.GetComponent<Character>().Speak(6);
                }
            }
            else
            {
                if (piece_id == 0)
                {
                    avatar.GetComponent<Character>().Speak(7); 
                }
                else if (piece_id == 1)
                {
                    avatar.GetComponent<Character>().Speak(8); 
                }
                else if (piece_id == 2)
                {
                    avatar.GetComponent<Character>().Speak(9); 
                }
            }

            while (avatar.GetComponent<Character>().isSpeaking())
            {
                yield return null; // Espera hasta que termine de hablar
            }

            // Paso 4: Detectar si la tarea se completa con límite de tiempo

            if (!user){

                lookAt(pieces[piece_id].transform);

                if (pieces[piece_id].GetComponent<SimpleRespawn>().getSpawnPoint() == spawnPoints[2]) // Si la pieza está en el spawn derecho
                {
                    avatar.GetComponent<Character>().MoveLeftHand(pieces[piece_id].transform.position + piece_height_offset, 0.3f);
                }
                else
                {
                    avatar.GetComponent<Character>().MoveRightHand(pieces[piece_id].transform.position + piece_height_offset, 0.3f);
                }
            }

            complete = false;
            result = 0;
            float timer = 0f;

            while (!complete)
            {
                timer += Time.deltaTime;

                if (timer >= timeLimit)
                {
                    result = 1; // Tiempo límite alcanzado
                    Debug.Log("Tiempo límite alcanzado. Tarea no completada.");
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
                        if (i == piece_id && user)
                        {
                            complete = true;
                            result = 2; // Tarea completada correctamente
                            Debug.Log("Tarea completada correctamente.");
                        }
                        else
                        {
                            complete = true;
                            result = 3; // Pieza incorrecta seleccionada
                            Debug.Log("Pieza incorrecta seleccionada.");
                        }
                        break;
                    }
                }

                yield return null; // ¡No bloquear! Espera al siguiente frame
            }

            if (!user)
            {
                lookAt(look_target);

                if (pieces[piece_id].GetComponent<SimpleRespawn>().getSpawnPoint() == spawnPoints[2]) // Si la pieza está en el spawn derecho
                {
                    avatar.GetComponent<Character>().MoveLeftHand(left_rest.transform.position, 0.5f);
                }
                else
                {

                    avatar.GetComponent<Character>().MoveRightHand(right_rest.transform.position, 0.5f);
                }
            }

            yield return new WaitForSeconds(1f);

            // Paso 5: Registrar resultados
            if (result == 1 && user)
            {
                Debug.Log("Resultado: Tiempo límite alcanzado.");
                avatar.GetComponent<Character>().Speak(1);
            }
            else if (result == 2 || (result == 1 && !user))
            {
                Debug.Log("Resultado: Tarea completada correctamente.");
                avatar.GetComponent<Character>().Speak(2);
            }
            else if (user && result == 3)
            {
                Debug.Log("Resultado: Pieza incorrecta seleccionada.");
                avatar.GetComponent<Character>().Speak(3);
            }
            else if (!user && (result == 2 || result == 3))
            {
                Debug.Log("Resultado: Turno incorrecto.");
                avatar.GetComponent<Character>().Speak(10);
            }

            while (avatar.GetComponent<Character>().isSpeaking())
            {
                yield return null; // Espera hasta que termine de hablar
            }

            yield return new WaitForSeconds(2f);
        }
    }
}
