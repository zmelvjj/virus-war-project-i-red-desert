using System.Collections.Generic;

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

public sealed class AbilityModifierCollection
{
    readonly List<AbilityModifier> m_Modifiers = new();

    public void Add(AbilityModifier modifier)
    {
        m_Modifiers.Add(modifier);
    }

    public void RemoveFromSource(string sourceNodeGuid)
    {
        m_Modifiers.RemoveAll(modifier => modifier.SourceNodeGuid == sourceNodeGuid);
    }

    public float Calculate(AbilityProperty property, float baseValue)
    {
        var value = GetSetValue(property, baseValue);
        return (value + Sum(property, SkillModifierOperation.Flat))
            * (1f + Sum(property, SkillModifierOperation.Percent) / 100f);
    }

    public float CalculateChance(AbilityProperty property, float baseValue)
    {
        return GetSetValue(property, baseValue)
            + Sum(property, SkillModifierOperation.Flat)
            + Sum(property, SkillModifierOperation.Percent) / 100f;
    }

    public float GetSetValue(AbilityProperty property, float defaultValue)
    {
        var value = defaultValue;
        foreach (var modifier in m_Modifiers)
        {
            if (modifier.Property == property
                && modifier.Operation == SkillModifierOperation.Set)
            {
                value = modifier.Value;
            }
        }

        return value;
    }

    public float Sum(
        AbilityProperty property,
        SkillModifierOperation operation)
    {
        var total = 0f;
        foreach (var modifier in m_Modifiers)
        {
            if (modifier.Property == property && modifier.Operation == operation)
                total += modifier.Value;
        }

        return total;
    }
}
