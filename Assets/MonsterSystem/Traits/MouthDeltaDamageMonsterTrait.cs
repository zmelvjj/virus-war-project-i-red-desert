using UnityEngine;

public sealed class MouthDeltaDamageMonsterTrait : MonoBehaviour, IMonsterTrait, IMonsterBasicAttackDamageModifier
{
    const float k_DamagePerDifference = 75f;

    public void Initialize(MonsterEntity entity, MonsterTraitBuildContext context)
    {
    }

    public float GetBonusDamage(
        float baseDamage,
        float mouthOpenDecrease,
        float mouthOpenDifference)
    {
        return mouthOpenDifference * k_DamagePerDifference;
    }
}
