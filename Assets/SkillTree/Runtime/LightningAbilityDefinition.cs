using UnityEngine;

[CreateAssetMenu(
    fileName = "LightningAbilityDefinition",
    menuName = "Skill Tree/Abilities/Lightning Definition")]
public sealed class LightningAbilityDefinition : ScriptableObject
{
    [SerializeField, Range(0f, 1f)] float baseProcChance = 0.1f;
    [SerializeField, Min(0f)] float baseRange;
    [SerializeField, Min(0f)] float baseDamageRatio = 0.5f;
    [SerializeField] ParticleSystem vfxPrefab;
    [SerializeField] float fixedGroundY;
    [SerializeField] Vector3 vfxPositionOffset;

    public float BaseProcChance => baseProcChance;
    public float BaseRange => baseRange;
    public float BaseDamageRatio => baseDamageRatio;
    public ParticleSystem VfxPrefab => vfxPrefab;
    public float FixedGroundY => fixedGroundY;
    public Vector3 VfxPositionOffset => vfxPositionOffset;
}
