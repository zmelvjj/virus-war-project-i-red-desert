using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class AugmentSelectionUI : MonoBehaviour
{
    readonly List<AugmentDefinition> m_Candidates = new();
    readonly List<AugmentCardUI> m_Cards = new();
    Action m_OnCompleted;
    bool m_IsSelecting;

    public bool TryShow(Action onCompleted)
    {
        if (AugmentManager.Instance == null)
            return false;

        AugmentManager.Instance.GetAvailableAugments(m_Candidates);
        m_Candidates.RemoveAll(HasInvalidCard);
        if (m_Candidates.Count == 0)
            return false;

        m_OnCompleted = onCompleted;
        transform.SetAsLastSibling();
        gameObject.SetActive(true);

        for (var i = 0; i < m_Candidates.Count; i++)
        {
            var definition = m_Candidates[i];
            var card = Instantiate(definition.CardUIPrefab, transform)
                .GetComponent<AugmentCardUI>();
            var rect = (RectTransform)card.transform;
            var size = rect.rect.size;
            rect.anchorMin = rect.anchorMax = new Vector2((i + 0.5f) / m_Candidates.Count, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition3D = Vector3.zero;
            card.Initialize(definition, Select);
            m_Cards.Add(card);
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(m_Cards[0].gameObject);

        return true;
    }

    static bool HasInvalidCard(AugmentDefinition definition)
    {
        if (definition.CardUIPrefab != null
            && definition.CardUIPrefab.activeSelf
            && definition.CardUIPrefab.TryGetComponent<AugmentCardUI>(out var card)
            && card.enabled
            && definition.CardUIPrefab.TryGetComponent<UnityEngine.UI.Button>(out var button)
            && button.enabled && button.interactable)
        {
            return false;
        }

        Debug.LogError("[AugmentSelectionUI] The card prefab needs an active AugmentCardUI and interactable Button.", definition);
        return true;
    }

    void Select(AugmentDefinition definition)
    {
        if (m_IsSelecting || m_OnCompleted == null)
            return;

        m_IsSelecting = true;
        foreach (var card in m_Cards)
            card.SetInteractable(false);

        if (!AugmentManager.Instance.TryAcquire(definition.AugmentType))
        {
            foreach (var card in m_Cards)
                card.SetInteractable(true);

            m_IsSelecting = false;
            return;
        }

        var onCompleted = m_OnCompleted;
        gameObject.SetActive(false);
        onCompleted();
    }

    void OnDisable()
    {
        foreach (var card in m_Cards)
            Destroy(card.gameObject);

        m_Cards.Clear();
        m_OnCompleted = null;
        m_IsSelecting = false;
    }
}
