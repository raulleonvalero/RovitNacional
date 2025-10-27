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

    [Header("Experimento")]
    [SerializeField] private float timeLimit = 15f;

    // Parámetros del experimento (puedes dejarlos si quieres ver estado global)
    private bool user = false;
    private bool complete = false;
    private int piece_id = 0;
    private int result = 0;

    void Start()
    {
        avatar.GetComponent<Character>().MoveLeftHand(left_rest.transform.position, 0.3f);
        avatar.GetComponent<Character>().MoveRightHand(right_rest.transform.position, 0.3f);

        StartCoroutine(ExperimentRoutine());
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

    IEnumerator ExperimentRoutine()
    {
        Debug.Log("Experimento iniciado.");

        while (true) // bucle de trials; si quieres número fijo, reemplaza por for (int t=0; t<T; t++)
        {

            SetRandomSpawns();

            // Paso 1: Explicación del experimento
            // avatar?.GetComponent<Speack>()?.Speack();

            // Paso 2: seleccionar usuario o avatar y una pieza aleatoria
            user = Random.Range(0, 2) == 1;                  // true = user, false = avatar
            piece_id = Random.Range(0, Mathf.Max(1, pieces.Count));

            // Paso 3: Explicar la tarea
            //avatar?.GetComponent<Speack>()?.Speack();

            // Paso 4: Detectar si la tarea se completa con límite de tiempo
            
            if (!user){
                Vector3 target = pieces[piece_id].transform.position;
                avatar.GetComponent<Character>().MoveRightHand(target, 0.3f);
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

            // Paso 5: Registrar resultados
            // avatar?.GetComponent<Speack>()?.Speack();

            if (!user)
            {
                avatar.GetComponent<Character>().MoveRightHand(right_rest.transform.position, 0.3f);
            }

            yield return new WaitForSeconds(3f);

        }
    }
}
