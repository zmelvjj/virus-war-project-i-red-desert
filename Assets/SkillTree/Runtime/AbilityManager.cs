using UnityEngine;

[DisallowMultipleComponent]
public sealed class AbilityManager : MonoBehaviour
{
    [SerializeField] LightningAbilityDefinition lightningDefinition;

    LightningAbility m_Lightning;

    public static AbilityManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    public LightningAbility Lightning => m_Lightning ??= new LightningAbility(
        lightningDefinition,
        PlayerStats.Instance);

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        m_Lightning?.Dispose();

        if (Instance == this)
            Instance = null;
    }

    public void Unlock(AbilityType ability)
    {
        if (ability == AbilityType.Lightning)
            Lightning.Unlock();
    }

    public void AddModifier(
        AbilityType ability,
        AbilityModifier modifier)
    {
        if (ability == AbilityType.Lightning)
            Lightning.AddModifier(modifier);
    }
}
