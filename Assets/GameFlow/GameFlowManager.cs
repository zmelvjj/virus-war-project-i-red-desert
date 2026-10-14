using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameFlowManager : MonoBehaviour
{
    [SerializeField] GameStaminaController staminaController;
    [SerializeField] GameFlowUIController uiController;

    bool m_HasState;

    public static GameFlowManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;
    public static bool IsMainGame =>
        HasInstance && Instance.CurrentState == GameFlowState.MainGame;

    public GameFlowState CurrentState { get; private set; }

    public event Action<GameFlowState> OnGameFlowStateChanged;
    public event Action OnRunRestarted;
    public event Action OnGameOver;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        staminaController.StaminaDepleted += HandleStaminaDepleted;
    }

    void Start()
    {
        staminaController.BeginRun();
        ChangeState(GameFlowState.MainGame);
    }

    void OnDisable()
    {
        staminaController.StaminaDepleted -= HandleStaminaDepleted;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ChangeState(GameFlowState state)
    {
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
        staminaController.BeginRun();
        OnRunRestarted?.Invoke();
        ChangeState(GameFlowState.MainGame);
    }

    void HandleStaminaDepleted()
    {
        if (CurrentState == GameFlowState.MainGame)
            ChangeState(GameFlowState.GameOver);
    }
}
