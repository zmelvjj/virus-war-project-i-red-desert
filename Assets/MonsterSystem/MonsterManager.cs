using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    readonly List<MonsterEntity> m_Monsters = new();
    readonly Queue<MonsterNavMeshMovement> m_NavigationQueue = new();
    GameFlowManager m_GameFlowManager;

    public IReadOnlyList<MonsterEntity> Monsters => m_Monsters;
    public int ActiveCount => m_Monsters.Count;

    void Awake()
    {
        Instance = this;
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
    }
}
