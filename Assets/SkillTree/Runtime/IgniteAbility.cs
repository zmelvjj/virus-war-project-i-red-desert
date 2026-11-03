using System.Collections.Generic;
using UnityEngine;

public sealed class IgniteAbility
{
    sealed class ActiveIgnite
    {
        public readonly MonsterEntity Target;
        public float RemainingDuration;
        public float TimeUntilTick;

        public ActiveIgnite(
            MonsterEntity target,
            float duration,
            float tickInterval)
        {
            Target = target;
            RemainingDuration = duration;
            TimeUntilTick = tickInterval;
        }
    }

    readonly IgniteAbilityDefinition m_Definition;
    readonly PlayerStats m_PlayerStats;
    readonly AbilityModifierCollection m_Modifiers = new();
    readonly List<ActiveIgnite> m_ActiveIgnites = new();

    bool m_IsUnlocked;

    public bool IsUnlocked => m_IsUnlocked;
    public float ProcChance => Mathf.Clamp01(
        m_Modifiers.CalculateChance(
            AbilityProperty.Chance,
            m_Definition.BaseProcChance));
    public float Duration => Mathf.Max(
        0f,
        m_Modifiers.Calculate(AbilityProperty.Duration, m_Definition.BaseDuration));
    public float SpreadChance => Mathf.Clamp01(
        m_Modifiers.CalculateChance(
            AbilityProperty.SpreadChance,
            m_Definition.BaseSpreadChance));
    public float SpreadRange => Mathf.Max(
        0f,
        m_Modifiers.Calculate(
            AbilityProperty.SpreadRange,
            m_Definition.BaseSpreadRange));
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

    public IgniteAbility(
        IgniteAbilityDefinition definition,
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

    public void Tick(float deltaTime)
    {
        for (var i = m_ActiveIgnites.Count - 1; i >= 0; i--)
        {
            var ignite = m_ActiveIgnites[i];
            if (ignite.Target == null || ignite.Target.CurrentHp <= 0)
            {
                m_ActiveIgnites.RemoveAt(i);
                continue;
            }

            ignite.RemainingDuration -= deltaTime;
            ignite.TimeUntilTick -= deltaTime;
            if (ignite.TimeUntilTick <= 0f)
            {
                var damage = CalculateDamage();
                ignite.Target.TakeDamage(damage);
                DamagePopupManager.Instance.ShowDamage(new DamageResult(
                    ignite.Target,
                    damage,
                    false));
                ignite.TimeUntilTick += m_Definition.TickInterval;
            }

            if (ignite.RemainingDuration <= 0f)
                m_ActiveIgnites.RemoveAt(i);
        }
    }

    public void Dispose()
    {
        if (m_IsUnlocked)
            BasicAttackEvents.Hit -= HandleBasicAttackHit;

        m_IsUnlocked = false;
        m_ActiveIgnites.Clear();
    }

    void HandleBasicAttackHit(BasicAttackHit hit)
    {
        if (hit.Target == null
            || hit.Target.CurrentHp <= 0
            || Random.value >= ProcChance)
        {
            return;
        }

        ApplyIgnite(hit.Target, true);
    }

    void ApplyIgnite(MonsterEntity target, bool canSpread)
    {
        var duration = Duration;
        if (target == null || target.CurrentHp <= 0 || duration <= 0f)
            return;

        foreach (var activeIgnite in m_ActiveIgnites)
        {
            if (activeIgnite.Target == target)
                return;
        }

        m_ActiveIgnites.Add(new ActiveIgnite(
            target,
            duration,
            m_Definition.TickInterval));

        var effectPosition = new Vector3(
            target.transform.position.x,
            m_Definition.FixedGroundY,
            target.transform.position.z) + m_Definition.VfxPositionOffset;
        ParticleVfxPoolManager.Instance.Play(
            m_Definition.VfxPrefab,
            effectPosition,
            Quaternion.identity,
            1f,
            target.transform,
            duration);

        if (canSpread)
            TrySpread(target);
    }

    void TrySpread(MonsterEntity source)
    {
        var spreadChance = SpreadChance;
        if (spreadChance <= 0f || Random.value >= spreadChance)
            return;

        var spreadRange = SpreadRange;
        if (spreadRange <= 0f || !MonsterManager.HasInstance)
            return;

        var sqrRange = spreadRange * spreadRange;
        var monsters = MonsterManager.Instance.Monsters;
        for (var i = 0; i < monsters.Count; i++)
        {
            var target = monsters[i];
            if (target == null
                || target == source
                || target.CurrentHp <= 0
                || (target.transform.position - source.transform.position).sqrMagnitude > sqrRange)
            {
                continue;
            }

            ApplyIgnite(target, false);
        }
    }

    int CalculateDamage()
    {
        return Mathf.RoundToInt(Mathf.Max(
            0f,
            m_PlayerStats.Damage * DamageRatio + FlatDamage));
    }
}
