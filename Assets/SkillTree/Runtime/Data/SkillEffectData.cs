using System;
using UnityEngine;

[Serializable]
public sealed class SkillEffectData
{
    [SerializeField] SkillEffectType effectType;
    [SerializeField] PlayerStatType stat;
    [SerializeField] AbilityType ability;
    [SerializeField] AbilityProperty abilityProperty;
    [SerializeField] SkillModifierOperation operation;
    [SerializeField] float value;

    public SkillEffectType EffectType => effectType;
    public PlayerStatType Stat => stat;
    public AbilityType Ability => ability;
    public AbilityProperty AbilityProperty => abilityProperty;
    public SkillModifierOperation Operation => operation;
    public float Value => value;
}
