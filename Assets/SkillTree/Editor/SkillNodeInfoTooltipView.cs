using System;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkillNodeInfoTooltipView : VisualElement
{
    readonly TextField m_MainTextField;
    readonly TextField m_DescriptionField;
    readonly Label m_StateLabel;
    readonly Label m_PriceLabel;
    readonly TextField m_StatNameField;
    readonly TextField m_ValueTextField;
    SkillNodeView m_Node;

    public event Action<SkillNodeView> NodeInfoChanged;

    public SkillNodeInfoTooltipView()
    {
        pickingMode = PickingMode.Ignore;
        AddToClassList("node-info-tooltip");
        AddToClassList("hidden");

        var titleRow = new VisualElement();
        titleRow.AddToClassList("node-info-tooltip-title-row");
        Add(titleRow);
        m_MainTextField = AddTextField(titleRow, "node-info-tooltip-title");
        m_StateLabel = new Label("열림");
        m_StateLabel.AddToClassList("node-info-tooltip-state");
        titleRow.Add(m_StateLabel);

        m_DescriptionField = AddTextField("node-info-tooltip-description", true);
        m_PriceLabel = new Label();
        m_PriceLabel.AddToClassList("node-info-tooltip-price");
        Add(m_PriceLabel);

        var statRow = new VisualElement();
        statRow.AddToClassList("node-info-tooltip-stat-row");
        Add(statRow);
        m_StatNameField = AddTextField(statRow, "node-info-tooltip-stat-name");
        m_ValueTextField = AddTextField(statRow, "node-info-tooltip-stat-value");

        BindCallbacks();
        RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
    }

    public void Show(SkillNodeView node, Vector2 position, bool editable)
    {
        m_Node = node;
        transform.position = new Vector3(position.x, position.y, 0f);
        pickingMode = editable ? PickingMode.Position : PickingMode.Ignore;
        SetEditable(editable);
        Refresh();
        EnableInClassList("hidden", false);
        EnableInClassList("node-info-tooltip-editable", editable);
        BringToFront();
    }

    public void Hide()
    {
        m_Node = null;
        EnableInClassList("hidden", true);
    }

    public void Refresh(SkillNodeView node)
    {
        if (m_Node == node)
            Refresh();
    }

    TextField AddTextField(string className, bool multiline = false)
    {
        return AddTextField(this, className, multiline);
    }

    static TextField AddTextField(
        VisualElement parent,
        string className,
        bool multiline = false)
    {
        var field = new TextField
        {
            multiline = multiline
        };
        field.AddToClassList("node-info-tooltip-field");
        field.AddToClassList(className);
        parent.Add(field);
        return field;
    }

    void SetEditable(bool editable)
    {
        SetEditable(m_MainTextField, editable);
        SetEditable(m_DescriptionField, editable);
        SetEditable(m_StatNameField, editable);
        SetEditable(m_ValueTextField, editable);
    }

    static void SetEditable(TextField field, bool editable)
    {
        field.isReadOnly = !editable;
        field.focusable = editable;
        field.pickingMode = editable ? PickingMode.Position : PickingMode.Ignore;
    }

    void Refresh()
    {
        if (m_Node == null)
            return;

        var info = m_Node.Draft.nodeInfo ??= new SkillNodeInfoDraftData();
        m_MainTextField.SetValueWithoutNotify(info.mainText);
        m_DescriptionField.SetValueWithoutNotify(info.description);
        m_StateLabel.text = "열림";
        m_PriceLabel.text = $"가격  {m_Node.Draft.price}";
        m_StatNameField.SetValueWithoutNotify(info.statName);
        m_ValueTextField.SetValueWithoutNotify(info.valueText);

        var font = SkillTreeAssetReferenceUtility.Load<TMP_FontAsset>(info.fontGuid, 0);
        style.unityFont = font != null ? font.sourceFontFile : null;
    }

    void BindCallbacks()
    {
        m_MainTextField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null || m_MainTextField.isReadOnly)
                return;
            m_Node.Draft.nodeInfo.mainText = evt.newValue;
            NodeInfoChanged?.Invoke(m_Node);
        });
        m_DescriptionField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null || m_DescriptionField.isReadOnly)
                return;
            m_Node.Draft.nodeInfo.description = evt.newValue;
            NodeInfoChanged?.Invoke(m_Node);
        });
        m_StatNameField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null || m_StatNameField.isReadOnly)
                return;
            m_Node.Draft.nodeInfo.statName = evt.newValue;
            NodeInfoChanged?.Invoke(m_Node);
        });
        m_ValueTextField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null || m_ValueTextField.isReadOnly)
                return;
            m_Node.Draft.nodeInfo.valueText = evt.newValue;
            NodeInfoChanged?.Invoke(m_Node);
        });
    }
}
