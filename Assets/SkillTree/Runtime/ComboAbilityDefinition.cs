using UnityEngine;

[CreateAssetMenu(
    fileName = "ComboAbilityDefinition",
    menuName = "Skill Tree/Abilities/Combo Definition")]
public sealed class ComboAbilityDefinition : ScriptableObject
{
    [SerializeField, Min(0f)] float baseBonusDamagePercent = 35f;
    [SerializeField, Range(0f, 1f)] float baseAngle = 0.2f;

    public float BaseBonusDamagePercent => baseBonusDamagePercent;
    public float BaseAngle => baseAngle;
}
