// Assets/Scripts/Operator/OperatorHotkeys.cs
using UnityEngine;

public class OperatorHotkeys : MonoBehaviour
{
    ExperimentController ctrl;
    void Awake() => ctrl = FindObjectOfType<ExperimentController>(true);

    void Update()
    {
        if (!ctrl) return;
        if (Input.GetKeyDown(KeyCode.F5)) ctrl.Prepare();
        if (Input.GetKeyDown(KeyCode.F6)) ctrl.StartExperiment();
        if (Input.GetKeyDown(KeyCode.F7)) ctrl.TogglePause();
        if (Input.GetKeyDown(KeyCode.F8)) ctrl.NextTrial();
        if (Input.GetKeyDown(KeyCode.F12)) ctrl.Abort();
    }
}
