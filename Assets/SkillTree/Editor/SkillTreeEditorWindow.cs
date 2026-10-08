using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkillTreeEditorWindow : EditorWindow
{
    const string k_UxmlPath = "Assets/SkillTree/Editor/SkillTreeEditorWindow.uxml";
    const string k_UssPath = "Assets/SkillTree/Editor/SkillTreeEditorWindow.uss";

    Vector2 m_RequestedBaseSize;
    SkillTreeDraftData m_Draft;
    SkillTreeGraphView m_GraphView;
    SkillNodeInspectorView m_Inspector;
    Label m_ModeLabel;

    [MenuItem("Tools/Skill Tree/Editor")]
    public static void OpenFromMenu()
    {
        Open(SkillTreeDraftStore.Load().baseSize);
    }

    public static void Open(Vector2 baseSize)
    {
        var window = GetWindow<SkillTreeEditorWindow>();
        window.m_RequestedBaseSize = baseSize;
        window.titleContent = new GUIContent("Skill Tree Editor");
        window.minSize = new Vector2(900f, 560f);
        window.Show();

        if (window.m_GraphView != null)
            window.BuildEditor();
    }

    public void CreateGUI()
    {
        BuildEditor();
    }

    void OnEnable()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= SaveDraft;
        AssemblyReloadEvents.beforeAssemblyReload += SaveDraft;
    }

    void OnDisable()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= SaveDraft;
        SaveDraft();
    }

    void BuildEditor()
    {
        rootVisualElement.Clear();

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
        visualTree.CloneTree(rootVisualElement);
        rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_UssPath));

        SaveDraft();
        m_Draft = SkillTreeDraftStore.Load();
        if (m_RequestedBaseSize.x > 0f && m_RequestedBaseSize.y > 0f)
            m_Draft.baseSize = m_RequestedBaseSize;

        m_GraphView = new SkillTreeGraphView(m_Draft);
        m_Inspector = new SkillNodeInspectorView();
        m_ModeLabel = rootVisualElement.Q<Label>("modeLabel");

        rootVisualElement.Q<VisualElement>("graphHost").Add(m_GraphView);
        rootVisualElement.Q<VisualElement>("inspectorHost").Add(m_Inspector);

        m_GraphView.NodeSelected += m_Inspector.Bind;
        m_GraphView.ConnectionSelected += m_Inspector.BindConnection;
        m_GraphView.NodeChanged += m_Inspector.RefreshTransformFields;
        m_GraphView.DraftChanged += SaveDraft;
        m_Inspector.DraftChanged += SaveDraft;
        m_Inspector.ConnectionBendPointCountChanged += m_GraphView.SetConnectionBendPointCount;
        m_GraphView.ModeChanged += UpdateModeLabel;
        UpdateModeLabel(m_GraphView.EditMode);
        SaveDraft();
    }

    void UpdateModeLabel(SkillNodeEditMode mode)
    {
        var isResize = mode == SkillNodeEditMode.Resize;
        m_ModeLabel.text = isResize ? "Resize Mode" : "Move Mode";
        m_ModeLabel.EnableInClassList("mode-resize", isResize);
        m_ModeLabel.EnableInClassList("mode-move", !isResize);
    }

    void SaveDraft()
    {
        if (m_Draft != null)
            SkillTreeDraftStore.Save(m_Draft);
    }
}
