public enum DamagePopupStyle
{
    Normal,
    Ignite
}

public readonly struct DamageResult
{
    public readonly MonsterEntity Target;
    public readonly int FinalDamage;
    public readonly bool IsCritical;
    public readonly DamagePopupStyle PopupStyle;

    public DamageResult(
        MonsterEntity target,
        int finalDamage,
        bool isCritical,
        DamagePopupStyle popupStyle = DamagePopupStyle.Normal)
    {
        Target = target;
        FinalDamage = finalDamage;
        IsCritical = isCritical;
        PopupStyle = popupStyle;
    }
}
