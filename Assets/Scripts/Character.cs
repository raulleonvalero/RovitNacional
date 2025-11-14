using Piper;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86;
using RovitNacional;
using System.Linq;

public class Character : MonoBehaviour
{
    [SerializeField] private GameObject rightObject;
    [SerializeField] private GameObject leftObject;
    [SerializeField] private GameObject test;

    private string path = "";

    private IEnumerator moveRightHand;
    private IEnumerator moveLeftHand;

    private Vector3 originPos;

    private AudioSource source;
    private PiperManager piper;

    private Animator anim;

    private int m_TalkStateHash;
    private bool initAnim = false;

    private int activeMoveCoroutines = 0;

    private static readonly string[] Frases = { "spanish0", "spanish1", "spanish2", "spanish3", "spanish4",
                                                "spanish5", "spanish6", "spanish7", "spanish8", "spanish9", "spanish10"};
    public void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    public void setMode(Activity actividad, Mode version)
    {

        switch (actividad)
        {
            case Activity.BuldingTower: path = "BuildTower/"; break;
            case Activity.GoStopGo: path = "GoStopGo/"; break;
        }
        switch (version)
        {
            case Mode.TEA: path += "TEA/"; break;
            case Mode.AC: path += "AC/"; break;
            case Mode.Down: path += "Down/"; break;
        }
    }


    public void Start()
    {
        anim = test.GetComponent<Animator>();
        m_TalkStateHash = Animator.StringToHash("Base Layer.test_Imported_4487785415424_TempMotion");
        anim.Play(m_TalkStateHash, 0, 1f);

        piper = GetComponentInChildren<PiperManager>();
        source = GetComponentInChildren<AudioSource>();
    }

    public void Update()
    {
        if (source.isPlaying && !initAnim)
        {
            initAnim = true;
            anim.Play(m_TalkStateHash, 0, 0.25f);
        }
        if (!source.isPlaying)
        {
            initAnim = false;
            anim.Play(m_TalkStateHash, 0, 1f);
        }
    }

    public void Speak(AudioClip clip)
    {
        source.Stop();
        Debug.Log("CLIP: " + clip.name);
        source.PlayOneShot(clip);
    }

    public void Speak(int i)
    {
        source.Stop();
        AudioClip clip = Resources.Load<AudioClip>("Sounds/spanish" + i);
        Debug.Log("CLIP: " + clip.name);
        source.PlayOneShot(clip);
    }

    public void Speak(string id)
    {
        source.Stop();

        // Cargar todos los clips dentro de Resources/Sounds/
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Sounds");

        // Filtrar los que contienen el id en el nombre
        var matchingClips = clips.Where(c => c.name.Contains(id)).ToList();

        if (matchingClips.Count == 0)
        {
            Debug.LogWarning("No se encontraron clips que contengan: " + id);
            return;
        }

        // Elegir uno aleatorio
        AudioClip randomClip = matchingClips[Random.Range(0, matchingClips.Count)];

        Debug.Log("Reproduciendo clip: " + randomClip.name);

        // Reproducirlo
        source.PlayOneShot(randomClip);
    }

    public bool isSpeaking()
    {
        return source.isPlaying;
    }

    private IEnumerator TrackMovement(IEnumerator routine)
    {
        activeMoveCoroutines++;
        yield return routine;            // espera a que termine la corutina real
        activeMoveCoroutines--;
        if (activeMoveCoroutines < 0)    // por seguridad ante StopAllCoroutines()
            activeMoveCoroutines = 0;
    }

    public bool isMoving()
    {
        return activeMoveCoroutines > 0;
    }

    public IEnumerator MoveAvatarHandTopDown(
        GameObject go,
        Vector3 endPoint,
        float speed = 0.1f,
        float overHeight = 0.15f,
        float hoverFraction = 0.6f,
        float stopDistance = 0.001f)
    {
        Transform tr = go.transform;
        float eps2 = stopDistance * stopDistance;

        Vector3 start = tr.position;

        Vector2 startXZ = new Vector2(start.x, start.z);
        Vector2 endXZ = new Vector2(endPoint.x, endPoint.z);
        Vector2 deltaXZ = endXZ - startXZ;
        float distXZ = deltaXZ.magnitude;

        float frac = Mathf.Clamp01(hoverFraction);
        if (distXZ > 0f)
            frac = Mathf.Clamp(frac, 0.01f, 0.99f);

        Vector2 hoverXZ = startXZ + deltaXZ * frac;
        float hoverY = Mathf.Max(start.y, endPoint.y + overHeight);
        Vector3 hoverPoint = new Vector3(hoverXZ.x, hoverY, hoverXZ.y);

        // Fase 1
        while ((tr.position - hoverPoint).sqrMagnitude > eps2)
        {
            tr.position = Vector3.MoveTowards(tr.position, hoverPoint, speed * Time.deltaTime);
            yield return null;
        }

        // Fase 2
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
        StartCoroutine(TrackMovement(moveRightHand));
    }

    public void MoveLeftHand(Vector3 target, float speed = 0.08f)
    {
        moveLeftHand = MoveAvatarHandTopDown(leftObject, target, speed);
        StartCoroutine(TrackMovement(moveLeftHand));
    }

    public void MovePieceLeftHand(GameObject obj, Vector3 target, float speed = 0.08f)
    {
        obj.GetComponent<Tower_Piece>().SetAvatarHandPose(leftObject);
        moveLeftHand = MoveAvatarHandTopDown(leftObject, target, speed);
        StartCoroutine(TrackMovement(moveLeftHand));
    }

    public void MovePieceRightHand(GameObject obj, Vector3 target, float speed = 0.08f)
    {
        obj.GetComponent<Tower_Piece>().SetAvatarHandPose(rightObject);
        moveRightHand = MoveAvatarHandTopDown(rightObject, target, speed);
        StartCoroutine(TrackMovement(moveRightHand));
    }

    public void SetLeftHand(Transform target)
    {
        leftObject.transform.position = target.position;
        StopAllCoroutines();
        activeMoveCoroutines = 0;
        //leftObject.transform.rotation = target.rotation;
    }

    public void SetRightHand(Transform target)
    {
        rightObject.transform.position = target.position;
        StopAllCoroutines();
        activeMoveCoroutines = 0;
        //rightObject.transform.rotation = target.rotation;
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