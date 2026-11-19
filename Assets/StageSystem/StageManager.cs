using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageManager : MonoBehaviour
{
    readonly List<StageData> m_Stages = new();

    StageAddressableLoader m_Loader;
    MonsterManager m_MonsterManager;
    GameFlowManager m_GameFlowManager;
    int m_CurrentStageListIndex = -1;
    bool m_IsInitialized;

    public static StageManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    public int CurrentStageIndex => CurrentStageData != null
        ? CurrentStageData.StageIndex
        : -1;
    public int CurrentKillCount { get; private set; }
    public StageData CurrentStageData => m_CurrentStageListIndex >= 0
        && m_CurrentStageListIndex < m_Stages.Count
            ? m_Stages[m_CurrentStageListIndex]
            : null;
    public StageData NextStageData => m_CurrentStageListIndex >= 0
        && m_CurrentStageListIndex + 1 < m_Stages.Count
            ? m_Stages[m_CurrentStageListIndex + 1]
            : null;
    public int NextRequiredKillCount => NextStageData != null
        ? NextStageData.RequiredKillCount
        : 0;

    public event Action<StageData> OnStageChanged;
    public event Action<int, int> OnStageProgressChanged;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        AttachRuntimeSystems();
    }

    IEnumerator Start()
    {
        AttachRuntimeSystems();
        m_Loader = new StageAddressableLoader();
        yield return m_Loader.LoadAll(m_Stages);

        if (!m_Loader.Succeeded)
        {
            Debug.LogError("[StageManager] Addressables StageData 로드에 실패했습니다.", this);
            yield break;
        }

        if (!ValidateAndSortStages())
            yield break;

        m_IsInitialized = true;
        ResetRun();
    }

    void OnDisable()
    {
        DetachRuntimeSystems();
    }

    void OnDestroy()
    {
        m_Loader?.Release();
        if (Instance == this)
            Instance = null;
    }

    public void ResetRun()
    {
        if (!m_IsInitialized)
            return;

        m_CurrentStageListIndex = 0;
        CurrentKillCount = 0;
        OnStageChanged?.Invoke(CurrentStageData);
        OnStageProgressChanged?.Invoke(
            CurrentKillCount,
            NextRequiredKillCount);
    }

    public void NotifyMonsterKilled()
    {
        if (!m_IsInitialized)
            return;

        CurrentKillCount++;
        var nextStage = NextStageData;
        if (nextStage != null
            && CurrentKillCount >= nextStage.RequiredKillCount)
        {
            m_CurrentStageListIndex++;
            CurrentKillCount = 0;
            OnStageChanged?.Invoke(CurrentStageData);
        }

        OnStageProgressChanged?.Invoke(
            CurrentKillCount,
            NextRequiredKillCount);
    }

    public bool TrySelectMonster(out MonsterDefinition definition)
    {
        definition = null;
        var stage = CurrentStageData;
        if (stage == null)
            return false;

        var totalWeight = 0f;
        MonsterDefinition lastValidDefinition = null;
        foreach (var entry in stage.MonsterSpawnEntries)
        {
            if (entry == null
                || entry.MonsterDefinition == null
                || entry.SpawnWeight <= 0f)
            {
                continue;
            }

            totalWeight += entry.SpawnWeight;
            lastValidDefinition = entry.MonsterDefinition;
        }

        if (totalWeight <= 0f || lastValidDefinition == null)
            return false;

        var randomValue = UnityEngine.Random.Range(0f, totalWeight);
        var accumulatedWeight = 0f;
        foreach (var entry in stage.MonsterSpawnEntries)
        {
            if (entry == null
                || entry.MonsterDefinition == null
                || entry.SpawnWeight <= 0f)
            {
                continue;
            }

            accumulatedWeight += entry.SpawnWeight;
            if (randomValue <= accumulatedWeight)
            {
                definition = entry.MonsterDefinition;
                return true;
            }
        }

        definition = lastValidDefinition;
        return true;
    }

    bool ValidateAndSortStages()
    {
        m_Stages.Sort((left, right) => left.StageIndex.CompareTo(right.StageIndex));
        if (m_Stages.Count == 0 || m_Stages[0].StageIndex != 0)
        {
            Debug.LogError("[StageManager] Stage 0 데이터가 없습니다.", this);
            return false;
        }

        for (var i = 0; i < m_Stages.Count; i++)
        {
            var stage = m_Stages[i];
            if (stage.StageIndex != i)
            {
                Debug.LogError(
                    $"[StageManager] StageIndex는 0부터 연속되어야 합니다. 누락 또는 중복: {stage.StageIndex}",
                    stage);
                return false;
            }

            var hasValidEntry = false;
            foreach (var entry in stage.MonsterSpawnEntries)
            {
                if (entry != null
                    && entry.MonsterDefinition != null
                    && entry.SpawnWeight > 0f)
                {
                    hasValidEntry = true;
                    break;
                }
            }

            if (!hasValidEntry)
            {
                Debug.LogError(
                    $"[StageManager] Stage {stage.StageIndex}에 유효한 Spawn Entry가 없습니다.",
                    stage);
                return false;
            }
        }

        return true;
    }

    void AttachRuntimeSystems()
    {
        if (MonsterManager.HasInstance
            && m_MonsterManager != MonsterManager.Instance)
        {
            if (m_MonsterManager != null)
                m_MonsterManager.MonsterKilled -= HandleMonsterKilled;

            m_MonsterManager = MonsterManager.Instance;
            m_MonsterManager.MonsterKilled += HandleMonsterKilled;
        }

        if (GameFlowManager.HasInstance
            && m_GameFlowManager != GameFlowManager.Instance)
        {
            if (m_GameFlowManager != null)
                m_GameFlowManager.OnRunRestarted -= ResetRun;

            m_GameFlowManager = GameFlowManager.Instance;
            m_GameFlowManager.OnRunRestarted += ResetRun;
        }
    }

    void DetachRuntimeSystems()
    {
        if (m_MonsterManager != null)
            m_MonsterManager.MonsterKilled -= HandleMonsterKilled;
        if (m_GameFlowManager != null)
            m_GameFlowManager.OnRunRestarted -= ResetRun;

        m_MonsterManager = null;
        m_GameFlowManager = null;
    }

    void HandleMonsterKilled(MonsterEntity monster)
    {
        NotifyMonsterKilled();
    }
}
