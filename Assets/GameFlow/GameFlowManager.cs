using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameFlowManager : MonoBehaviour
{
    [SerializeField] GameStaminaController staminaController;
    [SerializeField] GameFlowUIController uiController;

    bool m_HasState;
    bool m_IsPaused;
    float m_TimeScaleBeforePause = 1f;

    public static GameFlowManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;
    public static bool IsPaused => HasInstance && Instance.m_IsPaused;
    public static bool IsMainGameState =>
        HasInstance
        && Instance.m_HasState
        && Instance.CurrentState == GameFlowState.MainGame;
    public static bool IsMainGame =>
        IsMainGameState && !IsPaused;

    public GameFlowState CurrentState { get; private set; }
    public GameStaminaController StaminaController => staminaController;

    public event Action<GameFlowState> OnGameFlowStateChanged;
    public event Action OnRunRestarted;
    public event Action OnGameOver;
    public event Action<bool> OnPauseChanged;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        staminaController.StaminaDepleted += HandleStaminaDepleted;
    }

    IEnumerator Start()
    {
        if (RunManager.HasInstance)
            yield return RunManager.Instance.Initialize();

        staminaController.BeginRun();
        ChangeState(GameFlowState.MainGame);
        if (RunManager.HasInstance)
            RunManager.Instance.BeginPreparedRun();
    }

    void OnDisable()
    {
        staminaController.StaminaDepleted -= HandleStaminaDepleted;
    }

    void OnDestroy()
    {
        if (m_IsPaused)
            Time.timeScale = m_TimeScaleBeforePause;

        if (Instance == this)
            Instance = null;
    }

    public void SetPaused(bool paused)
    {
        if (m_IsPaused == paused)
            return;

        m_IsPaused = paused;
        if (paused)
        {
            m_TimeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = m_TimeScaleBeforePause;
        }

        OnPauseChanged?.Invoke(paused);
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void ChangeState(GameFlowState state)
    {
        if (state != GameFlowState.MainGame && m_IsPaused)
            SetPaused(false);

        if (m_HasState && CurrentState == state)
            return;

        m_HasState = true;
        CurrentState = state;
        uiController.ApplyState(state);
        OnGameFlowStateChanged?.Invoke(state);

        if (state == GameFlowState.GameOver)
            OnGameOver?.Invoke();
    }

    public void OpenUpgrade()
    {
        ChangeState(GameFlowState.Upgrade);
    }

    public void RestartRun()
    {
        SetPaused(false);
        if (RunManager.HasInstance)
            RunManager.Instance.PrepareNextRun();

        staminaController.BeginRun();
        OnRunRestarted?.Invoke();
        ChangeState(GameFlowState.MainGame);
        if (RunManager.HasInstance)
            RunManager.Instance.BeginPreparedRun();
    }

    public void EndRun()
    {
        if (CurrentState != GameFlowState.MainGame)
            return;

        if (RunManager.HasInstance)
            RunManager.Instance.NotifyRunEnded();

        ChangeState(GameFlowState.GameOver);
    }

    void HandleStaminaDepleted()
    {
        if (CurrentState == GameFlowState.MainGame)
            EndRun();
    }
}
