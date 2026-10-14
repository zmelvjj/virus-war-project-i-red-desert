using System.Collections.Generic;
using UnityEngine;

public readonly struct AbilityModifier
{
    public readonly string SourceNodeGuid;
    public readonly AbilityProperty Property;
    public readonly SkillModifierOperation Operation;
    public readonly float Value;

    public AbilityModifier(
        string sourceNodeGuid,
        AbilityProperty property,
        SkillModifierOperation operation,
        float value)
    {
        SourceNodeGuid = sourceNodeGuid;
        Property = property;
        Operation = operation;
        Value = value;
    }
}

public sealed class LightningAbility
{
    readonly LightningAbilityDefinition m_Definition;
    readonly PlayerStats m_PlayerStats;
    readonly List<AbilityModifier> m_Modifiers = new();

    bool m_IsUnlocked;

    public bool IsUnlocked => m_IsUnlocked;
    public float ProcChance => Mathf.Clamp01(
        Calculate(AbilityProperty.Chance, m_Definition.BaseProcChance));
    public float Range => Mathf.Max(
        0f,
        Calculate(AbilityProperty.Range, m_Definition.BaseRange));
    public float DamageRatio => Mathf.Max(
        0f,
        m_Definition.BaseDamageRatio
            * (1f + SumModifiers(
                AbilityProperty.Damage,
                SkillModifierOperation.Percent)));
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
        m_Modifiers.RemoveAll(modifier => modifier.SourceNodeGuid == sourceNodeGuid);
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
        var flat = SumModifiers(property, SkillModifierOperation.Flat);
        var percent = SumModifiers(property, SkillModifierOperation.Percent);

        return (baseValue + flat) * (1f + percent);
    }

    float SumModifiers(
        AbilityProperty property,
        SkillModifierOperation operation)
    {
        var total = 0f;

        foreach (var modifier in m_Modifiers)
        {
            if (modifier.Property != property || modifier.Operation != operation)
                continue;

            total += modifier.Value;
        }

        return total;
    }
}
