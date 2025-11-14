using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;

public class Init_exp : MonoBehaviour
{
    public TextMeshProUGUI Actividad;
    public TextMeshProUGUI Modo;

    private InputAction jumpAction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStartButton()
    {
        string actividad = Actividad.text;

        Debug.Log("Activity: " + actividad);
    }
}
