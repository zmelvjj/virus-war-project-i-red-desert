using UnityEngine;

[CreateAssetMenu(
    fileName = "CriticalAbilityDefinition",
    menuName = "Skill Tree/Abilities/Critical Definition")]
public sealed class CriticalAbilityDefinition : ScriptableObject
{
    [SerializeField, Range(0f, 1f)] float baseChance = 0.05f;
    [SerializeField, Min(0f)] float baseDamageMultiplier = 2f;
    [SerializeField, Min(0f)] float baseBonusAttack;
    [SerializeField, Min(0f)] float baseBonusAttackDuration;
    [SerializeField, Range(0f, 1f)] float baseMark;

    public float BaseChance => baseChance;
    public float BaseDamageMultiplier => baseDamageMultiplier;
    public float BaseBonusAttack => baseBonusAttack;
    public float BaseBonusAttackDuration => baseBonusAttackDuration;
    public float BaseMark => baseMark;
}
