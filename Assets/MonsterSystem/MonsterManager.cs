using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    readonly List<MonsterEntity> m_Monsters = new();

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

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
