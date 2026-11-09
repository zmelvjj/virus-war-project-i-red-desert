using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillEffectSystem : MonoBehaviour
{
    const float k_StaminaPerDamageStack = 20f;
    const string k_ConsumedDamageSource = "OtherUpgrade.Consumed.";
    const string k_LowStaminaDamageSource = "OtherUpgrade.LowStamina.";
    const string k_LivingAgentDamageSource = "OtherUpgrade.LivingAgent.";
    const string k_KillDamageSource = "OtherAbility.KillDamage";

    readonly struct OtherUpgradeEffect
    {
        public readonly string Key;
        public readonly OtherUpgradeProperty Property;
        public readonly SkillModifierOperation Operation;
        public readonly float Value;
        public readonly float SecondaryValue;

        public OtherUpgradeEffect(
            string key,
            OtherUpgradeProperty property,
            SkillModifierOperation operation,
            float value,
            float secondaryValue)
        {
            Key = key;
            Property = property;
            Operation = operation;
            Value = value;
            SecondaryValue = secondaryValue;
        }
    }

    readonly List<OtherUpgradeEffect> m_OtherUpgradeEffects = new();
    readonly HashSet<string> m_ActiveLowStaminaEffects = new();
    readonly HashSet<OtherAbilityType> m_UnlockedOtherAbilities = new();

    SkillTreeManager m_SkillTreeManager;
    GameFlowManager m_GameFlowManager;
    GameStaminaController m_StaminaController;
    MonsterManager m_MonsterManager;
    float m_ConsumedStamina;
    int m_ConsumedDamageStacks;
    int m_NextOtherEffectId;
    int m_RunKillCount;
    float m_DNARewardRemainder;

    public static SkillEffectSystem Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        AttachManager();
        AttachRuntimeSystems();
    }

    void Start()
    {
        AttachManager();
        AttachRuntimeSystems();
    }

    void OnDisable()
    {
        if (m_SkillTreeManager != null)
            m_SkillTreeManager.OnNodeUnlocked -= HandleNodeUnlocked;

        m_SkillTreeManager = null;
        DetachRuntimeSystems();
        ResetRunEffects();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void AttachManager()
    {
        if (!SkillTreeManager.HasInstance
            || m_SkillTreeManager == SkillTreeManager.Instance)
        {
            return;
        }

        if (m_SkillTreeManager != null)
            m_SkillTreeManager.OnNodeUnlocked -= HandleNodeUnlocked;

        m_SkillTreeManager = SkillTreeManager.Instance;
        m_SkillTreeManager.OnNodeUnlocked += HandleNodeUnlocked;
    }

    void AttachRuntimeSystems()
    {
        if (GameFlowManager.HasInstance
            && m_GameFlowManager != GameFlowManager.Instance)
        {
            if (m_GameFlowManager != null)
            {
                m_GameFlowManager.OnGameOver -= ResetRunEffects;
                m_GameFlowManager.OnRunRestarted -= ResetRunEffects;
            }

            if (m_StaminaController != null)
            {
                m_StaminaController.StaminaConsumed -= HandleStaminaConsumed;
                m_StaminaController.StaminaChanged -= HandleStaminaChanged;
            }

            m_GameFlowManager = GameFlowManager.Instance;
            m_StaminaController = m_GameFlowManager.StaminaController;
            m_GameFlowManager.OnGameOver += ResetRunEffects;
            m_GameFlowManager.OnRunRestarted += ResetRunEffects;
            m_StaminaController.StaminaConsumed += HandleStaminaConsumed;
            m_StaminaController.StaminaChanged += HandleStaminaChanged;
        }

        if (MonsterManager.HasInstance
            && m_MonsterManager != MonsterManager.Instance)
        {
            if (m_MonsterManager != null)
            {
                m_MonsterManager.MonsterKilled -= HandleMonsterKilled;
                m_MonsterManager.LivingCountChanged -= HandleLivingCountChanged;
            }

            m_MonsterManager = MonsterManager.Instance;
            m_MonsterManager.MonsterKilled += HandleMonsterKilled;
            m_MonsterManager.LivingCountChanged += HandleLivingCountChanged;
        }
    }

    void DetachRuntimeSystems()
    {
        if (m_GameFlowManager != null)
        {
            m_GameFlowManager.OnGameOver -= ResetRunEffects;
            m_GameFlowManager.OnRunRestarted -= ResetRunEffects;
        }

        if (m_StaminaController != null)
        {
            m_StaminaController.StaminaConsumed -= HandleStaminaConsumed;
            m_StaminaController.StaminaChanged -= HandleStaminaChanged;
        }

        if (m_MonsterManager != null)
        {
            m_MonsterManager.MonsterKilled -= HandleMonsterKilled;
            m_MonsterManager.LivingCountChanged -= HandleLivingCountChanged;
        }

        m_GameFlowManager = null;
        m_StaminaController = null;
        m_MonsterManager = null;
    }

    void HandleNodeUnlocked(SkillNodeData nodeData)
    {
        foreach (var effect in nodeData.Effects)
            Apply(nodeData.Guid, effect);
    }

    void Apply(string sourceNodeGuid, SkillEffectData effect)
    {
        switch (effect.EffectType)
        {
            case SkillEffectType.StatModifier:
                PlayerStats.Instance.AddModifier(new StatModifier(
                    sourceNodeGuid,
                    effect.Stat,
                    effect.Operation,
                    effect.Value));
                break;

            case SkillEffectType.AbilityUnlock:
                AbilityManager.Instance.Unlock(effect.Ability);
                break;

            case SkillEffectType.AbilityModifier:
                AbilityManager.Instance.AddModifier(
                    effect.Ability,
                    new AbilityModifier(
                        sourceNodeGuid,
                        effect.AbilityProperty,
                        effect.Operation,
                        effect.Value));
                break;

            case SkillEffectType.OtherUpgrade:
                AddOtherUpgrade(sourceNodeGuid, effect);
                break;

            case SkillEffectType.OtherAbilityUnlock:
                UnlockOtherAbility(effect.OtherAbility);
                break;
        }
    }

    void UnlockOtherAbility(OtherAbilityType ability)
    {
        if (!m_UnlockedOtherAbilities.Add(ability))
            return;

        if (ability == OtherAbilityType.DamagePerKill)
            RefreshKillDamage();
    }

    void AddOtherUpgrade(string sourceNodeGuid, SkillEffectData effect)
    {
        var otherEffect = new OtherUpgradeEffect(
            $"{sourceNodeGuid}.{m_NextOtherEffectId++}",
            effect.OtherUpgradeProperty,
            effect.Operation,
            effect.Value,
            effect.SecondaryValue);
        m_OtherUpgradeEffects.Add(otherEffect);

        if (otherEffect.Property == OtherUpgradeProperty.DamagePer20StaminaConsumed)
            ApplyConsumedDamage(otherEffect);
        else if (otherEffect.Property == OtherUpgradeProperty.LowStaminaDamage
            && m_StaminaController != null)
        {
            RefreshLowStaminaEffect(
                otherEffect,
                m_StaminaController.CurrentStamina,
                m_StaminaController.CurrentMaxStamina);
        }
        else if (otherEffect.Property == OtherUpgradeProperty.DamagePerLivingAgent
            && m_MonsterManager != null)
        {
            ApplyLivingAgentDamage(otherEffect, m_MonsterManager.LivingCount);
        }
    }

    public int CalculateDNAReward(int baseReward)
    {
        var reward = (float)baseReward;
        foreach (var effect in m_OtherUpgradeEffects)
        {
            if (effect.Property != OtherUpgradeProperty.DNAGain)
                continue;

            if (effect.Operation == SkillModifierOperation.Flat)
                reward += effect.Value;
            else if (effect.Operation == SkillModifierOperation.Percent)
                reward *= 1f + effect.Value / 100f;
            else if (effect.Operation == SkillModifierOperation.Set)
                reward = baseReward * (1f + effect.Value / 100f);
        }

        if (m_UnlockedOtherAbilities.Contains(OtherAbilityType.DNAPer10Kills))
        {
            var bonusPercent = Mathf.FloorToInt(m_RunKillCount / 10f);
            reward *= 1f + bonusPercent / 100f;
        }

        reward += m_DNARewardRemainder;
        var wholeReward = Mathf.Max(0, Mathf.FloorToInt(reward));
        m_DNARewardRemainder = reward - wholeReward;
        return wholeReward;
    }

    public float ApplyMultiTargetDamage(float baseDamage, bool hitsMultipleTargets)
    {
        if (!hitsMultipleTargets
            || !m_UnlockedOtherAbilities.Contains(OtherAbilityType.MultiTargetDamage))
        {
            return baseDamage;
        }

        return baseDamage * 1.25f;
    }

    void HandleMonsterKilled(MonsterEntity monster)
    {
        if (!GameFlowManager.IsMainGame)
            return;

        m_RunKillCount++;
        RefreshKillDamage();

        var chance = Mathf.Clamp(
            CalculateOtherValue(OtherUpgradeProperty.KillStaminaRecoveryChance),
            0f,
            100f);
        if (Random.value * 100f >= chance)
            return;

        m_StaminaController.Restore(
            CalculateOtherValue(OtherUpgradeProperty.KillStaminaRecoveryAmount));
    }

    void RefreshKillDamage()
    {
        PlayerStats.Instance.RemoveModifiersFromSource(k_KillDamageSource);
        if (!m_UnlockedOtherAbilities.Contains(OtherAbilityType.DamagePerKill)
            || m_RunKillCount <= 0)
        {
            return;
        }

        PlayerStats.Instance.AddModifier(new StatModifier(
            k_KillDamageSource,
            PlayerStatType.Damage,
            SkillModifierOperation.Percent,
            Mathf.Min(m_RunKillCount, 150)));
    }

    void HandleLivingCountChanged(int livingCount)
    {
        foreach (var effect in m_OtherUpgradeEffects)
        {
            if (effect.Property == OtherUpgradeProperty.DamagePerLivingAgent)
                ApplyLivingAgentDamage(effect, livingCount);
        }
    }

    void ApplyLivingAgentDamage(OtherUpgradeEffect effect, int livingCount)
    {
        SetDamageModifier(
            k_LivingAgentDamageSource + effect.Key,
            effect,
            livingCount);
    }

    float CalculateOtherValue(OtherUpgradeProperty property)
    {
        var value = 0f;
        foreach (var effect in m_OtherUpgradeEffects)
        {
            if (effect.Property != property)
                continue;

            if (effect.Operation == SkillModifierOperation.Flat)
                value += effect.Value;
            else if (effect.Operation == SkillModifierOperation.Percent)
                value *= 1f + effect.Value / 100f;
            else if (effect.Operation == SkillModifierOperation.Set)
                value = effect.Value;
        }

        return Mathf.Max(0f, value);
    }

    void HandleStaminaConsumed(float amount)
    {
        m_ConsumedStamina += amount;
        var stacks = Mathf.FloorToInt(
            m_ConsumedStamina / k_StaminaPerDamageStack);
        if (stacks == m_ConsumedDamageStacks)
            return;

        m_ConsumedDamageStacks = stacks;
        foreach (var effect in m_OtherUpgradeEffects)
        {
            if (effect.Property == OtherUpgradeProperty.DamagePer20StaminaConsumed)
                ApplyConsumedDamage(effect);
        }
    }

    void ApplyConsumedDamage(OtherUpgradeEffect effect)
    {
        SetDamageModifier(
            k_ConsumedDamageSource + effect.Key,
            effect,
            m_ConsumedDamageStacks);
    }

    void HandleStaminaChanged(float current, float maximum)
    {
        foreach (var effect in m_OtherUpgradeEffects)
        {
            if (effect.Property == OtherUpgradeProperty.LowStaminaDamage)
                RefreshLowStaminaEffect(effect, current, maximum);
        }
    }

    void RefreshLowStaminaEffect(
        OtherUpgradeEffect effect,
        float current,
        float maximum)
    {
        var source = k_LowStaminaDamageSource + effect.Key;
        var threshold = Mathf.Clamp(effect.SecondaryValue, 0f, 100f);
        var currentPercent = maximum > 0f ? current / maximum * 100f : 0f;
        var shouldBeActive = GameFlowManager.IsMainGame
            && currentPercent <= threshold;
        var isActive = m_ActiveLowStaminaEffects.Contains(effect.Key);
        if (shouldBeActive == isActive)
            return;

        if (shouldBeActive)
        {
            m_ActiveLowStaminaEffects.Add(effect.Key);
            SetDamageModifier(source, effect, 1);
        }
        else
        {
            m_ActiveLowStaminaEffects.Remove(effect.Key);
            PlayerStats.Instance.RemoveModifiersFromSource(source);
        }
    }

    static void SetDamageModifier(
        string source,
        OtherUpgradeEffect effect,
        int multiplier)
    {
        PlayerStats.Instance.RemoveModifiersFromSource(source);
        if (multiplier <= 0 || Mathf.Approximately(effect.Value, 0f))
            return;

        var operation = effect.Operation == SkillModifierOperation.Set
            ? SkillModifierOperation.Percent
            : effect.Operation;
        PlayerStats.Instance.AddModifier(new StatModifier(
            source,
            PlayerStatType.Damage,
            operation,
            effect.Value * multiplier));
    }

    void ResetRunEffects()
    {
        m_ConsumedStamina = 0f;
        m_ConsumedDamageStacks = 0;
        m_RunKillCount = 0;
        m_DNARewardRemainder = 0f;

        if (PlayerStats.HasInstance)
        {
            PlayerStats.Instance.RemoveModifiersFromSource(k_KillDamageSource);
            foreach (var effect in m_OtherUpgradeEffects)
            {
                if (effect.Property == OtherUpgradeProperty.DamagePer20StaminaConsumed)
                {
                    PlayerStats.Instance.RemoveModifiersFromSource(
                        k_ConsumedDamageSource + effect.Key);
                }
                else if (effect.Property == OtherUpgradeProperty.LowStaminaDamage)
                {
                    PlayerStats.Instance.RemoveModifiersFromSource(
                        k_LowStaminaDamageSource + effect.Key);
                }
                else if (effect.Property == OtherUpgradeProperty.DamagePerLivingAgent)
                {
                    PlayerStats.Instance.RemoveModifiersFromSource(
                        k_LivingAgentDamageSource + effect.Key);
                }
            }
        }

        m_ActiveLowStaminaEffects.Clear();
    }
}
