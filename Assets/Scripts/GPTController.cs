using UnityEngine;
using System.Collections;


public class TestTextToAudio : MonoBehaviour
{
    [Header("GPTClient")]
    [SerializeField] private GameObject apiController;
    [SerializeField] private AudioSource audioSource;

    [Header("Avatar")]
    [SerializeField] private GameObject avatar;
    [SerializeField] private GameObject left_rest;
    [SerializeField] private GameObject right_rest;

    void Start()
    {
        var ch = avatar.GetComponent<Character>();

        ch.MoveLeftHand(left_rest.transform.position, 0.3f);
        ch.MoveRightHand(right_rest.transform.position, 0.3f);

        IEnumerator Example()
        {
            yield return new WaitForSeconds(2.0f);
            //Probar();
        }

        //wait 2 seconds
        StartCoroutine(Example());
    }



    public void Probar()
    {
        
    }
}