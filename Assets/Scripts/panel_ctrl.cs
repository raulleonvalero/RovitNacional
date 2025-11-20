// Assets/Scripts/Operator/OperatorPanel.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OperatorPanel : MonoBehaviour
{
    [Header("Wiring")]
    public Button prepareBtn, startBtn, pauseBtn, nextBtn, abortBtn;
    public TMP_InputField seedInput, trialsInput;
    public TMP_Text stateText, trialText;

    private ExperimentController ctrl;

    void Start()
    {
        ctrl = FindObjectOfType<ExperimentController>(includeInactive: true);
        if (!ctrl)
        {
            Debug.LogError("No ExperimentController in scene.");
            enabled = false;
            return;
        }

        // Inicializa campos UI con valores actuales
        if (trialsInput) trialsInput.text = ctrl.GetTotalTrials().ToString();

        // Bind botones
        if (prepareBtn) prepareBtn.onClick.AddListener(ctrl.Prepare);
        if (startBtn) startBtn.onClick.AddListener(ctrl.StartExperiment);
        if (pauseBtn) pauseBtn.onClick.AddListener(ctrl.TogglePause);
        if (nextBtn) nextBtn.onClick.AddListener(ctrl.NextTrial);
        if (abortBtn) abortBtn.onClick.AddListener(ctrl.Abort);

        // Bind inputs
        if (seedInput) seedInput.onEndEdit.AddListener(v => { if (int.TryParse(v, out var s)) ctrl.SetSeed(s); });
        if (trialsInput) trialsInput.onEndEdit.AddListener(v =>
        {
            if (int.TryParse(v, out var t)) ctrl.SetTotalTrials(t);
            // re-escribe el texto para reflejar el valor clamp/actual
            trialsInput.text = ctrl.GetTotalTrials().ToString();
        });

        // Eventos de estado
        ctrl.OnStateChanged += OnStateChanged;
        ctrl.OnTrialChanged += i => { if (trialText) trialText.text = $"Trial: {i + 1}"; };

        // Pinta estado inicial
        OnStateChanged(ctrl.State);
    }

    private void OnDestroy()
    {
        if (!ctrl) return;
        ctrl.OnStateChanged -= OnStateChanged;
        ctrl.OnTrialChanged -= i => { };
    }

    private void OnStateChanged(ExpState s)
    {
        if (stateText) stateText.text = $"Estado: {s}";

        // Habilita/deshabilita botones según el estado
        SetInteractable(prepareBtn, s == ExpState.Idle || s == ExpState.Finished);
        SetInteractable(startBtn, s == ExpState.Ready);
        SetInteractable(pauseBtn, s == ExpState.Running || s == ExpState.Paused);
        SetInteractable(nextBtn, s == ExpState.Running);
        SetInteractable(abortBtn, s == ExpState.Running || s == ExpState.Paused);
    }

    private static void SetInteractable(Button b, bool v)
    {
        if (b) b.interactable = v;
    }
}
