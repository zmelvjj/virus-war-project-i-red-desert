using UnityEngine;

[CreateAssetMenu(
    fileName = "ShockwaveAbilityDefinition",
    menuName = "Skill Tree/Abilities/Shockwave Definition")]
public sealed class ShockwaveAbilityDefinition : ScriptableObject
{
    [SerializeField, Range(0f, 1f)] float baseProcChance = 0.1f;
    [SerializeField, Min(0f)] float baseDamageRatio = 0.5f;
    [SerializeField] GameObject vfxPrefab;
    [SerializeField] Vector3 vfxPositionOffset = new(0f, 0.08f, 0f);

    public float BaseProcChance => baseProcChance;
    public float BaseDamageRatio => baseDamageRatio;
    public GameObject VfxPrefab => vfxPrefab;
    public Vector3 VfxPositionOffset => vfxPositionOffset;
}
