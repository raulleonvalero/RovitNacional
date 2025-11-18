using UnityEngine;

public class multi_display : MonoBehaviour
{
    public GameObject central;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

        central = GameObject.Find("CenterEyeAnchor");
        transform.position = central.transform.position;
        transform.rotation = central.transform.rotation;
    }
}
