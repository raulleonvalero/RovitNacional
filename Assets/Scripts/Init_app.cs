using UnityEngine;
using RovitNacional;

using UnityEngine.SceneManagement;

public class Init_app : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Logging.createFile();
        Logging.WriteLog(0, "APP init");
    }

    public void OnLoadExperiment1()
    {
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
