using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ParticleVfxPoolSettings
{
    public ParticleSystem prefab;
    [Min(0)] public int initialSize;
}

[DisallowMultipleComponent]
public sealed class ParticleVfxPoolManager : MonoBehaviour
{
    [SerializeField] List<ParticleVfxPoolSettings> pools = new();

    readonly Dictionary<ParticleSystem, Queue<PooledParticleVfx>> m_Pools = new();

    public static ParticleVfxPoolManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    void Awake()
    {
        Instance = this;

        foreach (var settings in pools)
        {
            var pool = GetOrCreatePool(settings.prefab);
            for (var i = 0; i < settings.initialSize; i++)
                pool.Enqueue(CreateInstance(settings.prefab));
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Play(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        var pool = GetOrCreatePool(prefab);
        var instance = pool.Count > 0
            ? pool.Dequeue()
            : CreateInstance(prefab);
        instance.Play(position, rotation);
    }

    internal void Return(PooledParticleVfx instance)
    {
        instance.PrepareForPool();
        GetOrCreatePool(instance.SourcePrefab).Enqueue(instance);
    }

    Queue<PooledParticleVfx> GetOrCreatePool(ParticleSystem prefab)
    {
        if (!m_Pools.TryGetValue(prefab, out var pool))
        {
            pool = new Queue<PooledParticleVfx>();
            m_Pools.Add(prefab, pool);
        }

        return pool;
    }

    PooledParticleVfx CreateInstance(ParticleSystem prefab)
    {
        var particleSystem = Instantiate(prefab, transform);
        particleSystem.name = prefab.name;
        var instance = particleSystem.GetComponent<PooledParticleVfx>();
        if (instance == null)
            instance = particleSystem.gameObject.AddComponent<PooledParticleVfx>();
        instance.Initialize(this, prefab, particleSystem);
        instance.PrepareForPool();
        return instance;
    }
}
