using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkillNodeInspectorView : VisualElement
{
    readonly Label m_EmptyLabel;
    readonly ScrollView m_Fields;
    readonly ScrollView m_ConnectionFields;
    readonly ObjectField m_NodeSpriteField;
    readonly ColorField m_NodeColorField;
    readonly ObjectField m_SkillSpriteField;
    readonly ColorField m_SkillColorField;
    readonly Vector2Field m_SkillSpriteRatioField;
    readonly EnumField m_ActionTypeField;
    readonly Vector2Field m_PositionField;
    readonly Vector2Field m_SizeField;
    readonly IntegerField m_BendPointCountField;

    SkillNodeView m_Node;
    SkillConnectionView m_Connection;

    public event Action DraftChanged;
    public event Action<SkillConnectionView, int> ConnectionBendPointCountChanged;

    public SkillNodeInspectorView()
    {
        AddToClassList("node-inspector");

        var title = new Label("Node Inspector");
        title.AddToClassList("inspector-title");
        Add(title);

        m_EmptyLabel = new Label("Select a node to edit it.");
        m_EmptyLabel.AddToClassList("inspector-empty");
        Add(m_EmptyLabel);

        m_Fields = new ScrollView();
        m_Fields.AddToClassList("inspector-fields");
        Add(m_Fields);

        m_ConnectionFields = new ScrollView();
        m_ConnectionFields.AddToClassList("inspector-fields");
        Add(m_ConnectionFields);

        m_NodeSpriteField = AddObjectField("Node Sprite", typeof(Sprite));
        m_NodeColorField = new ColorField("Node Color");
        AddField(m_NodeColorField);
        m_SkillSpriteField = AddObjectField("Skill Sprite", typeof(Sprite));
        m_SkillColorField = new ColorField("Skill Color");
        AddField(m_SkillColorField);
        m_SkillSpriteRatioField = new Vector2Field("Skill Sprite Ratio");
        AddField(m_SkillSpriteRatioField);
        m_ActionTypeField = new EnumField("Skill Action", SkillActionType.None);
        AddField(m_ActionTypeField);
        m_PositionField = new Vector2Field("Position");
        AddField(m_PositionField);
        m_SizeField = new Vector2Field("Size");
        AddField(m_SizeField);

        m_BendPointCountField = new IntegerField("Bend Points");
        m_BendPointCountField.AddToClassList("inspector-field");
        m_ConnectionFields.Add(m_BendPointCountField);

        BindCallbacks();
        Bind(null);
    }

    public void Bind(SkillNodeView node)
    {
        m_Node = node;
        m_Connection = null;
        var hasNode = node != null;
        m_EmptyLabel.EnableInClassList("hidden", hasNode);
        m_Fields.EnableInClassList("hidden", !hasNode);
        m_ConnectionFields.EnableInClassList("hidden", true);

        if (hasNode)
            RefreshAllFields();
    }

    public void BindConnection(SkillConnectionView connection)
    {
        m_Node = null;
        m_Connection = connection;
        var hasConnection = connection != null;
        m_EmptyLabel.EnableInClassList("hidden", hasConnection);
        m_Fields.EnableInClassList("hidden", true);
        m_ConnectionFields.EnableInClassList("hidden", !hasConnection);

        if (hasConnection)
        {
            m_BendPointCountField.SetValueWithoutNotify(
                Mathf.Max(0, connection.Draft.pathPoints.Count - 2));
        }
    }

    public void RefreshTransformFields(SkillNodeView node)
    {
        if (m_Node != node)
            return;

        m_PositionField.SetValueWithoutNotify(node.Draft.position);
        m_SizeField.SetValueWithoutNotify(node.Draft.size);
    }

    ObjectField AddObjectField(string label, Type type)
    {
        var field = new ObjectField(label)
        {
            objectType = type,
            allowSceneObjects = false
        };
        AddField(field);
        return field;
    }

    void AddField(VisualElement field)
    {
        field.AddToClassList("inspector-field");
        m_Fields.Add(field);
    }

    void BindCallbacks()
    {
        m_NodeSpriteField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null)
                return;

            SkillTreeAssetReferenceUtility.GetReference(
                evt.newValue,
                out m_Node.Draft.nodeSpriteGuid,
                out m_Node.Draft.nodeSpriteLocalId);
            m_Node.RefreshVisuals();
            DraftChanged?.Invoke();
        });
        m_NodeColorField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null)
                return;

            m_Node.Draft.nodeColor = evt.newValue;
            m_Node.RefreshVisuals();
            DraftChanged?.Invoke();
        });
        m_SkillSpriteField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null)
                return;

            SkillTreeAssetReferenceUtility.GetReference(
                evt.newValue,
                out m_Node.Draft.skillSpriteGuid,
                out m_Node.Draft.skillSpriteLocalId);
            m_Node.RefreshVisuals();
            DraftChanged?.Invoke();
        });
        m_SkillColorField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null)
                return;

            m_Node.Draft.skillColor = evt.newValue;
            m_Node.RefreshVisuals();
            DraftChanged?.Invoke();
        });
        m_SkillSpriteRatioField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null)
                return;

            var ratio = new Vector2(Mathf.Clamp01(evt.newValue.x), Mathf.Clamp01(evt.newValue.y));
            m_Node.Draft.skillSpriteSizeRatio = ratio;
            m_SkillSpriteRatioField.SetValueWithoutNotify(ratio);
            m_Node.RefreshVisuals();
            DraftChanged?.Invoke();
        });
        m_ActionTypeField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null)
                return;

            m_Node.Draft.actionType = (SkillActionType)evt.newValue;
            DraftChanged?.Invoke();
        });
        m_PositionField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null)
                return;

            var rect = m_Node.GetPosition();
            rect.position = evt.newValue - rect.size * 0.5f;
            m_Node.SetPosition(rect);
            DraftChanged?.Invoke();
        });
        m_SizeField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null)
                return;

            var rect = m_Node.GetPosition();
            var center = rect.center;
            rect.size = evt.newValue;
            rect.position = center - rect.size * 0.5f;
            m_Node.SetPosition(rect);
            DraftChanged?.Invoke();
        });
        m_BendPointCountField.RegisterValueChangedCallback(evt =>
        {
            if (m_Connection == null)
                return;

            var bendPointCount = Mathf.Max(0, evt.newValue);
            m_BendPointCountField.SetValueWithoutNotify(bendPointCount);
            ConnectionBendPointCountChanged?.Invoke(m_Connection, bendPointCount);
        });
    }

    void RefreshAllFields()
    {
        var draft = m_Node.Draft;
        m_NodeSpriteField.SetValueWithoutNotify(
            SkillTreeAssetReferenceUtility.Load<Sprite>(
                draft.nodeSpriteGuid,
                draft.nodeSpriteLocalId));
        m_NodeColorField.SetValueWithoutNotify(draft.nodeColor);
        m_SkillSpriteField.SetValueWithoutNotify(
            SkillTreeAssetReferenceUtility.Load<Sprite>(
                draft.skillSpriteGuid,
                draft.skillSpriteLocalId));
        m_SkillColorField.SetValueWithoutNotify(draft.skillColor);
        m_SkillSpriteRatioField.SetValueWithoutNotify(draft.skillSpriteSizeRatio);
        m_ActionTypeField.SetValueWithoutNotify(draft.actionType);
        RefreshTransformFields(m_Node);
    }

}
