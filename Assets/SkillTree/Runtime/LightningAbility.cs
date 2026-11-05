using UnityEngine;

public sealed class LightningAbility
{
    readonly LightningAbilityDefinition m_Definition;
    readonly PlayerStats m_PlayerStats;
    readonly AbilityModifierCollection m_Modifiers = new();

    bool m_IsUnlocked;

    public bool IsUnlocked => m_IsUnlocked;
    public float ProcChance => Mathf.Clamp01(
        m_Modifiers.CalculateChance(
            AbilityProperty.Chance,
            m_Definition.BaseProcChance));
    public float Range => Mathf.Max(
        0f,
        Calculate(AbilityProperty.Range, m_Definition.BaseRange));
    public float DamageRatio => Mathf.Max(
        0f,
        m_Modifiers.GetSetValue(
            AbilityProperty.Damage,
            m_Definition.BaseDamageRatio)
            * (1f + SumModifiers(
                AbilityProperty.Damage,
                SkillModifierOperation.Percent) / 100f));
    public float FlatDamage => SumModifiers(
        AbilityProperty.Damage,
        SkillModifierOperation.Flat);

    public LightningAbility(
        LightningAbilityDefinition definition,
        PlayerStats playerStats)
    {
        m_Definition = definition;
        m_PlayerStats = playerStats;
    }

    public void Unlock()
    {
        if (m_IsUnlocked)
            return;

        m_IsUnlocked = true;
        BasicAttackEvents.Hit += HandleBasicAttackHit;
    }

    public void AddModifier(AbilityModifier modifier)
    {
        m_Modifiers.Add(modifier);
    }

    public void RemoveModifiersFromSource(string sourceNodeGuid)
    {
        m_Modifiers.RemoveFromSource(sourceNodeGuid);
    }

    public void Dispose()
    {
        if (m_IsUnlocked)
            BasicAttackEvents.Hit -= HandleBasicAttackHit;

        m_IsUnlocked = false;
    }

    void HandleBasicAttackHit(BasicAttackHit hit)
    {
        if (hit.Target == null || Random.value > ProcChance)
            return;

        var damage = Mathf.RoundToInt(Mathf.Max(
            0f,
            m_PlayerStats.Damage * DamageRatio + FlatDamage));
        hit.Target.TakeDamage(damage);
        DamagePopupManager.Instance.ShowDamage(new DamageResult(
            hit.Target,
            damage,
            false));

        var effectPosition = new Vector3(
            hit.Position.x,
            m_Definition.FixedGroundY,
            hit.Position.z) + m_Definition.VfxPositionOffset;
        ParticleVfxPoolManager.Instance.Play(
            m_Definition.VfxPrefab,
            effectPosition,
            Quaternion.identity);
    }

    float Calculate(AbilityProperty property, float baseValue)
    {
        return m_Modifiers.Calculate(property, baseValue);
    }

    float SumModifiers(
        AbilityProperty property,
        SkillModifierOperation operation)
    {
        return m_Modifiers.Sum(property, operation);
    }
}
