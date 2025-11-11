using UnityEngine;

public class cuboColision : MonoBehaviour
{
    bool instaciate = true;
    [SerializeField] private Vector3 origin;
    [SerializeField] private Quaternion rotation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        origin = transform.position;
        rotation = transform.rotation;

        GetComponent<Rigidbody>().isKinematic = false;
    }

    private float distance()
    {
        return (Vector3.Distance(transform.position, origin));
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(distance());
        if (instaciate && distance() > 0.1f)
        {
            Instantiate(gameObject, origin, rotation);
            instaciate = false;
        }
    }


    
}
