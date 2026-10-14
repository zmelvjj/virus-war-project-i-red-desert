using System.Collections.Generic;
using UnityEngine;

public sealed class SkillNodeData : ScriptableObject
{
    [SerializeField] string guid;
    [SerializeField] Sprite nodeSprite;
    [SerializeField] Color nodeColor = new(100f / 255f, 100f / 255f, 100f / 255f);
    [SerializeField] Sprite skillSprite;
    [SerializeField] Color skillColor = new(100f / 255f, 100f / 255f, 100f / 255f);
    [SerializeField] Vector2 skillSpriteSizeRatio = new(0.64f, 0.64f);
    [SerializeField] NodeInfo nodeInfo;
    [SerializeField] int price;
    [SerializeField] List<SkillEffectData> effects = new();
    [SerializeField] Vector2 position;
    [SerializeField] Vector2 size;

    public string Guid => guid;
    public Sprite NodeSprite => nodeSprite;
    public Color NodeColor => nodeColor;
    public Sprite SkillSprite => skillSprite;
    public Color SkillColor => skillColor;
    public Vector2 SkillSpriteSizeRatio => skillSpriteSizeRatio;
    public NodeInfo NodeInfo => nodeInfo;
    public int Price => price;
    public IReadOnlyList<SkillEffectData> Effects => effects;
    public Vector2 Position => position;
    public Vector2 Size => size;
}
