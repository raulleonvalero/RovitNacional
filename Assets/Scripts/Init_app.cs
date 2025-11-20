using RovitNacional;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Init_app : MonoBehaviour
{
    public GameObject[] NoDestroy;

    public TextMeshProUGUI output;

    public TextMeshProUGUI actividad;
    public TextMeshProUGUI modo;
    public TextMeshProUGUI Nombre;
    public UnityEngine.UI.Slider NTurnos;
    public UnityEngine.UI.Slider Scale;
    public UnityEngine.UI.Slider TimeLimit;
    public TextMeshProUGUI labelScrollTime;
    public TextMeshProUGUI labelScrollTurns;
    public TextMeshProUGUI labelScrollScale;

    public ScrollRect scrollView;

    private bool inExperiment = false; //TODO Cambiar por Variables.isRunning
    private bool firstTime = true;

    public RecenterFromScript rec;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        TextMeshProUGUI[] textos = GameObject.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.InstanceID);
        foreach (var t in textos)
        {
            Debug.Log("Textos: " + t.name);
            if (t.name == "Content")
                output = t;
        }

        //Logging.createFile();
        //Logging.WriteLog(-1,-1, "APP init");

        foreach(GameObject gb in NoDestroy)
        {
            DontDestroyOnLoad(gb);
        }
    }

    public void OnButtonStartClick()
    {

        Debug.Log("Actividad " + actividad.text + "\t" + modo.text);
        switch (actividad.text)
        {
            case "Construir Torre": Experimento.Actividad = Activity.BuldingTower; break;
            case "Go Stop Go": Experimento.Actividad = Activity.GoStopGo; break;
        }

        switch (modo.text)
        {
            case "TEA": Experimento.Modo = Mode.TEA; break;
            case "Sindrome de Down": Experimento.Modo = Mode.Down; break;
            case "Altas Capacidades": Experimento.Modo = Mode.AC; break;
        }

        Experimento.Nombre = Nombre.text;

        if (firstTime) Logging.createFile(Nombre.text);
        firstTime = false;

        Experimento.NTrunos = (int) NTurnos.value;
        Experimento.Scale = Scale.value;
        Experimento.TimeLimit = TimeLimit.value;

        Logging.WriteLog(-1,-1, "Cargando Exeprimento");
        inExperiment = true;
        if (Experimento.Actividad == Activity.GoStopGo)
            SceneManager.LoadScene("Experiment_1");
        if (Experimento.Actividad == Activity.BuldingTower)
            SceneManager.LoadScene("Experiment_2");
    }

    public void OnButtonQuitClick()
    {
        rec.RecenterUser();

        if (inExperiment)
        {
            SceneManager.LoadScene("MainScene");
            inExperiment = false;
        }
        else
        {
            Debug.Log("Exit . . .");
            Application.Quit();
        }
    }

    public void OnButtonCenterClick()
    {
        Debug.Log("Recentering user...");
    }

    public void OnLoadExperiment1()
    {
        Experimento.Actividad = Activity.GoStopGo;
       
        SceneManager.LoadScene("Experiment_1");
    }
    public void OnLoadExperiment2()
    {
        SceneManager.LoadScene("Experiment_2");
    }

    // Update is called once per frame
    void Update()
    {
        output.text = Variables.logOutput;
        scrollView.normalizedPosition = new Vector2(0, 0);

        labelScrollTurns.text = ((int)NTurnos.value).ToString();
        labelScrollScale.text = Scale.value.ToString("F2");
        labelScrollTime.text = TimeLimit.value.ToString("F0") + "s";

        Experimento.Scale = Scale.value;
        Debug.Log("[Scalar] Scale: " + Experimento.Scale);
    }
}
