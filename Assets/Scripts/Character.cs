using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;


public class Character : MonoBehaviour
{
    GameObject rightObject;
    GameObject leftObject;

    private IEnumerator moveRightHand;
    private IEnumerator moveLeftHand;

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
    }
    public void Update()
    {

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
        moveRightHand = MoveAvatarHand(rightObject, target, speed);
        StartCoroutine(moveRightHand);
    }

    public void MoveLeftHand(Vector3 target, float speed = 0.08f)
    {
        moveLeftHand = MoveAvatarHand(leftObject, target, speed);
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
