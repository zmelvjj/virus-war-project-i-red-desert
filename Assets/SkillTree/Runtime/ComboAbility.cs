using UnityEngine;

public sealed class ComboAbility
{
    readonly ComboAbilityDefinition m_Definition;
    readonly AbilityModifierCollection m_Modifiers = new();

    bool m_IsUnlocked;
    bool m_HasPreviousMouthOpen;
    float m_PreviousMouthOpen;

    public bool IsUnlocked => m_IsUnlocked;
    public float BonusDamagePercent => Mathf.Max(
        0f,
        m_Modifiers.Calculate(
            AbilityProperty.BonusDamage,
            m_Definition.BaseBonusDamagePercent));
    public float RequiredAngle
    {
        get
        {
            var reduction = m_Modifiers.GetSetValue(
                    AbilityProperty.AngleReduction,
                    0f)
                + m_Modifiers.Sum(
                    AbilityProperty.AngleReduction,
                    SkillModifierOperation.Flat)
                + m_Definition.BaseAngle
                * m_Modifiers.Sum(
                    AbilityProperty.AngleReduction,
                    SkillModifierOperation.Percent) / 100f;
            return Mathf.Max(0f, m_Definition.BaseAngle - reduction);
        }
    }

    public ComboAbility(ComboAbilityDefinition definition)
    {
        m_Definition = definition;
    }

    public void Unlock()
    {
        m_IsUnlocked = true;
        m_HasPreviousMouthOpen = false;
    }

    public void AddModifier(AbilityModifier modifier)
    {
        m_Modifiers.Add(modifier);
    }

    public void RemoveModifiersFromSource(string sourceNodeGuid)
    {
        m_Modifiers.RemoveFromSource(sourceNodeGuid);
    }

    public float ResolveBasicAttackDamage(
        float baseDamage,
        out bool comboTriggered)
    {
        comboTriggered = false;
        baseDamage = Mathf.Max(0f, baseDamage);
        if (!m_IsUnlocked
            || !MouthOpenTracker.HasInstance
            || !MouthOpenTracker.Instance.HasFace)
        {
            m_HasPreviousMouthOpen = false;
            return baseDamage;
        }

        var currentMouthOpen = MouthOpenTracker.Instance.MouthOpen;
        if (!m_HasPreviousMouthOpen)
        {
            m_PreviousMouthOpen = currentMouthOpen;
            m_HasPreviousMouthOpen = true;
            return baseDamage;
        }

        var mouthOpenDelta = Mathf.Abs(currentMouthOpen - m_PreviousMouthOpen);
        m_PreviousMouthOpen = currentMouthOpen;
        if (mouthOpenDelta < RequiredAngle)
            return baseDamage;

        comboTriggered = true;
        return baseDamage * (1f + BonusDamagePercent / 100f);
    }

    public void Dispose()
    {
        m_IsUnlocked = false;
        m_HasPreviousMouthOpen = false;
    }
}
