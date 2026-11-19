using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterEntity : MonoBehaviour
{
    [SerializeField] Transform damagePopupPoint;
    [SerializeField, Min(0f)] float fallbackDamagePopupHeight = 2f;

    public MonsterDefinition Definition { get; private set; }
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public MonsterMovementType MovementType { get; private set; }
    public bool IsDead => m_IsDead;
    public Vector3 DamagePopupPosition => damagePopupPoint != null
        ? damagePopupPoint.position
        : transform.position + Vector3.up * fallbackDamagePopupHeight;

    Renderer[] m_Renderers;
    Material[][] m_OriginalMaterials;
    MaterialPropertyBlock m_HitPropertyBlock;
    Coroutine m_HitFlash;
    Animator m_Animator;
    MonsterNavMeshMovement m_NavMeshMovement;
    bool m_IsDead;
    bool m_SuppressDeathReward;
    float m_RewardMultiplier;
    readonly List<IMonsterDeathHandler> m_DeathHandlers = new();
    readonly List<IMonsterBasicAttackDamageModifier> m_DamageModifiers = new();
    readonly List<IMonsterDamageTakenHandler> m_DamageTakenHandlers = new();

    public void Initialize(
        MonsterDefinition definition,
        int hp,
        MonsterMovementType movementType,
        float rewardMultiplier)
    {
        Definition = definition;
        MaxHp = hp;
        CurrentHp = hp;
        m_IsDead = false;
        m_SuppressDeathReward = false;
        MovementType = movementType;
        m_RewardMultiplier = rewardMultiplier;
        CacheMaterials();
        m_Animator = GetComponent<Animator>();
        m_NavMeshMovement = GetComponent<MonsterNavMeshMovement>();
    }

    public void CacheTraits()
    {
        m_DeathHandlers.Clear();
        m_DamageModifiers.Clear();
        m_DamageTakenHandlers.Clear();

        var behaviours = GetComponents<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (behaviour is IMonsterDeathHandler deathHandler)
                m_DeathHandlers.Add(deathHandler);
            if (behaviour is IMonsterBasicAttackDamageModifier damageModifier)
                m_DamageModifiers.Add(damageModifier);
            if (behaviour is IMonsterDamageTakenHandler damageTakenHandler)
                m_DamageTakenHandlers.Add(damageTakenHandler);
        }
    }

    public int ApplyBasicAttackTraitDamage(
        float baseDamage,
        float mouthOpenDecrease,
        float mouthOpenDifference,
        int currentDamage)
    {
        var bonusDamage = 0f;
        foreach (var modifier in m_DamageModifiers)
        {
            bonusDamage += modifier.GetBonusDamage(
                baseDamage,
                mouthOpenDecrease,
                mouthOpenDifference);
        }

        return currentDamage + Mathf.RoundToInt(bonusDamage);
    }

    public void Kill(MonsterDeathCause cause)
    {
        if (m_IsDead)
            return;

        CurrentHp = 0;
        Die(cause);
    }

    public void SuppressDeathReward()
    {
        m_SuppressDeathReward = true;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, null);
        if (m_Animator != null)
            m_Animator.SetInteger("HP_Ratio", (int)((float)CurrentHp / MaxHp * 100));
    }

    public void TakeDamage(int damage, Material hitMaterial)
    {
        TakeDamage(damage, hitMaterial, null);
    }

    public void TakeDamage(
        int damage,
        Material hitMaterial,
        Color? hitColor)
    {
        if (m_IsDead)
            return;

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        if (m_Animator != null)
            m_Animator.SetInteger("HP_Ratio", (int)((float)CurrentHp / MaxHp * 100));
        if (CurrentHp <= 0)
        {
            Die(MonsterDeathCause.Damage);
            return;
        }

        if (damage > 0)
        {
            foreach (var handler in m_DamageTakenHandlers)
                handler.OnDamageTaken(damage);
        }

        if (hitMaterial != null)
        {
            if (m_HitFlash != null)
                StopCoroutine(m_HitFlash);

            m_HitFlash = StartCoroutine(FlashMaterial(hitMaterial, hitColor));
        }
    }

    void Die(MonsterDeathCause cause)
    {
        m_IsDead = true;
        if (m_Animator != null)
            m_Animator.SetInteger("HP_Ratio", 0);
        if (m_NavMeshMovement != null)
            m_NavMeshMovement.enabled = false;

        if (cause == MonsterDeathCause.Damage)
        {
            if (MonsterManager.HasInstance)
                MonsterManager.Instance.NotifyMonsterKilled(this);
        }

        foreach (var handler in m_DeathHandlers)
            handler.OnMonsterDied(cause);

        if (cause == MonsterDeathCause.Damage
            && !m_SuppressDeathReward
            && DNADropSystem.HasInstance)
        {
            var rewardDNA = Random.Range(
                Definition.MinimumRewardDNA,
                Definition.MaximumRewardDNA + 1);
            rewardDNA = Mathf.FloorToInt(rewardDNA * m_RewardMultiplier);
            DNADropSystem.Instance.Drop(rewardDNA, transform.position);
        }

        if (MonsterManager.HasInstance)
            MonsterManager.Instance.NotifyMonsterDied(this);

        // 일반 처치 오브젝트 삭제는 사망 애니메이션 처리 후 외부에서 수행한다.
    }

    void CacheMaterials()
    {
        m_Renderers = GetComponentsInChildren<Renderer>();
        m_OriginalMaterials = new Material[m_Renderers.Length][];

        for (var i = 0; i < m_Renderers.Length; i++)
            m_OriginalMaterials[i] = m_Renderers[i].sharedMaterials;
    }

    IEnumerator FlashMaterial(Material hitMaterial, Color? hitColor)
    {
        if (hitColor.HasValue)
        {
            m_HitPropertyBlock ??= new MaterialPropertyBlock();
            m_HitPropertyBlock.Clear();
            m_HitPropertyBlock.SetColor("_BaseColor", hitColor.Value);
            m_HitPropertyBlock.SetColor("_Color", hitColor.Value);
        }

        for (var i = 0; i < m_Renderers.Length; i++)
        {
            var materials = new Material[m_OriginalMaterials[i].Length];
            for (var j = 0; j < materials.Length; j++)
                materials[j] = hitMaterial;

            m_Renderers[i].sharedMaterials = materials;
            m_Renderers[i].SetPropertyBlock(null);

            if (hitColor.HasValue)
                m_Renderers[i].SetPropertyBlock(m_HitPropertyBlock);
        }

        yield return new WaitForSeconds(0.2f);

        for (var i = 0; i < m_Renderers.Length; i++)
        {
            m_Renderers[i].sharedMaterials = m_OriginalMaterials[i];
            m_Renderers[i].SetPropertyBlock(null);
        }

        m_HitFlash = null;
    }

    void OnDestroy()
    {
        if (MonsterManager.HasInstance)
            MonsterManager.Instance.Unregister(this);
    }
}
