using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class DNADropSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] DNAPickup pickupPrefab;
    [SerializeField] Transform playerTarget;

    [Header("Pool")]
    [SerializeField, Min(0)] int initialPoolSize = 32;
    [SerializeField, Min(1)] int dnaValuePerPickup = 5;

    [Header("Pickup")]
    [SerializeField, Min(0.01f)] float collectRadius = 2f;

    [Header("Scatter")]
    [FormerlySerializedAs("spawnRadius")]
    [SerializeField, Min(0f)] float scatterRadius = 0.2f;
    [SerializeField, Min(0f)] float minimumHorizontalImpulse;
    [FormerlySerializedAs("horizontalImpulse")]
    [SerializeField, Min(0f)] float maximumHorizontalImpulse = 2.5f;
    [SerializeField, Min(0f)] float upwardImpulse = 2f;
    [SerializeField, Range(0f, 180f)] float angleJitter = 12f;
    [SerializeField, Range(0f, 1f)] float forceJitter = 0.2f;

    readonly Queue<DNAPickup> m_Pool = new();
    readonly List<DNAPickup> m_ActivePickups = new();

    GameFlowManager m_GameFlowManager;

    public static DNADropSystem Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    void Awake()
    {
        Instance = this;

        for (var i = 0; i < initialPoolSize; i++)
            m_Pool.Enqueue(CreateInstance());
    }

    void Start()
    {
        m_GameFlowManager = GameFlowManager.Instance;
        m_GameFlowManager.OnGameOver += CollectAllImmediately;
        m_GameFlowManager.OnRunRestarted += CollectAllImmediately;
    }

    void OnDestroy()
    {
        if (m_GameFlowManager != null)
        {
            m_GameFlowManager.OnGameOver -= CollectAllImmediately;
            m_GameFlowManager.OnRunRestarted -= CollectAllImmediately;
        }

        if (Instance == this)
            Instance = null;
    }

    public void Drop(int rewardDNA, Vector3 deathPosition)
    {
        if (SkillEffectSystem.HasInstance)
            rewardDNA = SkillEffectSystem.Instance.CalculateDNAReward(rewardDNA);

        var spawnCount = Mathf.CeilToInt((float)rewardDNA / dnaValuePerPickup);
        if (spawnCount <= 0)
            return;

        var angleStep = 360f / spawnCount;
        for (var i = 0; i < spawnCount; i++)
        {
            var angle = angleStep * i + Random.Range(-angleJitter, angleJitter);
            var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            var radialRatio = Mathf.Sqrt(Random.value);
            var impulseRatio = Mathf.Clamp01(
                radialRatio + Random.Range(-forceJitter, forceJitter));
            var horizontalImpulse = Mathf.Lerp(
                minimumHorizontalImpulse,
                maximumHorizontalImpulse,
                impulseRatio);
            var position = deathPosition + direction * (scatterRadius * radialRatio);
            var impulse = direction * horizontalImpulse
                + Vector3.up * upwardImpulse;
            var pickup = m_Pool.Count > 0 ? m_Pool.Dequeue() : CreateInstance();
            m_ActivePickups.Add(pickup);
            var pickupDNA = Mathf.Min(
                dnaValuePerPickup,
                rewardDNA - i * dnaValuePerPickup);
            pickup.Spawn(
                position,
                playerTarget,
                pickupDNA,
                collectRadius,
                impulse);
        }
    }

    internal void Release(DNAPickup pickup)
    {
        m_ActivePickups.Remove(pickup);
        pickup.PrepareForPool();
        m_Pool.Enqueue(pickup);
    }

    void CollectAllImmediately()
    {
        while (m_ActivePickups.Count > 0)
            m_ActivePickups[m_ActivePickups.Count - 1].CollectImmediately();
    }

    DNAPickup CreateInstance()
    {
        var pickup = Instantiate(pickupPrefab, transform);
        pickup.name = pickupPrefab.name;
        pickup.Initialize(this);
        pickup.PrepareForPool();
        return pickup;
    }

}
