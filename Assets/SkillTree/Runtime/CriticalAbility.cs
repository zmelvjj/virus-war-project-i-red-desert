using System;
using UnityEngine;

public sealed class CriticalAbility
{
    const string k_BonusAttackSource = "CriticalAbility.BonusAttack";

    readonly CriticalAbilityDefinition m_Definition;
    readonly PlayerStats m_PlayerStats;
    readonly AbilityModifierCollection m_Modifiers = new();

    bool m_IsUnlocked;
    float m_BonusAttackRemaining;

    public event Action<MonsterEntity> MarkTriggered;

    public bool IsUnlocked => m_IsUnlocked;
    public float Chance => Mathf.Clamp01(
        m_Modifiers.CalculateChance(
            AbilityProperty.Chance,
            m_Definition.BaseChance));
    public float DamageMultiplier => Mathf.Max(
        0f,
        m_Modifiers.Calculate(
            AbilityProperty.Damage,
            m_Definition.BaseDamageMultiplier));
    public float BonusAttack => Mathf.Max(
        0f,
        m_Modifiers.Calculate(
            AbilityProperty.BonusAttack,
            m_Definition.BaseBonusAttack));
    public float BonusAttackDuration => Mathf.Max(
        0f,
        m_Modifiers.Calculate(
            AbilityProperty.BonusAttackDuration,
            m_Definition.BaseBonusAttackDuration));
    public bool HasMark => m_Modifiers.Calculate(
        AbilityProperty.Mark,
        m_Definition.BaseMark) > 0f;

    public CriticalAbility(
        CriticalAbilityDefinition definition,
        PlayerStats playerStats)
    {
        m_Definition = definition;
        m_PlayerStats = playerStats;
    }

    public void Unlock()
    {
        m_IsUnlocked = true;
    }

    public void AddModifier(AbilityModifier modifier)
    {
        m_Modifiers.Add(modifier);
    }

    public void RemoveModifiersFromSource(string sourceNodeGuid)
    {
        m_Modifiers.RemoveFromSource(sourceNodeGuid);
    }

    public DamageResult ResolveBasicAttackDamage(float baseDamage, MonsterEntity target)
    {
        if (!m_IsUnlocked
            || target == null
            || target.CurrentHp <= 0
            || UnityEngine.Random.value >= Chance)
        {
            return new DamageResult(
                target,
                Mathf.RoundToInt(Mathf.Max(0f, baseDamage)),
                false);
        }

        var damage = Mathf.RoundToInt(Mathf.Max(0f, baseDamage * DamageMultiplier));
        RefreshBonusAttack();

        if (HasMark)
            MarkTriggered?.Invoke(target);

        return new DamageResult(target, damage, true);
    }

    public void Tick(float deltaTime)
    {
        if (m_BonusAttackRemaining <= 0f)
            return;

        m_BonusAttackRemaining -= deltaTime;
        if (m_BonusAttackRemaining > 0f)
            return;

        m_PlayerStats.RemoveModifiersFromSource(k_BonusAttackSource);
        m_BonusAttackRemaining = 0f;
    }

    public void Dispose()
    {
        m_PlayerStats.RemoveModifiersFromSource(k_BonusAttackSource);
        m_BonusAttackRemaining = 0f;
        m_IsUnlocked = false;
        MarkTriggered = null;
    }

    void RefreshBonusAttack()
    {
        var bonusAttack = BonusAttack;
        var duration = BonusAttackDuration;
        if (bonusAttack <= 0f || duration <= 0f)
            return;

        m_PlayerStats.RemoveModifiersFromSource(k_BonusAttackSource);
        m_PlayerStats.AddModifier(new StatModifier(
            k_BonusAttackSource,
            PlayerStatType.Damage,
            SkillModifierOperation.Percent,
            bonusAttack));
        m_BonusAttackRemaining = duration;
    }
}
