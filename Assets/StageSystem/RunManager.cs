using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunManager : MonoBehaviour
{
    [SerializeField] SpecialStageManager specialStageManager;

    readonly List<SpecialStageData> m_SpecialStages = new();
    readonly HashSet<string> m_ResolvedSpecialStageIds = new();

    SpecialStageAddressableLoader m_Loader;
    StageManager m_StageManager;
    SpecialStageData m_PreparedSpecialStage;
    string m_PendingSpecialStageId;

    public static RunManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;
    public static bool IsNormalRun => !HasInstance
        || Instance.CurrentRunType == RunType.NormalRun;

    public RunType CurrentRunType { get; private set; } = RunType.NormalRun;
    public int HighestReachedStage { get; private set; }
    public bool IsInitialized { get; private set; }
    public IReadOnlyCollection<string> ResolvedSpecialStageIds =>
        m_ResolvedSpecialStageIds;
    public string PendingSpecialStageId => m_PendingSpecialStageId;

    public event Action<RunType> OnRunTypeChanged;

    void Awake()
    {
        Instance = this;
    }

    public IEnumerator Initialize()
    {
        if (IsInitialized)
            yield break;

        m_Loader = new SpecialStageAddressableLoader();
        yield return m_Loader.LoadAll(m_SpecialStages);
        if (!m_Loader.Succeeded)
            Debug.LogError("[RunManager] Addressables SpecialStageData 로드에 실패했습니다.", this);

        ValidateSpecialStages();

        while (SaveManager.HasInstance && !SaveManager.Instance.IsLoaded)
            yield return null;

        if (SaveManager.HasInstance)
        {
            var progress = SaveManager.Instance.SpecialStageProgress;
            m_ResolvedSpecialStageIds.UnionWith(progress.resolvedSpecialStageIds);
            m_PendingSpecialStageId = progress.pendingSpecialStageId;
        }

        AttachStageManager();
        IsInitialized = true;
        PrepareNextRun();
    }

    public void PrepareNextRun()
    {
        m_PreparedSpecialStage = null;
        CurrentRunType = RunType.NormalRun;

        if (!string.IsNullOrEmpty(m_PendingSpecialStageId))
        {
            m_PreparedSpecialStage = m_SpecialStages.Find(
                stage => stage.SpecialStageId == m_PendingSpecialStageId);

            if (m_PreparedSpecialStage != null)
            {
                CurrentRunType = RunType.SpecialRun;
            }
            else
            {
                Debug.LogError(
                    $"[RunManager] Pending SpecialStageData를 찾을 수 없습니다: {m_PendingSpecialStageId}",
                    this);
            }

            m_PendingSpecialStageId = string.Empty;
            SaveProgress();
        }

        AttachStageManager();
        HighestReachedStage = CurrentRunType == RunType.NormalRun
            && m_StageManager != null
                ? Mathf.Max(0, m_StageManager.CurrentStageIndex)
                : 0;
        OnRunTypeChanged?.Invoke(CurrentRunType);
    }

    public void BeginPreparedRun()
    {
        if (CurrentRunType == RunType.SpecialRun)
            specialStageManager.Begin(m_PreparedSpecialStage);
    }

    public void NotifyRunEnded()
    {
        if (CurrentRunType == RunType.NormalRun)
            EvaluateSpecialStageReservation();
        else
            specialStageManager.EndRun();
    }

    void EvaluateSpecialStageReservation()
    {
        SpecialStageData selected = null;
        foreach (var stage in m_SpecialStages)
        {
            if (m_ResolvedSpecialStageIds.Contains(stage.SpecialStageId)
                || stage.RequiredReachedStage > HighestReachedStage)
            {
                continue;
            }

            if (selected == null
                || stage.RequiredReachedStage > selected.RequiredReachedStage
                || (stage.RequiredReachedStage == selected.RequiredReachedStage
                    && string.CompareOrdinal(
                        stage.SpecialStageId,
                        selected.SpecialStageId) < 0))
            {
                selected = stage;
            }
        }

        if (selected == null)
            return;

        foreach (var stage in m_SpecialStages)
        {
            if (!m_ResolvedSpecialStageIds.Contains(stage.SpecialStageId)
                && stage.RequiredReachedStage <= selected.RequiredReachedStage
                && stage.RequiredReachedStage <= HighestReachedStage)
            {
                m_ResolvedSpecialStageIds.Add(stage.SpecialStageId);
            }
        }

        m_PendingSpecialStageId = selected.SpecialStageId;
        SaveProgress();
    }

    void HandleStageChanged(StageData stage)
    {
        if (CurrentRunType == RunType.NormalRun && stage != null)
            HighestReachedStage = Mathf.Max(HighestReachedStage, stage.StageIndex);
    }

    void AttachStageManager()
    {
        if (!StageManager.HasInstance || m_StageManager == StageManager.Instance)
            return;

        if (m_StageManager != null)
            m_StageManager.OnStageChanged -= HandleStageChanged;

        m_StageManager = StageManager.Instance;
        m_StageManager.OnStageChanged += HandleStageChanged;
    }

    void SaveProgress()
    {
        if (SaveManager.HasInstance)
            SaveManager.Instance.Save();
    }

    void ValidateSpecialStages()
    {
        var ids = new HashSet<string>();
        for (var i = m_SpecialStages.Count - 1; i >= 0; i--)
        {
            var stage = m_SpecialStages[i];
            if (stage == null
                || string.IsNullOrWhiteSpace(stage.SpecialStageId)
                || !ids.Add(stage.SpecialStageId))
            {
                Debug.LogError("[RunManager] SpecialStageId가 비어 있거나 중복되었습니다.", stage);
                m_SpecialStages.RemoveAt(i);
            }
        }
    }

    void OnDestroy()
    {
        if (m_StageManager != null)
            m_StageManager.OnStageChanged -= HandleStageChanged;

        m_Loader?.Release();
        if (Instance == this)
            Instance = null;
    }
}
