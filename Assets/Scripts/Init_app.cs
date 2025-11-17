using UnityEngine;
using RovitNacional;

using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;

public class Init_app : MonoBehaviour
{
    public GameObject[] NoDestroy;

    public static  TextMeshProUGUI output;
    public TextMeshProUGUI actividad;
    public TextMeshProUGUI modo;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TextMeshProUGUI[] textos = GameObject.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.InstanceID);
        foreach (var t in textos)
        {
            Debug.Log("Textos: " + t.name);
            if (t.name == "TextOut")
                output = t;
        }
        Logging.createFile();
        output.text += Logging.WriteLog(0, "APP init");

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

        if (Experimento.Actividad == Activity.GoStopGo)
            SceneManager.LoadScene("Experiment_1");
        if (Experimento.Actividad == Activity.BuldingTower)
            SceneManager.LoadScene("Experiment_2");

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
        
    }
}
