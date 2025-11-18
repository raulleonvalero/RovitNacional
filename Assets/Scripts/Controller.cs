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
        Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Experimento Iniciado");

        var ch = avatar.GetComponent<Character>();

        // Paso 1: Explicación del experimento
        ch.Speak("greeting_hello");

        Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Explicacion del experimento");

        while (ch.isSpeaking()) yield return null;

        yield return new WaitForSeconds(0.5f);

        ch.Speak("explain");

        while (ch.isSpeaking()) yield return null;

        yield return new WaitForSeconds(0.5f);

        while (true) // bucle de trials; si quieres número fijo, reemplaza por for (int t=0; t<T; t++)
        {
            SetRandomSpawns();

            // Paso 2: seleccionar usuario o avatar y una pieza aleatoria
            user = Random.Range(0, 2) == 1;                  // true = user, false = avatar
            piece_id = Random.Range(0, Mathf.Max(1, pieces.Count));

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
                if (piece_id == 0) ch.Speak("action_touch_cube_red_avatar");

                else if (piece_id == 1) ch.Speak("action_touch_sphere_blue_avatar");

                else if (piece_id == 2) ch.Speak("action_touch_cylinder_green_avatar");
            }

            while (ch.isSpeaking()) yield return null; // Espera hasta que termine de hablar

            // Paso 4: Detectar si la tarea se completa con límite de tiempo

            if (!user){

                yield return new WaitForSeconds(3f);

                lookAt(pieces[piece_id].transform);

                if (pieces[piece_id].GetComponent<SimpleRespawn>().getSpawnPoint() == spawnPoints[2]) // Si la pieza está en el spawn derecho
                {
                    ch.MoveLeftHand(pieces[piece_id].transform.position + piece_height_offset, 0.4f);
                }
                else
                {
                    ch.MoveRightHand(pieces[piece_id].transform.position + piece_height_offset, 0.4f);
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
                        if (i == piece_id && user)
                        {
                            complete = true;
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

                yield return null; // ¡No bloquear! Espera al siguiente frame
            }


            lookAt(look_target);

            if (!user)
            {
                if (pieces[piece_id].GetComponent<SimpleRespawn>().getSpawnPoint() == spawnPoints[2]) // Si la pieza está en el spawn derecho
                {
                    ch.MoveLeftHand(left_rest.transform.position, 0.6f);
                }
                else
                {
                    ch.MoveRightHand(right_rest.transform.position, 0.6f);
                }

            }

            yield return new WaitForSeconds(1.5f);

            ch.SetLeftHand(left_rest.transform);
            ch.SetRightHand(right_rest.transform);

            // Paso 5: Registrar resultados
            if (result == 1 && user)
            {
                Debug.Log("Resultado: Tiempo límite alcanzado.");
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
                Debug.Log("Resultado: Pieza incorrecta seleccionada.");
                Logging.WriteLog((int)Experimento.Actividad, (int)Experimento.Modo, "Pieza Incorrecta");
                ch.Speak("error_wrong_object");
            }

            while (ch.isSpeaking())
            {
                yield return null; // Espera hasta que termine de hablar
            }

            yield return new WaitForSeconds(2f);
        }
    }
}
