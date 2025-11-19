// Assets/Scripts/Experiment/ExperimentController.cs
using UnityEngine;
using System;

public enum ExpState { Idle, Ready, Running, Paused, Finished, Aborted }

public class ExperimentController : MonoBehaviour
{

    public ExpState State { get; private set; } = ExpState.Idle;

    // Eventos para sincronizar UI/logs
    public event Action<ExpState> OnStateChanged;
    public event Action<int> OnTrialChanged;

    [Header("Trials")]
    [SerializeField] int totalTrials = 20;
    int currentTrial = -1;

    [Header("Randomness")]
    [SerializeField] int seed = 12345;
    System.Random rng;

    void Awake() => rng = new System.Random(seed);

    public void SetSeed(int newSeed)
    {
        seed = newSeed;
        rng = new System.Random(seed);
    }

    public void Prepare()
    {
        if (State != ExpState.Idle && State != ExpState.Finished) return;
        currentTrial = -1;
        ChangeState(ExpState.Ready);
    }

    public void StartExperiment()
    {
        if (State != ExpState.Ready) return;
        ChangeState(ExpState.Running);
        NextTrial();
    }

    public void TogglePause()
    {
        if (State == ExpState.Running) ChangeState(ExpState.Paused);
        else if (State == ExpState.Paused) ChangeState(ExpState.Running);
    }

    public void NextTrial()
    {
        if (State != ExpState.Running) return;
        currentTrial++;
        if (currentTrial >= totalTrials) { Finish(); return; }
        OnTrialChanged?.Invoke(currentTrial);

        // TODO: lanza estímulos, condiciones, timers, etc.
        // Puedes usar rng para contrabalanceo/aleatoriedad.
    }

    public void Abort()
    {
        // Limpieza segura
        ChangeState(ExpState.Aborted);
    }

    void Finish() => ChangeState(ExpState.Finished);

    void ChangeState(ExpState s)
    {
        State = s;
        OnStateChanged?.Invoke(State);
        // (Opcional) Time.timeScale = State == ExpState.Paused ? 0 : 1;
    }

    public void SetTotalTrials(int t) { totalTrials = Mathf.Max(1, t); }
    public int GetTotalTrials() { return totalTrials; }
}
