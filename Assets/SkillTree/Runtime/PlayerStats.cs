using System.Collections.Generic;
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
    }

    public void RemoveModifiersFromSource(string sourceNodeGuid)
    {
        m_Modifiers.RemoveAll(modifier => modifier.SourceNodeGuid == sourceNodeGuid);
    }

    float Calculate(PlayerStatType stat, float baseValue)
    {
        var flat = 0f;
        var percent = 0f;

        foreach (var modifier in m_Modifiers)
        {
            if (modifier.Stat != stat)
                continue;

            if (modifier.Operation == SkillModifierOperation.Flat)
                flat += modifier.Value;
            else
                percent += modifier.Value;
        }

        return Mathf.Max(0f, (baseValue + flat) * (1f + percent));
    }
}
