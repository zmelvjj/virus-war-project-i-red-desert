using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ParticleVfxPoolSettings
{
    public GameObject prefab;
    [Min(0)] public int initialSize;
}

[DisallowMultipleComponent]
public sealed class ParticleVfxPoolManager : MonoBehaviour
{
    [SerializeField] List<ParticleVfxPoolSettings> pools = new();

    readonly Dictionary<GameObject, Queue<PooledParticleVfx>> m_Pools = new();

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

    public void Play(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        float scaleMultiplier = 1f,
        Transform followTarget = null,
        float duration = 0f)
    {
        var pool = GetOrCreatePool(prefab);
        var instance = pool.Count > 0
            ? pool.Dequeue()
            : CreateInstance(prefab);
        instance.Play(
            position,
            rotation,
            scaleMultiplier,
            followTarget,
            duration);
    }

    internal void Return(PooledParticleVfx instance)
    {
        instance.PrepareForPool();
        GetOrCreatePool(instance.SourcePrefab).Enqueue(instance);
    }

    Queue<PooledParticleVfx> GetOrCreatePool(GameObject prefab)
    {
        if (!m_Pools.TryGetValue(prefab, out var pool))
        {
            pool = new Queue<PooledParticleVfx>();
            m_Pools.Add(prefab, pool);
        }

        return pool;
    }

    PooledParticleVfx CreateInstance(GameObject prefab)
    {
        var effect = Instantiate(prefab, transform);
        effect.name = prefab.name;
        var instance = effect.GetComponent<PooledParticleVfx>();
        if (instance == null)
            instance = effect.AddComponent<PooledParticleVfx>();
        instance.Initialize(this, prefab);
        instance.PrepareForPool();
        return instance;
    }
}
