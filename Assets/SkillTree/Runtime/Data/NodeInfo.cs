using UnityEngine;

public sealed class NodeInfo : ScriptableObject
{
    [SerializeField] string mainText;
    [SerializeField] Font font;
    [SerializeField, TextArea] string description;
    [SerializeField] string statName;
    [SerializeField] string valueText;

    public string MainText => mainText;
    public Font Font => font;
    public string Description => description;
    public string StatName => statName;
    public string ValueText => valueText;
}
