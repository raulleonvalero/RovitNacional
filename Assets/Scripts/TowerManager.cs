using System.Runtime.ExceptionServices;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    private bool firstCube;

    [SerializeField] private GameObject lastCube;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        firstCube = true;

        Debug.Log("TOWER: tower inicialized");
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("TOWER: Colision detected");
        if(collision.gameObject.layer == LayerMask.NameToLayer("cubos"))
        {
            Debug.Log("TOWER: Colision with cube");
            if (firstCube)
            {
                Debug.Log("TOWER: first cube colusion");
                lastCube = collision.gameObject;
                firstCube = false;
            }
            else
            {
                Debug.Log("TOWER: Ya hay un cubo");
            }
        }
    }

    public void putNextCube()
    {
        GameObject aux = Instantiate(lastCube,lastCube.transform.position + (Vector3.up * 0.11f), Quaternion.identity);
        lastCube = aux;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
