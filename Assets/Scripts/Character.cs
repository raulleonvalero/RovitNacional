using Piper;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86;

public class Character : MonoBehaviour
{
    private GameObject rightObject;
    private GameObject leftObject;

    private IEnumerator moveRightHand;
    private IEnumerator moveLeftHand;

    private Vector3 originPos;

    private AudioSource source;
    private PiperManager piper;

    private static readonly string[] Frases = { "spanish0", "spanish1", "spanish2", "spanish3", "spanish4", 
                                                "spanish5", "spanish6", "spanish7", "spanish8", "spanish9", "spanish10"};
    public void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    public void Start()
    {
        int child = transform.childCount;
        for (int i = 0; i < child; i++)
        {
            var aux = transform.GetChild(i);
            if (aux.name == "Right")
            {
                rightObject = aux.gameObject;
            }
            if (aux.name == "Left")
            {
                leftObject = aux.gameObject;
            }
        }

        piper = GetComponentInChildren<PiperManager>();
        source = GetComponentInChildren<AudioSource>();
    }
    public void Update()
    {

    }

    /*public async void Speak(string text)
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        var audio = piper.TextToSpeechAsync(text);

        source.Stop();
        if (source && source.clip)
            Destroy(source.clip);

        source.clip = await audio;
        source.Play();
    }*/
    public void Speak(int i)
    {
        source.Stop();

        /*var clip = "Sounds/" + Frases[i] + ".wav";
        if (source && source.clip)
            Destroy(source.clip);
        */
        AudioClip clip = Resources.Load<AudioClip>("Sounds/spanish" + i);
        Debug.Log("CLIP: " + clip.name);
        //source.clip = clip;

        source.PlayOneShot(clip);
    }

    public bool isSpeaking()
    {
        return source.isPlaying;
    }

    public IEnumerator MoveAvatarHandTopDown(
    GameObject go,
    Vector3 endPoint,
    float speed = 0.1f,
    float overHeight = 0.15f,   // altura extra por encima del objetivo para iniciar la bajada
    float hoverFraction = 0.6f,  // 0..1: porcentaje del recorrido XZ desde start hacia el objetivo
    float stopDistance = 0.001f) // tolerancia de llegada
    {
        Transform tr = go.transform;
        float eps2 = stopDistance * stopDistance;

        Vector3 start = tr.position;

        // Punto de hover proyectado SOBRE EL SEGMENTO start->end en XZ (entre ambos)
        Vector2 startXZ = new Vector2(start.x, start.z);
        Vector2 endXZ = new Vector2(endPoint.x, endPoint.z);
        Vector2 deltaXZ = endXZ - startXZ;
        float distXZ = deltaXZ.magnitude;

        float frac = Mathf.Clamp01(hoverFraction);
        if (distXZ > 0f)
            frac = Mathf.Clamp(frac, 0.01f, 0.99f); // asegurar que queda "entre", no en los extremos

        Vector2 hoverXZ = startXZ + deltaXZ * frac;
        float hoverY = Mathf.Max(start.y, endPoint.y + overHeight);
        Vector3 hoverPoint = new Vector3(hoverXZ.x, hoverY, hoverXZ.y);

        // Fase 1: ir al punto intermedio elevado (entre start y end en XZ)
        while ((tr.position - hoverPoint).sqrMagnitude > eps2)
        {
            tr.position = Vector3.MoveTowards(tr.position, hoverPoint, speed * Time.deltaTime);
            yield return null;
        }

        // Fase 2: bajada en DIAGONAL desde el hoverPoint directamente hasta el endPoint
        while ((tr.position - endPoint).sqrMagnitude > eps2)
        {
            tr.position = Vector3.MoveTowards(tr.position, endPoint, speed * Time.deltaTime);
            yield return null;
        }

        tr.position = endPoint;
    }



    public IEnumerator MoveAvatarHand(GameObject gameObject, Vector3 endPoint, float speed = 0.1f)
    {
        while (!(gameObject.transform.position == endPoint))
        {
            gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, endPoint, speed * Time.deltaTime);
            yield return null;
        }
        yield return null;
    }

    public void MoveRightHand(Vector3 target, float speed = 0.08f)
    {
        moveRightHand = MoveAvatarHandTopDown(rightObject, target, speed);
        StartCoroutine(moveRightHand);
    }

    public void MoveLeftHand(Vector3 target, float speed = 0.08f)
    {
        moveLeftHand = MoveAvatarHandTopDown(leftObject, target, speed);
        StartCoroutine(moveLeftHand);
    }
}

/*
public class MoveObject : MonoBehaviour
{
    public GameObject rightObject;
    public GameObject leftObject;

    private float speed = 0.5f;

    private Vector3 rightStartPosition;
    private Vector3 leftStartPosition;

    public Transform goalPosition;// = new Vector3(-0.372000009f, 1.86300004f, -3.86800003f);

    private Vector3 goal;

    private Vector3[] allPositions;
    public Transform offset;// = new Vector3(-0.372000009f, 2.10899997f, -4.11499977f);
    private int counter;
    private const float DistanceToTarget = 0.1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static Vector3[] GetQuadraticBezierPoints(Vector3 startpoint, Vector3 endPoint, float curveHeigh)
    {
        Vector3 heighPoint = startpoint + (endPoint - startpoint) / 2 + Vector3.up * curveHeigh;

        Vector3[] res = new Vector3[100];
        int maxT = 1;
        int index = 0;

        for (float t = 0; t <= maxT; t += 0.01f)
        {
            Vector3 newPoint = (Mathf.Pow(1 - t, 2) * startpoint) + (2 * (1 - t) * t * heighPoint) + (t * t * endPoint);
            try
            {
                res[index] = newPoint;
                var aux = CreateSphere(Color.white);
                aux.transform.position = newPoint;
                Debug.Log("Index: " + index + " Point: " + newPoint);
                index++;
            }
            catch
            {
                break;
            }
        }
        return res;
    }

    public static Renderer CreateSphere(Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var render = sphere.GetComponent<Renderer>();
        render.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        render.material.color = color;


        return render;
    }
    void Start()
    {
        counter = 0;

        rightStartPosition = rightObject.transform.position;
        leftStartPosition = leftObject.transform.position;

        goal = goalPosition.position;
        allPositions = GetQuadraticBezierPoints(rightStartPosition, goalPosition.position, 0.1f);

        print("Points:: " + allPositions.ToString());
    }

    private bool move(GameObject gameObject, Vector3 endPoint)
    {
        Vector3 test = Vector3.Lerp(gameObject.transform.position, endPoint, speed);
        gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, endPoint, speed * Time.deltaTime);
        //gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, test, speed * Time.deltaTime);
        if (gameObject.transform.position == endPoint)
            return true;
        else
            return false;

        
    }

    public IEnumerator moveHand(GameObject gameObject, Vector3 endPoint)
    {
        while(!(gameObject.transform.position == endPoint))
        {
            gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, endPoint, speed * Time.deltaTime);
            return null;
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        if (move(rightObject, goal))
            goal = rightStartPosition;
        if (rightObject.transform.position == rightStartPosition)
            goal = goalPosition.position;
        print("Counter: " + counter);
        print("allPoint: " + allPositions.Length);

        /*
        if (counter < allPositions.Length)
        {
            rightObject.transform.position = Vector3.MoveTowards(rightObject.transform.position, allPositions[counter], Time.deltaTime);
            if (Vector3.Distance(rightObject.transform.position, allPositions[counter]) < DistanceToTarget) counter++;

        }
    }
}
        */
