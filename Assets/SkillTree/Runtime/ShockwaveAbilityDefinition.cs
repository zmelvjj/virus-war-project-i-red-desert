using UnityEngine;

[CreateAssetMenu(
    fileName = "ShockwaveAbilityDefinition",
    menuName = "Skill Tree/Abilities/Shockwave Definition")]
public sealed class ShockwaveAbilityDefinition : ScriptableObject
{
    [SerializeField, Range(0f, 1f)] float baseProcChance = 0.1f;
    [SerializeField, Min(0f)] float baseRange = 3f;
    [SerializeField, Min(0f)] float baseDamageRatio = 0.5f;
    [SerializeField] GameObject vfxPrefab;
    [SerializeField] float fixedGroundY;
    [SerializeField] Vector3 vfxPositionOffset;

    public float BaseProcChance => baseProcChance;
    public float BaseRange => baseRange;
    public float BaseDamageRatio => baseDamageRatio;
    public GameObject VfxPrefab => vfxPrefab;
    public float FixedGroundY => fixedGroundY;
    public Vector3 VfxPositionOffset => vfxPositionOffset;
}
