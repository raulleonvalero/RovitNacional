using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

public class getAudio : MonoBehaviour
{

    InputAction sendAction;
    IEnumerator GenerateClip()
    {
        WWWForm form = new WWWForm();
        form.AddField("texto", "Esto es una prueba desde Unit. Mandando una request");

        using (UnityWebRequest www = UnityWebRequest.Post("http://127.0.0.1:5000//texto", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Debug.Log(www.result);

                StartCoroutine(GetAudioClip());
            }
        }
    }

    IEnumerator GetAudioClip()
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("http://127.0.0.1:5000/getAudio", AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                gameObject.GetComponent<AudioSource>().clip = myClip;
                gameObject.GetComponent<AudioSource>().Play();
            }
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sendAction = InputSystem.actions.FindAction("Jump");
    }

    // Update is called once per frame
    void Update()
    {
        bool tri = sendAction.triggered;
        bool jump = sendAction.ReadValue<float>() > 0f;
        bool send = sendAction.triggered && (sendAction.ReadValue<float>() > 0f);
        Debug.Log("trggered: " + tri);
        Debug.Log("Jump: " + jump);

        Debug.Log("Jump pressed: " + send);
        if (send)
            StartCoroutine(GenerateClip());
    }
}
