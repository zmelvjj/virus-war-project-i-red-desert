using System;
using UnityEngine;

[Serializable]
public sealed class SkillEffectData
{
    [SerializeField] SkillEffectType effectType;
    [SerializeField] PlayerStatType stat;
    [SerializeField] AbilityType ability;
    [SerializeField] AbilityProperty abilityProperty;
    [SerializeField] OtherAbilityType otherAbility;
    [SerializeField] OtherUpgradeProperty otherUpgradeProperty;
    [SerializeField] SkillModifierOperation operation;
    [SerializeField] float value;
    [SerializeField] float secondaryValue;

    public SkillEffectType EffectType => effectType;
    public PlayerStatType Stat => stat;
    public AbilityType Ability => ability;
    public AbilityProperty AbilityProperty => abilityProperty;
    public OtherAbilityType OtherAbility => otherAbility;
    public OtherUpgradeProperty OtherUpgradeProperty => otherUpgradeProperty;
    public SkillModifierOperation Operation => operation;
    public float Value => value;
    public float SecondaryValue => secondaryValue;
}
