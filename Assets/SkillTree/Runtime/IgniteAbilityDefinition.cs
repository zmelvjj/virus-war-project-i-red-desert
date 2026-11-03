using UnityEngine;

[CreateAssetMenu(
    fileName = "IgniteAbilityDefinition",
    menuName = "Skill Tree/Abilities/Ignite Definition")]
public sealed class IgniteAbilityDefinition : ScriptableObject
{
    [SerializeField, Range(0f, 1f)] float baseProcChance = 0.1f;
    [SerializeField, Min(0f)] float baseDamageRatio = 0.1f;
    [SerializeField, Min(0f)] float baseDuration = 3f;
    [SerializeField, Min(0.01f)] float tickInterval = 1f;
    [SerializeField, Range(0f, 1f)] float baseSpreadChance;
    [SerializeField, Min(0f)] float baseSpreadRange = 3f;
    [SerializeField] GameObject vfxPrefab;
    [SerializeField] float fixedGroundY;
    [SerializeField] Vector3 vfxPositionOffset;

    public float BaseProcChance => baseProcChance;
    public float BaseDamageRatio => baseDamageRatio;
    public float BaseDuration => baseDuration;
    public float TickInterval => tickInterval;
    public float BaseSpreadChance => baseSpreadChance;
    public float BaseSpreadRange => baseSpreadRange;
    public GameObject VfxPrefab => vfxPrefab;
    public float FixedGroundY => fixedGroundY;
    public Vector3 VfxPositionOffset => vfxPositionOffset;
}
