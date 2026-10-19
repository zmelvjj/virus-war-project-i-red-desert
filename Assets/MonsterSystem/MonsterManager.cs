using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    [Header("Navigation Distribution")]
    [SerializeField, Min(1)] int navigationSlotCount = 32;
    [SerializeField, Min(0f)] float navigationSlotMinSpacing = 1.5f;
    [SerializeField, Min(0.01f)] float navigationDensityRadius = 3f;
    [SerializeField, Range(0f, 1f)] float navigationMinimumTravelDistanceRatio = 0.3f;
    [SerializeField, Range(0f, 1f)] float navigationPreferredTravelDistanceRatio = 0.55f;
    [SerializeField, Range(0f, 1f)] float navigationMaximumPreferredDistanceRatio = 0.8f;
    [SerializeField, Min(1)] int navigationTopCandidateCount = 5;

    public static MonsterManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    readonly List<MonsterEntity> m_Monsters = new();
    readonly Queue<MonsterNavMeshMovement> m_NavigationQueue = new();
    NavigationDistributionSystem m_NavigationDistribution;
    GameFlowManager m_GameFlowManager;

    public IReadOnlyList<MonsterEntity> Monsters => m_Monsters;
    public int ActiveCount => m_Monsters.Count;

    void Awake()
    {
        Instance = this;
        m_NavigationDistribution = new NavigationDistributionSystem(
            navigationSlotCount,
            navigationSlotMinSpacing,
            navigationDensityRadius,
            navigationMinimumTravelDistanceRatio,
            navigationPreferredTravelDistanceRatio,
            navigationMaximumPreferredDistanceRatio,
            navigationTopCandidateCount);
    }

    void OnEnable()
    {
        AttachGameFlowManager();
    }

    void Start()
    {
        AttachGameFlowManager();
    }

    public void Register(MonsterEntity monster)
    {
        m_Monsters.Add(monster);
    }

    public void Unregister(MonsterEntity monster)
    {
        m_Monsters.Remove(monster);
    }

    public void QueueNavigation(MonsterNavMeshMovement movement)
    {
        m_NavigationQueue.Enqueue(movement);
    }

    public void RegisterNavigation(MonsterNavMeshMovement movement)
    {
        m_NavigationDistribution.Register(movement);
    }

    public void UnregisterNavigation(MonsterNavMeshMovement movement)
    {
        m_NavigationDistribution.Unregister(movement);
    }

    public void ReleaseNavigationDestination(MonsterNavMeshMovement movement)
    {
        m_NavigationDistribution.Release(movement);
    }

    public bool TryAssignNavigationDestination(
        MonsterNavMeshMovement movement,
        out Vector3 destination,
        out UnityEngine.AI.NavMeshPath path)
    {
        return m_NavigationDistribution.TryRequestDestination(
            movement,
            out destination,
            out path);
    }

    void Update()
    {
        if (!GameFlowManager.IsMainGame || m_NavigationQueue.Count == 0)
            return;

        var movement = m_NavigationQueue.Dequeue();
        if (movement != null && movement.isActiveAndEnabled)
            movement.CalculateNextPath();
    }

    void OnDestroy()
    {
        if (m_GameFlowManager != null)
            m_GameFlowManager.OnRunRestarted -= ResetRun;

        if (Instance == this)
            Instance = null;
    }

    void AttachGameFlowManager()
    {
        if (!GameFlowManager.HasInstance
            || m_GameFlowManager == GameFlowManager.Instance)
        {
            return;
        }

        if (m_GameFlowManager != null)
            m_GameFlowManager.OnRunRestarted -= ResetRun;

        m_GameFlowManager = GameFlowManager.Instance;
        m_GameFlowManager.OnRunRestarted += ResetRun;
    }

    void ResetRun()
    {
        for (var i = m_Monsters.Count - 1; i >= 0; i--)
        {
            if (m_Monsters[i] != null)
                Destroy(m_Monsters[i].gameObject);
        }

        m_Monsters.Clear();
        m_NavigationQueue.Clear();
        m_NavigationDistribution.Reset();
    }
}
