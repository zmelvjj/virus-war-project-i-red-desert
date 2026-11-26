using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AugmentManager : MonoBehaviour
{
    [SerializeField] List<AugmentDefinition> definitions = new();

    readonly HashSet<AugmentType> m_AcquiredAugments = new();

    public static AugmentManager Instance { get; private set; }
    public IReadOnlyCollection<AugmentType> AcquiredAugments => m_AcquiredAugments;

    public event Action<AugmentType> OnAugmentAcquired;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool HasAcquired(AugmentType type)
    {
        return m_AcquiredAugments.Contains(type);
    }

    public void GetAvailableAugments(List<AugmentDefinition> output)
    {
        output.Clear();
        foreach (var definition in definitions)
        {
            if (definition != null && !HasAcquired(definition.AugmentType)
                && !output.Exists(candidate => candidate.AugmentType == definition.AugmentType))
            {
                output.Add(definition);
            }
        }
    }

    public bool TryAcquire(AugmentType type)
    {
        if (!SaveManager.HasInstance
            || !definitions.Exists(definition => definition != null && definition.AugmentType == type)
            || !m_AcquiredAugments.Add(type))
        {
            return false;
        }

        if (!SaveManager.Instance.TrySave())
        {
            m_AcquiredAugments.Remove(type);
            return false;
        }

        OnAugmentAcquired?.Invoke(type);
        return true;
    }

    public void RestoreAcquiredAugments(IEnumerable<AugmentType> acquiredAugments)
    {
        m_AcquiredAugments.Clear();
        m_AcquiredAugments.UnionWith(acquiredAugments);
    }
}
