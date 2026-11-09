using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameStaminaController : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float baseMaxStamina = 100f;
    [SerializeField, Min(0f)] float baseDrainPerSecond = 1f;

    bool m_IsDepleted;
    PlayerStats m_PlayerStats;

    public float CurrentStamina { get; private set; }
    public float CurrentMaxStamina { get; private set; }
    public float CurrentDrainPerSecond { get; private set; }

    public event Action<float, float> StaminaChanged;
    public event Action<float> StaminaConsumed;
    public event Action StaminaDepleted;

    void Awake()
    {
        CurrentMaxStamina = baseMaxStamina;
        CurrentDrainPerSecond = baseDrainPerSecond;
        CurrentStamina = CurrentMaxStamina;
    }

    void Start()
    {
        m_PlayerStats = PlayerStats.Instance;
        m_PlayerStats.StatsChanged += RefreshStats;
        RefreshStats();
    }

    void OnDestroy()
    {
        if (m_PlayerStats != null)
            m_PlayerStats.StatsChanged -= RefreshStats;
    }

    void Update()
    {
        if (m_IsDepleted || !GameFlowManager.IsMainGame)
            return;

        var previousStamina = CurrentStamina;
        CurrentStamina = Mathf.Max(
            0f,
            CurrentStamina - CurrentDrainPerSecond * Time.deltaTime);
        var consumed = previousStamina - CurrentStamina;
        if (consumed > 0f)
            StaminaConsumed?.Invoke(consumed);
        StaminaChanged?.Invoke(CurrentStamina, CurrentMaxStamina);

        if (CurrentStamina > 0f)
            return;

        m_IsDepleted = true;
        StaminaDepleted?.Invoke();
    }

    public void BeginRun()
    {
        m_IsDepleted = false;
        CurrentStamina = CurrentMaxStamina;
        StaminaChanged?.Invoke(CurrentStamina, CurrentMaxStamina);
    }

    public void SetCurrentMaxStamina(float value)
    {
        CurrentMaxStamina = Mathf.Max(0.01f, value);
        CurrentStamina = Mathf.Min(CurrentStamina, CurrentMaxStamina);
        StaminaChanged?.Invoke(CurrentStamina, CurrentMaxStamina);
    }

    public void SetCurrentDrainPerSecond(float value)
    {
        CurrentDrainPerSecond = Mathf.Max(0f, value);
    }

    public void SetCurrentSettings(float maxStamina, float drainPerSecond)
    {
        CurrentMaxStamina = Mathf.Max(0.01f, maxStamina);
        CurrentDrainPerSecond = Mathf.Max(0f, drainPerSecond);
        CurrentStamina = Mathf.Min(CurrentStamina, CurrentMaxStamina);
        StaminaChanged?.Invoke(CurrentStamina, CurrentMaxStamina);
    }

    public void Restore(float amount)
    {
        if (amount <= 0f || CurrentStamina >= CurrentMaxStamina)
            return;

        CurrentStamina = Mathf.Min(CurrentMaxStamina, CurrentStamina + amount);
        StaminaChanged?.Invoke(CurrentStamina, CurrentMaxStamina);
    }

    void RefreshStats()
    {
        CurrentMaxStamina = Mathf.Max(
            0.01f,
            m_PlayerStats.CalculateMaxStamina(baseMaxStamina));
        CurrentDrainPerSecond = m_PlayerStats.CalculateStaminaDrain(
            baseDrainPerSecond);
        CurrentStamina = Mathf.Min(CurrentStamina, CurrentMaxStamina);
        StaminaChanged?.Invoke(CurrentStamina, CurrentMaxStamina);
    }
}
