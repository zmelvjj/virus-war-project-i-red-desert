using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    readonly List<MonsterEntity> m_Monsters = new();
    readonly Queue<MonsterNavMeshMovement> m_NavigationQueue = new();

    public IReadOnlyList<MonsterEntity> Monsters => m_Monsters;
    public int ActiveCount => m_Monsters.Count;

    void Awake()
    {
        Instance = this;
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
        if (m_NavigationQueue.Count == 0)
            return;

        var movement = m_NavigationQueue.Dequeue();
        if (movement != null && movement.isActiveAndEnabled)
            movement.CalculateNextPath();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
