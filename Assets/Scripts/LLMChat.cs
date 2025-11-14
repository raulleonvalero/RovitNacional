using Abuksigun.Piper;
using LLMUnity;
using Meta.WitAi.TTS.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using static Abuksigun.Piper.PiperLib;

public class LLMChat : MonoBehaviour
{
    public LLMCharacter llmCharater;
    public TextMeshProUGUI input;
    public TextMeshProUGUI output;

    [SerializeField] string modelPath;
    [SerializeField] string espeakDataPath;

    public AudioSource audioSpeaker;
    private int characters = 0;
    private string _las_reply = "";
    private Queue<string> lista;
    Piper piper;
    PiperVoice voice;
    PiperSpeaker piperSpeaker;

    private InputAction jumpAction;

    bool isIn = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lista = new Queue<string>();

        AsyncStart();
        
    }
    private async void AsyncStart()
    {
        string fullModelPath = Path.Join(Application.streamingAssetsPath, modelPath);
        string fullEspeakDataPath = Path.Join(Application.streamingAssetsPath, espeakDataPath);

        piper ??= await Piper.LoadPiper(fullEspeakDataPath);
        voice ??= await PiperVoice.LoadPiperVoice(piper, fullModelPath);
        piperSpeaker ??= new PiperSpeaker(voice);
    }

    private void Speak(string text)
    {
        string fullModelPath = Path.Join(Application.streamingAssetsPath, modelPath);
        string fullEspeakDataPath = Path.Join(Application.streamingAssetsPath, espeakDataPath);

        
        _ = piperSpeaker.ContinueSpeach(text).ContinueWith(x => Debug.Log($"Generation finished with status: {x.Status}"));
        audioSpeaker.clip = piperSpeaker.AudioClip;
        //audioSpeaker.loop = true;
        Debug.Log("Texto: " + text);
        audioSpeaker.Play();
        /*

        AudioClip clip = await piper.TextToSpeechAsync(text);

        audioSpeaker.clip = clip;
        audioSpeaker.Play();
        */
        Debug.Log(audioSpeaker.isPlaying);
    }

    void HandleReply(string reply)
    {
        // do something with the reply from the model

        UnityEngine.Debug.Log(characters);
        reply.TrimEnd('\r', '\n');
        _las_reply = reply.Substring(characters);
        /*
        Debug.Log(reply.Substring(characters));*/

        //speaker.SpeakQueued(reply);
        UnityEngine.Debug.Log("char '.' : " + reply.IndexOf('*'));

        int index = _las_reply.IndexOf('.');
        if (index == -1)
            index = _las_reply.IndexOf('!');
        if (index == -1)
            index = _las_reply.IndexOf('?');
        if (index == -1)
            index = _las_reply.IndexOf(':');



        if (index != -1)
        {
            string aux = reply.Substring(characters, index + 1);
            UnityEngine.Debug.Log("speaker: " + aux);
            //speaker.SpeakQueued(aux);
            characters += index + 1;
            Speak(aux);
            output.text += aux;
        }
        UnityEngine.Debug.Log("reply: " + reply);
        //Debug.Log(_las_reply);
    }

    public async void SpeakQueue(string text)
    {
        string aux;
        lista.Enqueue(text);
        if (isIn) return;
        aux = lista.Dequeue();
        while(aux != null)
        {
            Debug.Log("Cola: " + aux);
            isIn = true;
            //await Speak(aux);
            aux = lista.Dequeue();
        }
        isIn = false;
    }

    public void OnButtonChat()
    {
        _ = llmCharater.Chat(input.text, HandleReply);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
