using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkillNodeInspectorView : VisualElement
{
    static readonly List<AbilityProperty> s_LightningProperties = new()
    {
        AbilityProperty.Chance,
        AbilityProperty.Range,
        AbilityProperty.Damage
    };

    static readonly List<AbilityProperty> s_IgniteProperties = new()
    {
        AbilityProperty.Chance,
        AbilityProperty.Damage,
        AbilityProperty.Duration,
        AbilityProperty.SpreadChance,
        AbilityProperty.SpreadRange
    };

    static readonly List<AbilityProperty> s_CriticalProperties = new()
    {
        AbilityProperty.Chance,
        AbilityProperty.Damage,
        AbilityProperty.BonusAttack,
        AbilityProperty.BonusAttackDuration,
        AbilityProperty.Mark
    };

    static readonly List<AbilityProperty> s_ShockwaveProperties = new()
    {
        AbilityProperty.Chance,
        AbilityProperty.Range,
        AbilityProperty.Damage
    };

    static readonly List<AbilityProperty> s_ComboProperties = new()
    {
        AbilityProperty.BonusDamage,
        AbilityProperty.AngleReduction
    };

    static readonly List<SkillModifierOperation> s_StatOperations = new()
    {
        SkillModifierOperation.Flat,
        SkillModifierOperation.Percent,
        SkillModifierOperation.Set
    };

    static readonly List<SkillModifierOperation> s_AbilityOperations = new()
    {
        SkillModifierOperation.Flat,
        SkillModifierOperation.Percent,
        SkillModifierOperation.Set
    };

    readonly Label m_TitleLabel;
    readonly Label m_EmptyLabel;
    readonly ScrollView m_Fields;
    readonly ScrollView m_ConnectionFields;
    readonly ScrollView m_InfoFields;
    readonly ObjectField m_NodeSpriteField;
    readonly ColorField m_NodeColorField;
    readonly ObjectField m_SkillSpriteField;
    readonly ColorField m_SkillColorField;
    readonly Vector2Field m_SkillSpriteRatioField;
    readonly IntegerField m_DNACostField;
    readonly VisualElement m_EffectsContainer;
    readonly Vector2Field m_PositionField;
    readonly Vector2Field m_SizeField;
    readonly IntegerField m_BendPointCountField;
    readonly FloatField m_SegmentCoordinateField;
    readonly ColorField m_ConnectionColorField;
    readonly ObjectField m_ConnectionSpriteField;
    readonly Toggle m_RepeatConnectionSpriteField;
    readonly TextField m_InfoMainTextField;
    readonly ObjectField m_InfoFontField;
    readonly TextField m_InfoDescriptionField;
    readonly TextField m_InfoStatNameField;
    readonly TextField m_InfoValueTextField;

    SkillNodeView m_Node;
    SkillConnectionView m_Connection;
    SkillNodeEditMode m_Mode;

    public event Action DraftChanged;
    public event Action<SkillNodeView> NodeInfoChanged;
    public event Action<SkillConnectionView, int> ConnectionBendPointCountChanged;

    public SkillNodeInspectorView()
    {
        AddToClassList("node-inspector");

        m_TitleLabel = new Label("노드 인스펙터");
        m_TitleLabel.AddToClassList("inspector-title");
        Add(m_TitleLabel);

        m_EmptyLabel = new Label("편집할 노드나 연결선을 선택하세요.");
        m_EmptyLabel.AddToClassList("inspector-empty");
        Add(m_EmptyLabel);

        m_Fields = new ScrollView();
        m_Fields.AddToClassList("inspector-fields");
        Add(m_Fields);

        m_ConnectionFields = new ScrollView();
        m_ConnectionFields.AddToClassList("inspector-fields");
        Add(m_ConnectionFields);

        m_InfoFields = new ScrollView();
        m_InfoFields.AddToClassList("inspector-fields");
        Add(m_InfoFields);

        m_NodeSpriteField = AddObjectField("노드 스프라이트", typeof(Sprite));
        m_NodeColorField = new ColorField("노드 색상");
        AddField(m_NodeColorField);
        m_SkillSpriteField = AddObjectField("스킬 스프라이트", typeof(Sprite));
        m_SkillColorField = new ColorField("스킬 색상");
        AddField(m_SkillColorField);
        m_SkillSpriteRatioField = new Vector2Field("스킬 스프라이트 비율");
        AddField(m_SkillSpriteRatioField);
        m_DNACostField = new IntegerField("DNA 비용");
        AddField(m_DNACostField);

        var effectsHeader = new VisualElement();
        effectsHeader.AddToClassList("effect-list-header");
        effectsHeader.Add(new Label("효과"));
        var addEffectButton = new Button(AddEffect)
        {
            text = "+",
            tooltip = "효과 추가"
        };
        addEffectButton.AddToClassList("effect-add-button");
        effectsHeader.Add(addEffectButton);
        m_Fields.Add(effectsHeader);

        m_EffectsContainer = new VisualElement();
        m_EffectsContainer.AddToClassList("effect-list");
        m_Fields.Add(m_EffectsContainer);

        m_PositionField = new Vector2Field("위치");
        AddField(m_PositionField);
        m_SizeField = new Vector2Field("크기");
        AddField(m_SizeField);

        m_BendPointCountField = new IntegerField("꺾임점 개수");
        m_BendPointCountField.AddToClassList("inspector-field");
        m_ConnectionFields.Add(m_BendPointCountField);
        m_SegmentCoordinateField = new FloatField("선분 좌표");
        AddConnectionField(m_SegmentCoordinateField);
        m_ConnectionColorField = new ColorField("선 색상");
        AddConnectionField(m_ConnectionColorField);
        m_ConnectionSpriteField = new ObjectField("선 스프라이트")
        {
            objectType = typeof(Sprite),
            allowSceneObjects = false
        };
        AddConnectionField(m_ConnectionSpriteField);
        m_RepeatConnectionSpriteField = new Toggle("이미지 반복");
        AddConnectionField(m_RepeatConnectionSpriteField);

        m_InfoMainTextField = new TextField("제목");
        AddInfoField(m_InfoMainTextField);
        m_InfoFontField = new ObjectField("폰트")
        {
            objectType = typeof(TMP_FontAsset),
            allowSceneObjects = false
        };
        AddInfoField(m_InfoFontField);
        m_InfoDescriptionField = new TextField("설명")
        {
            multiline = true
        };
        m_InfoDescriptionField.AddToClassList("info-description-field");
        AddInfoField(m_InfoDescriptionField);
        m_InfoStatNameField = new TextField("스탯 이름");
        AddInfoField(m_InfoStatNameField);
        m_InfoValueTextField = new TextField("수치");
        AddInfoField(m_InfoValueTextField);

        BindCallbacks();
        Bind(null);
    }

    public void Bind(SkillNodeView node)
    {
        m_Node = node;
        m_Connection = null;
        var hasNode = node != null;
        m_EmptyLabel.EnableInClassList("hidden", hasNode);
        m_Fields.EnableInClassList(
            "hidden",
            !hasNode || m_Mode == SkillNodeEditMode.Info);
        m_ConnectionFields.EnableInClassList("hidden", true);
        m_InfoFields.EnableInClassList(
            "hidden",
            !hasNode || m_Mode != SkillNodeEditMode.Info);

        if (hasNode)
        {
            if (m_Mode == SkillNodeEditMode.Info)
                RefreshNodeInfoFields(node);
            else
                RefreshAllFields();
        }
    }

    public void BindConnection(SkillConnectionView connection)
    {
        m_Node = null;
        m_Connection = connection;
        var hasConnection = connection != null;
        m_EmptyLabel.EnableInClassList("hidden", hasConnection);
        m_Fields.EnableInClassList("hidden", true);
        m_ConnectionFields.EnableInClassList(
            "hidden",
            !hasConnection || m_Mode == SkillNodeEditMode.Info);
        m_InfoFields.EnableInClassList("hidden", true);

        if (hasConnection)
        {
            m_BendPointCountField.label = "꺾임점 개수";
            m_BendPointCountField.SetValueWithoutNotify(
                Mathf.Max(0, connection.Draft.pathPoints.Count - 2));
            var segmentIndex = connection.SelectedSegmentIndex;
            var hasSegment = segmentIndex >= 0
                && segmentIndex < connection.Draft.pathPoints.Count - 1;
            m_SegmentCoordinateField.EnableInClassList("hidden", !hasSegment);
            if (hasSegment)
            {
                m_SegmentCoordinateField.label = connection.IsSegmentHorizontal(segmentIndex)
                    ? "선분 Y 좌표"
                    : "선분 X 좌표";
                m_SegmentCoordinateField.SetValueWithoutNotify(
                    connection.GetSegmentCoordinate(segmentIndex));
                var canMove = connection.CanMoveSegment(segmentIndex);
                m_SegmentCoordinateField.SetEnabled(canMove);
                m_SegmentCoordinateField.tooltip = canMove
                    ? string.Empty
                    : "노드 연결점에 고정된 선분입니다.";
            }
            m_ConnectionColorField.SetValueWithoutNotify(connection.Draft.color);
            m_ConnectionSpriteField.SetValueWithoutNotify(
                SkillTreeAssetReferenceUtility.Load<Sprite>(
                    connection.Draft.spriteGuid,
                    connection.Draft.spriteLocalId));
            m_RepeatConnectionSpriteField.SetValueWithoutNotify(connection.Draft.repeatSprite);
        }
    }

    public void SetMode(SkillNodeEditMode mode)
    {
        m_Mode = mode;
        m_TitleLabel.text = mode == SkillNodeEditMode.Info
            ? "Tooltip 정보 편집"
            : "노드 인스펙터";
        Bind(m_Node);
    }

    public void RefreshNodeInfoFields(SkillNodeView node)
    {
        if (m_Mode != SkillNodeEditMode.Info || m_Node != node)
            return;

        var info = node.Draft.nodeInfo ??= new SkillNodeInfoDraftData();
        m_InfoMainTextField.SetValueWithoutNotify(info.mainText);
        m_InfoFontField.SetValueWithoutNotify(
            SkillTreeAssetReferenceUtility.Load<TMP_FontAsset>(info.fontGuid, 0));
        m_InfoDescriptionField.SetValueWithoutNotify(info.description);
        m_InfoStatNameField.SetValueWithoutNotify(info.statName);
        m_InfoValueTextField.SetValueWithoutNotify(info.valueText);
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

    void AddConnectionField(VisualElement field)
    {
        field.AddToClassList("inspector-field");
        m_ConnectionFields.Add(field);
    }

    void AddInfoField(VisualElement field)
    {
        field.AddToClassList("inspector-field");
        m_InfoFields.Add(field);
    }

    void BindCallbacks()
    {
        m_InfoMainTextField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null || m_Mode != SkillNodeEditMode.Info)
                return;
            m_Node.Draft.nodeInfo.mainText = evt.newValue;
            NodeInfoChanged?.Invoke(m_Node);
        });
        m_InfoFontField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null || m_Mode != SkillNodeEditMode.Info)
                return;
            SkillTreeAssetReferenceUtility.GetReference(
                evt.newValue,
                out m_Node.Draft.nodeInfo.fontGuid,
                out _);
            NodeInfoChanged?.Invoke(m_Node);
        });
        m_InfoDescriptionField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null || m_Mode != SkillNodeEditMode.Info)
                return;
            m_Node.Draft.nodeInfo.description = evt.newValue;
            NodeInfoChanged?.Invoke(m_Node);
        });
        m_InfoStatNameField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null || m_Mode != SkillNodeEditMode.Info)
                return;
            m_Node.Draft.nodeInfo.statName = evt.newValue;
            NodeInfoChanged?.Invoke(m_Node);
        });
        m_InfoValueTextField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null || m_Mode != SkillNodeEditMode.Info)
                return;
            m_Node.Draft.nodeInfo.valueText = evt.newValue;
            NodeInfoChanged?.Invoke(m_Node);
        });
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
        m_DNACostField.RegisterValueChangedCallback(evt =>
        {
            if (m_Node == null)
                return;

            m_Node.Draft.dnaCost = evt.newValue;
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
        m_SegmentCoordinateField.RegisterValueChangedCallback(evt =>
        {
            if (m_Connection == null)
                return;

            m_Connection.SetSelectedSegmentCoordinate(evt.newValue);
        });
        m_ConnectionColorField.RegisterValueChangedCallback(evt =>
        {
            if (m_Connection == null)
                return;

            m_Connection.Draft.color = evt.newValue;
            m_Connection.RefreshLayout();
            DraftChanged?.Invoke();
        });
        m_ConnectionSpriteField.RegisterValueChangedCallback(evt =>
        {
            if (m_Connection == null)
                return;

            SkillTreeAssetReferenceUtility.GetReference(
                evt.newValue,
                out m_Connection.Draft.spriteGuid,
                out m_Connection.Draft.spriteLocalId);
            m_Connection.RefreshLayout();
            DraftChanged?.Invoke();
        });
        m_RepeatConnectionSpriteField.RegisterValueChangedCallback(evt =>
        {
            if (m_Connection == null)
                return;

            m_Connection.Draft.repeatSprite = evt.newValue;
            m_Connection.RefreshLayout();
            DraftChanged?.Invoke();
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
        m_DNACostField.SetValueWithoutNotify(draft.dnaCost);
        RefreshEffectFields();
        RefreshTransformFields(m_Node);
    }

    void AddEffect()
    {
        if (m_Node == null)
            return;

        m_Node.Draft.effects.Add(new SkillEffectDraftData());
        RefreshEffectFields();
        DraftChanged?.Invoke();
    }

    void RefreshEffectFields()
    {
        m_EffectsContainer.Clear();
        if (m_Node == null)
            return;

        for (var i = 0; i < m_Node.Draft.effects.Count; i++)
            m_EffectsContainer.Add(CreateEffectEditor(m_Node.Draft.effects[i], i));
    }

    VisualElement CreateEffectEditor(SkillEffectDraftData effect, int index)
    {
        var container = new VisualElement();
        container.AddToClassList("effect-item");

        var header = new VisualElement();
        header.AddToClassList("effect-item-header");
        container.Add(header);

        var typeField = CreateLocalizedPopup(
            "효과 종류",
            new List<SkillEffectType>((SkillEffectType[])Enum.GetValues(
                typeof(SkillEffectType))),
            effect.effectType);
        typeField.AddToClassList("effect-type-field");
        typeField.RegisterValueChangedCallback(evt =>
        {
            effect.effectType = evt.newValue;
            RefreshEffectFields();
            DraftChanged?.Invoke();
        });
        header.Add(typeField);

        var removeButton = new Button(() =>
        {
            m_Node.Draft.effects.RemoveAt(index);
            RefreshEffectFields();
            DraftChanged?.Invoke();
        })
        {
            text = "삭제"
        };
        removeButton.AddToClassList("effect-remove-button");
        header.Add(removeButton);

        switch (effect.effectType)
        {
            case SkillEffectType.StatModifier:
                AddEnumEffectField(
                    container,
                    "스탯",
                    effect.stat,
                    value => effect.stat = (PlayerStatType)value);
                AddModifierFields(container, effect, false);
                break;

            case SkillEffectType.AbilityUnlock:
                AddEnumEffectField(
                    container,
                    "어빌리티",
                    effect.ability,
                    value => effect.ability = (AbilityType)value);
                break;

            case SkillEffectType.AbilityModifier:
                AddAbilityModifierFields(container, effect);
                break;

            case SkillEffectType.OtherAbilityUnlock:
                AddEnumEffectField(
                    container,
                    "기타 어빌리티",
                    effect.otherAbility,
                    value => effect.otherAbility = (OtherAbilityType)value);
                break;

            case SkillEffectType.OtherUpgrade:
                AddOtherUpgradeFields(container, effect);
                break;
        }

        if (effect.effectType != SkillEffectType.AbilityUnlock
            && effect.effectType != SkillEffectType.OtherAbilityUnlock
            && effect.operation != SkillModifierOperation.Set
            && Mathf.Approximately(effect.value, 0f))
        {
            var warning = new Label("값이 0이면 효과가 적용되지 않습니다.");
            warning.AddToClassList("effect-warning");
            container.Add(warning);
        }

        return container;
    }

    void AddModifierFields(
        VisualElement container,
        SkillEffectDraftData effect,
        bool allowSet,
        string valueLabel = "값")
    {
        var operations = allowSet ? s_AbilityOperations : s_StatOperations;
        if (!operations.Contains(effect.operation))
            effect.operation = SkillModifierOperation.Flat;

        var operationField = new PopupField<SkillModifierOperation>(
            "연산",
            operations,
            effect.operation);
        ApplyLocalizedFormat(operationField);
        operationField.RegisterValueChangedCallback(evt =>
        {
            effect.operation = evt.newValue;
            RefreshEffectFields();
            DraftChanged?.Invoke();
        });
        AddEffectField(container, operationField);

        var valueField = new FloatField(valueLabel);
        valueField.SetValueWithoutNotify(effect.value);
        valueField.RegisterValueChangedCallback(evt =>
        {
            effect.value = evt.newValue;
            DraftChanged?.Invoke();
        });
        AddEffectField(container, valueField);
    }

    void AddAbilityModifierFields(
        VisualElement container,
        SkillEffectDraftData effect)
    {
        var abilityField = CreateLocalizedPopup(
            "어빌리티",
            new List<AbilityType>((AbilityType[])Enum.GetValues(typeof(AbilityType))),
            effect.ability);
        abilityField.RegisterValueChangedCallback(evt =>
        {
            effect.ability = evt.newValue;
            var properties = GetAbilityProperties(effect.ability);
            if (!properties.Contains(effect.abilityProperty))
                effect.abilityProperty = properties[0];

            RefreshEffectFields();
            DraftChanged?.Invoke();
        });
        AddEffectField(container, abilityField);

        var supportedProperties = GetAbilityProperties(effect.ability);
        if (!supportedProperties.Contains(effect.abilityProperty))
            effect.abilityProperty = supportedProperties[0];

        var propertyField = new PopupField<AbilityProperty>(
            "속성",
            supportedProperties,
            effect.abilityProperty);
        ApplyLocalizedFormat(propertyField);
        propertyField.RegisterValueChangedCallback(evt =>
        {
            effect.abilityProperty = evt.newValue;
            DraftChanged?.Invoke();
        });
        AddEffectField(container, propertyField);

        AddModifierFields(container, effect, true);
    }

    void AddOtherUpgradeFields(
        VisualElement container,
        SkillEffectDraftData effect)
    {
        var propertyField = CreateLocalizedPopup(
            "기타 업그레이드",
            new List<OtherUpgradeProperty>(
                (OtherUpgradeProperty[])Enum.GetValues(
                    typeof(OtherUpgradeProperty))),
            effect.otherUpgradeProperty);
        propertyField.RegisterValueChangedCallback(evt =>
        {
            effect.otherUpgradeProperty = evt.newValue;
            RefreshEffectFields();
            DraftChanged?.Invoke();
        });
        AddEffectField(container, propertyField);

        if (effect.otherUpgradeProperty == OtherUpgradeProperty.LowStaminaDamage)
        {
            var thresholdField = new FloatField("발동 스태미나 (%)");
            thresholdField.SetValueWithoutNotify(effect.secondaryValue);
            thresholdField.RegisterValueChangedCallback(evt =>
            {
                effect.secondaryValue = Mathf.Clamp(evt.newValue, 0f, 100f);
                thresholdField.SetValueWithoutNotify(effect.secondaryValue);
                DraftChanged?.Invoke();
            });
            AddEffectField(container, thresholdField);
            AddModifierFields(container, effect, true, "추가 공격력");
            return;
        }

        AddModifierFields(
            container,
            effect,
            true,
            GetOtherUpgradeValueLabel(effect.otherUpgradeProperty));
    }

    static string GetOtherUpgradeValueLabel(OtherUpgradeProperty property)
    {
        return property switch
        {
            OtherUpgradeProperty.KillStaminaRecoveryChance => "스태미나 회복 확률",
            OtherUpgradeProperty.KillStaminaRecoveryAmount => "스태미나 회복 수치",
            OtherUpgradeProperty.DamagePer20StaminaConsumed => "20 소모당 추가 공격력",
            OtherUpgradeProperty.DNAGain => "DNA 획득 증가",
            OtherUpgradeProperty.DamagePerLivingAgent => "생존 Agent당 추가 공격력",
            _ => "값"
        };
    }

    static List<AbilityProperty> GetAbilityProperties(AbilityType ability)
    {
        return ability switch
        {
            AbilityType.Ignite => s_IgniteProperties,
            AbilityType.Critical => s_CriticalProperties,
            AbilityType.Shockwave => s_ShockwaveProperties,
            AbilityType.Combo => s_ComboProperties,
            _ => s_LightningProperties
        };
    }

    void AddEnumEffectField(
        VisualElement container,
        string label,
        Enum value,
        Action<Enum> changed)
    {
        var values = new List<Enum>();
        foreach (Enum enumValue in Enum.GetValues(value.GetType()))
            values.Add(enumValue);

        var field = new PopupField<Enum>(label, values, value);
        field.formatListItemCallback = GetLocalizedEnumName;
        field.formatSelectedValueCallback = GetLocalizedEnumName;
        field.RegisterValueChangedCallback(evt =>
        {
            changed(evt.newValue);
            DraftChanged?.Invoke();
        });
        AddEffectField(container, field);
    }

    static PopupField<T> CreateLocalizedPopup<T>(
        string label,
        List<T> choices,
        T value)
        where T : Enum
    {
        var field = new PopupField<T>(label, choices, value);
        ApplyLocalizedFormat(field);
        return field;
    }

    static void ApplyLocalizedFormat<T>(PopupField<T> field)
        where T : Enum
    {
        field.formatListItemCallback = value => GetLocalizedEnumName(value);
        field.formatSelectedValueCallback = value => GetLocalizedEnumName(value);
    }

    static string GetLocalizedEnumName(Enum value)
    {
        return value switch
        {
            SkillEffectType.StatModifier => "스탯 강화",
            SkillEffectType.AbilityUnlock => "어빌 해금",
            SkillEffectType.AbilityModifier => "어빌 강화",
            SkillEffectType.OtherUpgrade => "기타 강화",
            SkillEffectType.OtherAbilityUnlock => "기타 해금",

            PlayerStatType.Damage => "공격력",
            PlayerStatType.AttackSpeed => "공속",
            PlayerStatType.MaxStamina => "최대 스태미나",
            PlayerStatType.StaminaDrainReduction => "소모 감소",

            SkillModifierOperation.Flat => "수치",
            SkillModifierOperation.Percent => "비율",
            SkillModifierOperation.Set => "설정",

            AbilityType.Lightning => "번개",
            AbilityType.Ignite => "발화",
            AbilityType.Critical => "치명타",
            AbilityType.Shockwave => "충격파",
            AbilityType.Combo => "콤보",

            AbilityProperty.Chance => "확률",
            AbilityProperty.Range => "범위",
            AbilityProperty.Damage => "피해",
            AbilityProperty.Duration => "지속",
            AbilityProperty.SpreadChance => "확산 확률",
            AbilityProperty.SpreadRange => "확산 범위",
            AbilityProperty.BonusAttack => "추가 공격",
            AbilityProperty.BonusAttackDuration => "추가 지속",
            AbilityProperty.Mark => "표식",
            AbilityProperty.BonusDamage => "추가 피해",
            AbilityProperty.AngleReduction => "각도 감소",

            OtherAbilityType.DNAPer10Kills => "10킬 DNA",
            OtherAbilityType.DamagePerKill => "킬당 공격",
            OtherAbilityType.MultiTargetDamage => "다중 타격",

            OtherUpgradeProperty.KillStaminaRecoveryChance => "처치 회복 확률",
            OtherUpgradeProperty.KillStaminaRecoveryAmount => "처치 회복량",
            OtherUpgradeProperty.DamagePer20StaminaConsumed => "20 소모 공격",
            OtherUpgradeProperty.LowStaminaDamage => "저스태미나 공격",
            OtherUpgradeProperty.DNAGain => "DNA 증가",
            OtherUpgradeProperty.DamagePerLivingAgent => "생존당 공격",
            _ => value.ToString()
        };
    }

    static void AddEffectField(VisualElement container, VisualElement field)
    {
        field.AddToClassList("effect-field");
        container.Add(field);
    }

}
