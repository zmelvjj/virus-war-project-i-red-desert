using UnityEngine;

public enum MonsterTraitType
{
    [InspectorName("스테미나 드롭")]
    StaminaDrop = 1,
    [InspectorName("입 오므림 추가 데미지")]
    PursedMouthDamage = 2,
    [InspectorName("분열")]
    Split = 3,
    [InspectorName("입 벌림 차이 추가 데미지")]
    MouthDeltaDamage = 4,
    [InspectorName("항생제")]
    Antibiotic = 5,
    [InspectorName("피격 시 속도 증가")]
    HitSpeedBoost = 6
}

public enum MonsterDeathCause
{
    Damage,
    Antibiotic
}

public readonly struct MonsterTraitBuildContext
{
    public readonly Vector3 MovementCenter;
    public readonly float MovementRange;

    public MonsterTraitBuildContext(Vector3 movementCenter, float movementRange)
    {
        MovementCenter = movementCenter;
        MovementRange = movementRange;
    }
}

public interface IMonsterTrait
{
    void Initialize(MonsterEntity entity, MonsterTraitBuildContext context);
}

public interface IMonsterDeathHandler
{
    void OnMonsterDied(MonsterDeathCause cause);
}

public interface IMonsterBasicAttackDamageModifier
{
    float GetBonusDamage(
        float baseDamage,
        float mouthOpenDecrease,
        float mouthOpenDifference);
}

public interface IMonsterDamageTakenHandler
{
    void OnDamageTaken(int damage);
}
