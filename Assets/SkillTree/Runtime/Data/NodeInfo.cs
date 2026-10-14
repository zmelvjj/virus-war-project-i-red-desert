using TMPro;
using UnityEngine;

public sealed class NodeInfo : ScriptableObject
{
    [SerializeField] string mainText;
    [SerializeField] TMP_FontAsset font;
    [SerializeField, TextArea] string description;
    [SerializeField] string statName;
    [SerializeField] string valueText;
    SkillNodeInfoState state;

    public string MainText => mainText;
    public TMP_FontAsset Font => font;
    public string Description => description;
    public string StatName => statName;
    public string ValueText => valueText;
    public SkillNodeInfoState State => state;

    internal void SetState(SkillNodeInfoState value)
    {
        state = value;
    }
}
