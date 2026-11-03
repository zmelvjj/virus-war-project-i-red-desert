public enum SkillEffectType
{
    StatModifier,
    AbilityUnlock,
    AbilityModifier
}

public enum PlayerStatType
{
    Damage,
    AttackSpeed
}

public enum SkillModifierOperation
{
    Flat,
    Percent,
    Set
}

public enum AbilityType
{
    Lightning,
    Ignite,
    Critical,
    Shockwave,
    Combo
}

public enum AbilityProperty
{
    Chance,
    Range,
    Damage,
    Duration,
    SpreadChance,
    SpreadRange,
    BonusAttack,
    BonusAttackDuration,
    Mark,
    BonusDamage,
    AngleReduction
}

public enum SkillUnlockState
{
    Locked,
    Unlockable,
    Unlocked
}

public enum SkillNodeState
{
    Locked,
    Closed,
    Open,
    Unlocked
}

public enum SkillNodeInfoState
{
    Locked,
    Open
}

public enum SkillNodeAnchor
{
    Top,
    Right,
    Bottom,
    Left
}

public enum SkillConnectionRoutingMode
{
    Auto,
    Manual
}
