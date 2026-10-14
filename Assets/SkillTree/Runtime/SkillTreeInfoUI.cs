using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class SkillTreeInfoUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] RectTransform panelRect;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI stateText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] TextMeshProUGUI statNameText;
    [SerializeField] TextMeshProUGUI valueText;
    [SerializeField] Vector2 pointerOffset = new(18f, -18f);

    RectTransform m_RectTransform;
    Canvas m_Canvas;
    SkillTreeManager m_Manager;
    string m_CurrentNodeGuid;

    public void Initialize(
        GameObject panelObject,
        RectTransform panelTransform,
        TextMeshProUGUI title,
        TextMeshProUGUI state,
        TextMeshProUGUI description,
        TextMeshProUGUI price,
        TextMeshProUGUI statName,
        TextMeshProUGUI value)
    {
        panel = panelObject;
        panelRect = panelTransform;
        titleText = title;
        stateText = state;
        descriptionText = description;
        priceText = price;
        statNameText = statName;
        valueText = value;
    }

    void Awake()
    {
        m_RectTransform = (RectTransform)transform;
        m_Canvas = GetComponentInParent<Canvas>();
        panel.SetActive(false);
    }

    void OnEnable()
    {
        SkillTreeNodeUI.NodePointerEntered += HandleNodePointerEntered;
        SkillTreeNodeUI.NodePointerExited += HandleNodePointerExited;
        AttachManager();
    }

    void Start()
    {
        AttachManager();
    }

    void OnDisable()
    {
        SkillTreeNodeUI.NodePointerEntered -= HandleNodePointerEntered;
        SkillTreeNodeUI.NodePointerExited -= HandleNodePointerExited;

        if (m_Manager != null)
            m_Manager.OnNodeUnlocked -= HandleNodeUnlocked;

        m_Manager = null;
        m_CurrentNodeGuid = null;
        panel.SetActive(false);
    }

    void Update()
    {
        if (panel.activeSelf && Mouse.current != null)
            PositionPanel(Mouse.current.position.ReadValue());
    }

    void HandleNodePointerEntered(string nodeGuid)
    {
        AttachManager();
        if (m_Manager == null || !m_Manager.TryGetNodeData(nodeGuid, out var nodeData))
            return;

        m_CurrentNodeGuid = nodeGuid;
        Refresh(nodeData);
        panel.SetActive(true);

        if (Mouse.current != null)
            PositionPanel(Mouse.current.position.ReadValue());
    }

    void HandleNodePointerExited(string nodeGuid)
    {
        if (nodeGuid != m_CurrentNodeGuid)
            return;

        m_CurrentNodeGuid = null;
        panel.SetActive(false);
    }

    void HandleNodeUnlocked(SkillNodeData nodeData)
    {
        if (nodeData.Guid == m_CurrentNodeGuid)
            Refresh(nodeData);
    }

    void AttachManager()
    {
        if (!SkillTreeManager.HasInstance || m_Manager == SkillTreeManager.Instance)
            return;

        if (m_Manager != null)
            m_Manager.OnNodeUnlocked -= HandleNodeUnlocked;

        m_Manager = SkillTreeManager.Instance;
        m_Manager.OnNodeUnlocked += HandleNodeUnlocked;
    }

    void Refresh(SkillNodeData nodeData)
    {
        var info = nodeData.NodeInfo;
        ApplyFont(info.Font);
        titleText.text = info.MainText;
        stateText.text = info.State == SkillNodeInfoState.Open ? "열림" : "잠김";
        descriptionText.text = info.Description;
        priceText.text = $"가격  {nodeData.Price}";
        statNameText.text = info.StatName;
        valueText.text = info.ValueText;
    }

    void ApplyFont(TMP_FontAsset font)
    {
        if (font == null)
            return;

        titleText.font = font;
        stateText.font = font;
        descriptionText.font = font;
        priceText.font = font;
        statNameText.font = font;
        valueText.font = font;
    }

    void PositionPanel(Vector2 screenPosition)
    {
        var camera = m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : m_Canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_RectTransform,
                screenPosition,
                camera,
                out var localPosition))
        {
            return;
        }

        var rootRect = m_RectTransform.rect;
        var panelSize = panelRect.rect.size;
        var targetPosition = localPosition + pointerOffset;
        targetPosition.x = Mathf.Clamp(
            targetPosition.x,
            rootRect.xMin,
            rootRect.xMax - panelSize.x);
        targetPosition.y = Mathf.Clamp(
            targetPosition.y,
            rootRect.yMin + panelSize.y,
            rootRect.yMax);
        panelRect.anchoredPosition = targetPosition;
    }
}
