using UnityEngine;

public class multi_display : MonoBehaviour
{
    public Camera central;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.position = central.transform.position;
        gameObject.transform.rotation = central.transform.rotation;
    }
}
