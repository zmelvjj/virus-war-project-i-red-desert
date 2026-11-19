using System.Collections.Generic;
using System;
using UnityEngine;

public readonly struct StatModifier
{
    public readonly string SourceNodeGuid;
    public readonly PlayerStatType Stat;
    public readonly SkillModifierOperation Operation;
    public readonly float Value;

    public StatModifier(
        string sourceNodeGuid,
        PlayerStatType stat,
        SkillModifierOperation operation,
        float value)
    {
        SourceNodeGuid = sourceNodeGuid;
        Stat = stat;
        Operation = operation;
        Value = value;
    }
}

[DisallowMultipleComponent]
public sealed class PlayerStats : MonoBehaviour
{
    [SerializeField, Min(0f)] float baseDamage = 10f;
    [SerializeField, Min(0.01f)] float baseAttackSpeed = 1f;

    readonly List<StatModifier> m_Modifiers = new();

    public static PlayerStats Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    public float Damage => Calculate(PlayerStatType.Damage, baseDamage);
    public float AttackSpeed => Calculate(PlayerStatType.AttackSpeed, baseAttackSpeed);

    public event Action StatsChanged;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddModifier(StatModifier modifier)
    {
        m_Modifiers.Add(modifier);
        StatsChanged?.Invoke();
    }

    public void RemoveModifiersFromSource(string sourceNodeGuid)
    {
        if (m_Modifiers.RemoveAll(
            modifier => modifier.SourceNodeGuid == sourceNodeGuid) > 0)
        {
            StatsChanged?.Invoke();
        }
    }

    public float CalculateMaxStamina(float baseValue)
    {
        return CalculateSequential(PlayerStatType.MaxStamina, baseValue, false);
    }

    public float CalculateStaminaDrain(float baseValue)
    {
        return CalculateSequential(
            PlayerStatType.StaminaDrainReduction,
            baseValue,
            true);
    }

    float Calculate(PlayerStatType stat, float baseValue)
    {
        var flat = 0f;
        var percent = 0f;
        var value = baseValue;

        foreach (var modifier in m_Modifiers)
        {
            if (modifier.Stat != stat)
                continue;

            if (modifier.Operation == SkillModifierOperation.Flat)
                flat += modifier.Value;
            else if (modifier.Operation == SkillModifierOperation.Percent)
                percent += modifier.Value;
            else if (modifier.Operation == SkillModifierOperation.Set)
                value = modifier.Value;
        }

        return Mathf.Max(0f, (value * (1f + percent / 100f)) + flat);
    }

    float CalculateSequential(
        PlayerStatType stat,
        float baseValue,
        bool reduction)
    {
        var value = baseValue;
        foreach (var modifier in m_Modifiers)
        {
            if (modifier.Stat != stat)
                continue;

            if (modifier.Operation == SkillModifierOperation.Set)
                value = modifier.Value;
            else if (modifier.Operation == SkillModifierOperation.Flat)
                value += reduction ? -modifier.Value : modifier.Value;
            else if (modifier.Operation == SkillModifierOperation.Percent)
            {
                var ratio = modifier.Value / 100f;
                value *= reduction ? 1f - ratio : 1f + ratio;
            }
        }

        return Mathf.Max(0f, value);
    }
}
