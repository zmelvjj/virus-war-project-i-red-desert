using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillEffectSystem : MonoBehaviour
{
    SkillTreeManager m_SkillTreeManager;

    void OnEnable()
    {
        AttachManager();
    }

    void Start()
    {
        AttachManager();
    }

    void OnDisable()
    {
        if (m_SkillTreeManager != null)
            m_SkillTreeManager.OnNodeUnlocked -= HandleNodeUnlocked;

        m_SkillTreeManager = null;
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
        }
    }
}
