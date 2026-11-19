public enum SkillEffectType
{
    StatModifier,
    AbilityUnlock,
    AbilityModifier,
    OtherUpgrade,
    OtherAbilityUnlock
}

public enum PlayerStatType
{
    Damage,
    AttackSpeed,
    MaxStamina,
    StaminaDrainReduction
}

public enum OtherUpgradeProperty
{
    KillStaminaRecoveryChance,
    KillStaminaRecoveryAmount,
    DamagePer20StaminaConsumed,
    LowStaminaDamage,
    DNAGain,
    DamagePerLivingAgent
}

public enum OtherAbilityType
{
    DNAPer10Kills,
    DamagePerKill,
    MultiTargetDamage
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
