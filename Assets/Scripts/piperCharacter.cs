using Piper;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class piperCharacter : MonoBehaviour
{
    public PiperManager piper;
    public AudioSource audioSpeaker;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Speak("This is a test for Piper TTS in Unity");
    }
    private async void Speak(string text)
    {
        Debug.Log("Texto: " + text);

        AudioClip clip = await piper.TextToSpeechAsync(text);

        audioSpeaker.clip = clip;
        audioSpeaker.Play();

        Debug.Log(audioSpeaker.isPlaying);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Piper Manager: " + piper.ToString());
        Debug.Log("Audio Source: " + audioSpeaker.ToString());

        if (!audioSpeaker.isPlaying)
        {
            
        }
    }
}
