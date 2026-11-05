using UnityEngine;

public sealed class ShockwaveAbility
{
    readonly ShockwaveAbilityDefinition m_Definition;
    readonly PlayerStats m_PlayerStats;
    readonly AbilityModifierCollection m_Modifiers = new();

    bool m_IsUnlocked;

    public bool IsUnlocked => m_IsUnlocked;
    public float ProcChance => Mathf.Clamp01(
        m_Modifiers.CalculateChance(
            AbilityProperty.Chance,
            m_Definition.BaseProcChance));
    public float Range => Mathf.Max(
        0f,
        m_Modifiers.Calculate(
            AbilityProperty.Range,
            m_Definition.BaseRange));
    public float DamageRatio => Mathf.Max(
        0f,
        m_Modifiers.GetSetValue(
            AbilityProperty.Damage,
            m_Definition.BaseDamageRatio)
            * (1f + m_Modifiers.Sum(
                AbilityProperty.Damage,
                SkillModifierOperation.Percent) / 100f));
    public float FlatDamage => m_Modifiers.Sum(
        AbilityProperty.Damage,
        SkillModifierOperation.Flat);

    public ShockwaveAbility(
        ShockwaveAbilityDefinition definition,
        PlayerStats playerStats)
    {
        m_Definition = definition;
        m_PlayerStats = playerStats;
    }

    public void Unlock()
    {
        if (m_IsUnlocked)
            return;

        m_IsUnlocked = true;
        BasicAttackEvents.Hit += HandleBasicAttackHit;
    }

    public void AddModifier(AbilityModifier modifier)
    {
        m_Modifiers.Add(modifier);
    }

    public void RemoveModifiersFromSource(string sourceNodeGuid)
    {
        m_Modifiers.RemoveFromSource(sourceNodeGuid);
    }

    public void Dispose()
    {
        if (m_IsUnlocked)
            BasicAttackEvents.Hit -= HandleBasicAttackHit;

        m_IsUnlocked = false;
    }

    void HandleBasicAttackHit(BasicAttackHit hit)
    {
        if (hit.Target == null || Random.value >= ProcChance)
            return;

        var range = Range;
        if (range <= 0f)
            return;

        var origin = Vector3.zero;
        ParticleVfxPoolManager.Instance.Play(
            m_Definition.VfxPrefab,
            origin,
            Quaternion.identity,
            range);

        if (!MonsterManager.HasInstance)
            return;

        var damage = Mathf.RoundToInt(Mathf.Max(
            0f,
            m_PlayerStats.Damage * DamageRatio + FlatDamage));
        var sqrRange = range * range;
        var monsters = MonsterManager.Instance.Monsters;

        for (var i = 0; i < monsters.Count; i++)
        {
            var monster = monsters[i];
            if (monster == null || monster.CurrentHp <= 0)
                continue;

            var offset = monster.transform.position - origin;
            offset.y = 0f;
            if (offset.sqrMagnitude <= sqrRange)
            {
                monster.TakeDamage(damage);
                DamagePopupManager.Instance.ShowDamage(new DamageResult(
                    monster,
                    damage,
                    false));
            }
        }
    }
}
