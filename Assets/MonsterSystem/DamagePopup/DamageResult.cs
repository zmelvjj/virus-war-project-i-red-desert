public readonly struct DamageResult
{
    public readonly MonsterEntity Target;
    public readonly int FinalDamage;
    public readonly bool IsCritical;

    public DamageResult(
        MonsterEntity target,
        int finalDamage,
        bool isCritical)
    {
        Target = target;
        FinalDamage = finalDamage;
        IsCritical = isCritical;
    }
}
