using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(UnityEngine.UI.Button))]
public sealed class AugmentCardUI : MonoBehaviour
{
    UnityEngine.UI.Button m_Button;

    public void Initialize(AugmentDefinition definition, Action<AugmentDefinition> onSelected)
    {
        m_Button = GetComponent<UnityEngine.UI.Button>();
        m_Button.onClick.AddListener(() => onSelected(definition));
    }

    public void SetInteractable(bool interactable)
    {
        m_Button.interactable = interactable;
    }
}
