using UnityEngine;

[DisallowMultipleComponent]
public sealed class AbilityManager : MonoBehaviour
{
    [SerializeField] LightningAbilityDefinition lightningDefinition;
    [SerializeField] IgniteAbilityDefinition igniteDefinition;
    [SerializeField] CriticalAbilityDefinition criticalDefinition;
    [SerializeField] ShockwaveAbilityDefinition shockwaveDefinition;
    [SerializeField] ComboAbilityDefinition comboDefinition;

    LightningAbility m_Lightning;
    IgniteAbility m_Ignite;
    CriticalAbility m_Critical;
    ShockwaveAbility m_Shockwave;
    ComboAbility m_Combo;

    public static AbilityManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    public LightningAbility Lightning => m_Lightning ??= new LightningAbility(
        lightningDefinition,
        PlayerStats.Instance);
    public IgniteAbility Ignite => m_Ignite ??= new IgniteAbility(
        igniteDefinition,
        PlayerStats.Instance);
    public CriticalAbility Critical => m_Critical ??= new CriticalAbility(
        criticalDefinition,
        PlayerStats.Instance);
    public ShockwaveAbility Shockwave => m_Shockwave ??= new ShockwaveAbility(
        shockwaveDefinition,
        PlayerStats.Instance);
    public ComboAbility Combo => m_Combo ??= new ComboAbility(comboDefinition);

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        m_Ignite?.Tick(Time.deltaTime);
        m_Critical?.Tick(Time.deltaTime);
    }

    void OnDestroy()
    {
        m_Lightning?.Dispose();
        m_Ignite?.Dispose();
        m_Critical?.Dispose();
        m_Shockwave?.Dispose();
        m_Combo?.Dispose();

        if (Instance == this)
            Instance = null;
    }

    public void Unlock(AbilityType ability)
    {
        switch (ability)
        {
            case AbilityType.Lightning:
                Lightning.Unlock();
                break;
            case AbilityType.Ignite:
                Ignite.Unlock();
                break;
            case AbilityType.Critical:
                Critical.Unlock();
                break;
            case AbilityType.Shockwave:
                Shockwave.Unlock();
                break;
            case AbilityType.Combo:
                Combo.Unlock();
                break;
        }
    }

    public void AddModifier(
        AbilityType ability,
        AbilityModifier modifier)
    {
        switch (ability)
        {
            case AbilityType.Lightning:
                Lightning.AddModifier(modifier);
                break;
            case AbilityType.Ignite:
                Ignite.AddModifier(modifier);
                break;
            case AbilityType.Critical:
                Critical.AddModifier(modifier);
                break;
            case AbilityType.Shockwave:
                Shockwave.AddModifier(modifier);
                break;
            case AbilityType.Combo:
                Combo.AddModifier(modifier);
                break;
        }
    }

    public float ResolveBasicAttackBaseDamage(
        float baseDamage,
        out bool comboTriggered)
    {
        return Combo.ResolveBasicAttackDamage(baseDamage, out comboTriggered);
    }

    public DamageResult ResolveBasicAttackDamage(float baseDamage, MonsterEntity target)
    {
        return Critical.ResolveBasicAttackDamage(baseDamage, target);
    }
}
