using UnityEngine;

public sealed class PursedMouthDamageMonsterTrait : MonoBehaviour, IMonsterTrait, IMonsterBasicAttackDamageModifier
{
    const float k_RequiredDecrease = 0.4f;
    const float k_BonusDamageRatio = 1.35f;

    public void Initialize(MonsterEntity entity, MonsterTraitBuildContext context)
    {
    }

    public float GetBonusDamage(
        float baseDamage,
        float mouthOpenDecrease,
        float mouthOpenDifference)
    {
        return mouthOpenDecrease >= k_RequiredDecrease
            ? baseDamage * k_BonusDamageRatio
            : 0f;
    }
}
