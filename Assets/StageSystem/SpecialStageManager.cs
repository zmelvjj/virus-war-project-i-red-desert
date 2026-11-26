using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class SpecialStageManager : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] Transform spawnCenter;
    [SerializeField] Transform monsterParent;
    [SerializeField, Min(0.1f)] float spawnRadius = 10f;
    [SerializeField, Min(0f)] float minimumSpawnSpacing = 1.5f;
    [SerializeField, Min(1)] int candidateAttemptsPerMonster = 20;
    [SerializeField, Min(0.1f)] float navMeshSampleDistance = 2f;

    [Header("Special Reward")]
    [SerializeField, Min(0f)] float rewardScatterRadius = 4f;
    [SerializeField, Min(0f)] float rewardMinimumImpulse = 4f;
    [SerializeField, Min(0f)] float rewardMaximumImpulse = 8f;
    [SerializeField, Min(0f)] float rewardUpwardImpulse = 3f;
    [SerializeField, Min(0f)] float rewardCollectLockDuration = 1f;
    [SerializeField, Min(0f)] float rewardCollectionDuration = 5f;

    readonly HashSet<MonsterEntity> m_RemainingMonsters = new();
    SpecialStageData m_CurrentStage;
    int m_RewardBatchId;
    bool m_IsClearing;

    public static SpecialStageManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;
    public int RemainingMonsterCount => m_RemainingMonsters.Count;
    public bool IsClearing => m_IsClearing;

    void Awake()
    {
        Instance = this;
    }

    public void Begin(SpecialStageData stage)
    {
        m_CurrentStage = stage;
        m_RemainingMonsters.Clear();
        m_RewardBatchId = 0;
        m_IsClearing = false;

        MonsterManager.Instance.MonsterDied += HandleMonsterDied;
        var positions = GenerateSpawnPositions(stage.SpawnCount);
        foreach (var position in positions)
        {
            var monster = MonsterFactory.Instance
                .Create(stage.MonsterDefinition)
                .At(position)
                .Within(spawnCenter.position, spawnRadius)
                .Build();
            monster.transform.parent = monsterParent;
            RegisterSpawnedMonster(monster, stage.SpecialStageId);
        }

        if (m_RemainingMonsters.Count == 0)
            StartCoroutine(ClearSpecialStage(false));
    }

    public void RegisterSpawnedMonster(
        MonsterEntity monster,
        string specialStageId)
    {
        if (m_CurrentStage == null
            || m_IsClearing
            || specialStageId != m_CurrentStage.SpecialStageId)
        {
            return;
        }

        var marker = monster.GetComponent<SpecialStageMonster>();
        if (marker == null)
            marker = monster.gameObject.AddComponent<SpecialStageMonster>();

        marker.Initialize(specialStageId);
        m_RemainingMonsters.Add(monster);
    }

    public void EndRun()
    {
        StopAllCoroutines();
        if (MonsterManager.HasInstance)
            MonsterManager.Instance.MonsterDied -= HandleMonsterDied;

        if (m_RewardBatchId != 0 && DNADropSystem.HasInstance)
            DNADropSystem.Instance.DiscardBatch(m_RewardBatchId);

        m_CurrentStage = null;
        m_RemainingMonsters.Clear();
        m_RewardBatchId = 0;
        m_IsClearing = false;
    }

    void HandleMonsterDied(MonsterEntity monster)
    {
        if (!m_RemainingMonsters.Remove(monster)
            || m_RemainingMonsters.Count > 0
            || m_IsClearing)
        {
            return;
        }

        StartCoroutine(ClearSpecialStage(true));
    }

    IEnumerator ClearSpecialStage(bool completedCombat)
    {
        m_IsClearing = true;
        MonsterManager.Instance.MonsterDied -= HandleMonsterDied;

        var rewardDNA = m_CurrentStage.RollRewardDNA();
        if (DNADropSystem.HasInstance && rewardDNA > 0)
        {
            m_RewardBatchId = DNADropSystem.Instance.DropSpecialReward(
                rewardDNA,
                spawnCenter.position,
                rewardScatterRadius,
                rewardMinimumImpulse,
                rewardMaximumImpulse,
                rewardUpwardImpulse,
                rewardCollectLockDuration);
        }

        yield return new WaitForSeconds(
            rewardCollectLockDuration + rewardCollectionDuration);

        if (m_RewardBatchId != 0 && DNADropSystem.HasInstance)
            DNADropSystem.Instance.DiscardBatch(m_RewardBatchId);

        m_RewardBatchId = 0;
        if (completedCombat)
            GameFlowManager.Instance.CompleteSpecialRun();
        else
            GameFlowManager.Instance.EndRun();
    }

    List<Vector3> GenerateSpawnPositions(int requestedCount)
    {
        var positions = new List<Vector3>(requestedCount);
        for (var spacingPass = 0; spacingPass < 4
            && positions.Count < requestedCount; spacingPass++)
        {
            var spacing = minimumSpawnSpacing * Mathf.Pow(0.75f, spacingPass);
            var attempts = (requestedCount - positions.Count)
                * candidateAttemptsPerMonster;
            for (var attempt = 0; attempt < attempts
                && positions.Count < requestedCount; attempt++)
            {
                var point = Random.insideUnitCircle * spawnRadius;
                var candidate = spawnCenter.position
                    + new Vector3(point.x, 0f, point.y);
                if (!NavMesh.SamplePosition(
                    candidate,
                    out var hit,
                    navMeshSampleDistance,
                    NavMesh.AllAreas))
                {
                    continue;
                }

                var isSpaced = true;
                foreach (var position in positions)
                {
                    if ((position - hit.position).sqrMagnitude < spacing * spacing)
                    {
                        isSpaced = false;
                        break;
                    }
                }

                if (isSpaced)
                    positions.Add(hit.position);
            }
        }

        return positions;
    }

    void OnDestroy()
    {
        if (MonsterManager.HasInstance)
            MonsterManager.Instance.MonsterDied -= HandleMonsterDied;

        if (Instance == this)
            Instance = null;
    }
}
