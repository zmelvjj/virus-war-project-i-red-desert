using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkillTreeSetupWindow : EditorWindow
{
    const string k_UxmlPath = "Assets/SkillTree/Editor/SkillTreeSetupWindow.uxml";
    const string k_UssPath = "Assets/SkillTree/Editor/SkillTreeSetupWindow.uss";

    SkillTreeDraftData m_Draft;
    ObjectField m_ParentField;
    TextField m_OutputPathField;
    Vector2Field m_BaseSizeField;
    Label m_StatusLabel;

    [MenuItem("Tools/Skill Tree/Setup")]
    public static void Open()
    {
        var window = GetWindow<SkillTreeSetupWindow>();
        window.titleContent = new GUIContent("Skill Tree Setup");
        window.minSize = new Vector2(420f, 330f);
        window.Show();
    }

    public void CreateGUI()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
        visualTree.CloneTree(rootVisualElement);
        rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_UssPath));

        m_Draft = SkillTreeDraftStore.Load();
        BuildFields();
        BindActions();
        LoadDraftValues();
    }

    void BuildFields()
    {
        m_ParentField = new ObjectField("UGUI Parent")
        {
            objectType = typeof(RectTransform),
            allowSceneObjects = true
        };
        m_ParentField.AddToClassList("settings-field");
        rootVisualElement.Q<VisualElement>("parentFieldHost").Add(m_ParentField);

        m_OutputPathField = new TextField("ScriptableObject Path")
        {
            isDelayed = true
        };
        m_OutputPathField.AddToClassList("settings-field");
        rootVisualElement.Q<VisualElement>("outputPathHost").Add(m_OutputPathField);

        m_BaseSizeField = new Vector2Field("Base Size");
        m_BaseSizeField.AddToClassList("settings-field");
        rootVisualElement.Q<VisualElement>("baseSizeHost").Add(m_BaseSizeField);

        m_StatusLabel = rootVisualElement.Q<Label>("statusLabel");
    }

    void BindActions()
    {
        rootVisualElement.Q<Button>("openEditorButton").clicked += OpenEditor;
        rootVisualElement.Q<Button>("browseButton").clicked += BrowseOutputPath;
        rootVisualElement.Q<Button>("generateButton").clicked += Generate;

        m_ParentField.RegisterValueChangedCallback(evt =>
        {
            m_Draft.uguiParentGlobalObjectId = GetGlobalObjectId(evt.newValue as RectTransform);
            SaveDraft();
        });
        m_OutputPathField.RegisterValueChangedCallback(evt =>
        {
            m_Draft.assetOutputPath = evt.newValue;
            SaveDraft();
        });
        m_BaseSizeField.RegisterValueChangedCallback(evt =>
        {
            m_Draft.baseSize = evt.newValue;
            SaveDraft();
        });
    }

    void LoadDraftValues()
    {
        m_ParentField.SetValueWithoutNotify(ResolveParent(m_Draft.uguiParentGlobalObjectId));
        m_OutputPathField.SetValueWithoutNotify(m_Draft.assetOutputPath);
        m_BaseSizeField.SetValueWithoutNotify(m_Draft.baseSize);
    }

    void OpenEditor()
    {
        SaveDraft();
        SkillTreeEditorWindow.Open(m_Draft.baseSize);
    }

    void BrowseOutputPath()
    {
        var selectedPath = EditorUtility.OpenFolderPanel(
            "Select ScriptableObject Folder",
            Application.dataPath,
            string.Empty);
        if (string.IsNullOrEmpty(selectedPath) || !selectedPath.StartsWith(Application.dataPath, StringComparison.Ordinal))
            return;

        m_OutputPathField.value = "Assets" + selectedPath.Substring(Application.dataPath.Length);
    }

    void Generate()
    {
        var parent = m_ParentField.value as RectTransform;
        if (parent == null)
        {
            const string message = "UGUI Parent is required before generation.";
            Debug.LogWarning($"[SkillTree] {message}");
            m_StatusLabel.text = message;
            ShowNotification(new GUIContent(message));
            return;
        }

        SaveDraft();
        try
        {
            var result = SkillTreeGenerationProcessor.Generate(SkillTreeDraftStore.Load(), parent);
            m_StatusLabel.text = result.ReplacedExisting
                ? $"Replaced generation {result.GenerationId}."
                : $"Created generation {result.GenerationId}.";
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            m_StatusLabel.text = $"Generation failed: {exception.Message}";
        }
    }

    void SaveDraft()
    {
        if (m_Draft == null)
            return;

        var latestDraft = SkillTreeDraftStore.Load();
        latestDraft.baseSize = m_Draft.baseSize;
        latestDraft.assetOutputPath = m_Draft.assetOutputPath;
        latestDraft.uguiParentGlobalObjectId = m_Draft.uguiParentGlobalObjectId;
        SkillTreeDraftStore.Save(latestDraft);
    }

    void OnDisable()
    {
        SaveDraft();
    }

    static string GetGlobalObjectId(RectTransform parent)
    {
        return parent == null
            ? string.Empty
            : GlobalObjectId.GetGlobalObjectIdSlow(parent).ToString();
    }

    static RectTransform ResolveParent(string globalObjectId)
    {
        if (!GlobalObjectId.TryParse(globalObjectId, out var id))
            return null;

        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as RectTransform;
    }
}
