using System.Collections.Generic;
using UnityEngine;

public sealed class ShockwaveAbility
{
    readonly ShockwaveAbilityDefinition m_Definition;
    readonly PlayerStats m_PlayerStats;
    readonly AbilityModifierCollection m_Modifiers = new();
    readonly List<MonsterEntity> m_Targets = new();

    bool m_IsUnlocked;

    public bool IsUnlocked => m_IsUnlocked;
    public float ProcChance => Mathf.Clamp01(
        m_Modifiers.CalculateChance(
            AbilityProperty.Chance,
            m_Definition.BaseProcChance));
    public float DamageRatio => Mathf.Max(
        0f,
        m_Modifiers.GetSetValue(AbilityProperty.Damage, m_Definition.BaseDamageRatio)
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
        if (modifier.Property == AbilityProperty.Chance || modifier.Property == AbilityProperty.Damage)
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
        if (hit.Target == null)
            return;

        var chance = ProcChance;
        if (chance <= 0f || (chance < 1f && Random.value >= chance))
            return;

        GameSoundPlayer.Request(GameSound.Shockwave);
        if (Shake.Instance != null)
            Shake.Instance.ShakeCamera(5f, 2f);

        var prefab = m_Definition.VfxPrefab;
        if (prefab != null)
            Object.Instantiate(prefab, m_Definition.VfxPositionOffset, prefab.transform.rotation);

        if (!MonsterManager.HasInstance)
            return;

        var damage = Mathf.RoundToInt(Mathf.Max(0f, m_PlayerStats.Damage * DamageRatio + FlatDamage));
        // Death traits can spawn new monsters during the hit.
        m_Targets.AddRange(MonsterManager.Instance.Monsters);

        foreach (var monster in m_Targets)
        {
            if (monster == null || monster.CurrentHp <= 0)
                continue;

            monster.TakeDamage(damage);
            DamagePopupManager.Instance.ShowDamage(new DamageResult(
                monster,
                damage,
                false));
        }

        m_Targets.Clear();
    }
}
