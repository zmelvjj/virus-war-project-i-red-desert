using UnityEngine;
using UnityEngine.UI;

public static class SkillTreeUGuiGenerator
{
    const float k_ConnectionThickness = 3f;

    public static GameObject Generate(SkillTreeDraftData draft, RectTransform parent)
    {
        var root = CreateRectObject("GeneratedSkillTreeRoot", parent);
        Stretch(root.GetComponent<RectTransform>());

        var connectionLayer = CreateRectObject(
            "Connection Layer",
            root.GetComponent<RectTransform>());
        Stretch(connectionLayer.GetComponent<RectTransform>());

        var nodeLayer = CreateRectObject(
            "Node Layer",
            root.GetComponent<RectTransform>());
        Stretch(nodeLayer.GetComponent<RectTransform>());

        var parentSize = parent.rect.size;
        var positionScale = new Vector2(
            parentSize.x / draft.baseSize.x,
            parentSize.y / draft.baseSize.y);
        var uniformScale = Mathf.Min(positionScale.x, positionScale.y);

        for (var i = 0; i < draft.connections.Count; i++)
            CreateConnection(
                draft.connections[i],
                i,
                connectionLayer.GetComponent<RectTransform>(),
                positionScale,
                uniformScale);

        foreach (var node in draft.nodes)
            CreateNode(
                node,
                nodeLayer.GetComponent<RectTransform>(),
                positionScale,
                uniformScale);

        return root;
    }

    static void CreateNode(
        SkillNodeDraftData draft,
        RectTransform parent,
        Vector2 positionScale,
        float uniformScale)
    {
        var nodeObject = CreateImageObject($"Node_{draft.guid}", parent, out var nodeImage);
        var nodeRect = nodeObject.GetComponent<RectTransform>();
        Center(nodeRect);
        nodeRect.anchoredPosition = ScalePosition(draft.position, positionScale);
        nodeRect.sizeDelta = draft.size * uniformScale;
        nodeImage.sprite = SkillTreeAssetReferenceUtility.Load<Sprite>(
            draft.nodeSpriteGuid,
            draft.nodeSpriteLocalId);
        nodeImage.color = nodeImage.sprite != null ? draft.nodeColor : Color.clear;
        nodeImage.preserveAspect = false;
        nodeImage.raycastTarget = true;
        nodeImage.enabled = true;

        var skillObject = CreateImageObject("Skill Image", nodeRect, out var skillImage);
        var skillRect = skillObject.GetComponent<RectTransform>();
        var ratio = new Vector2(
            Mathf.Clamp01(draft.skillSpriteSizeRatio.x),
            Mathf.Clamp01(draft.skillSpriteSizeRatio.y));
        skillRect.anchorMin = Vector2.one * 0.5f - ratio * 0.5f;
        skillRect.anchorMax = Vector2.one * 0.5f + ratio * 0.5f;
        skillRect.offsetMin = Vector2.zero;
        skillRect.offsetMax = Vector2.zero;
        skillImage.sprite = SkillTreeAssetReferenceUtility.Load<Sprite>(
            draft.skillSpriteGuid,
            draft.skillSpriteLocalId);
        skillImage.color = draft.skillColor;
        skillImage.preserveAspect = false;
        skillImage.raycastTarget = false;
        skillImage.enabled = skillImage.sprite != null;

        var nodeUi = nodeObject.AddComponent<SkillTreeNodeUI>();
        nodeUi.Initialize(draft.guid, nodeImage, skillImage);
    }

    static void CreateConnection(
        SkillConnectionDraftData draft,
        int connectionIndex,
        RectTransform parent,
        Vector2 positionScale,
        float uniformScale)
    {
        var connectionObject = CreateRectObject($"Connection_{connectionIndex}", parent);
        var connectionRect = connectionObject.GetComponent<RectTransform>();
        Stretch(connectionRect);

        var connectionUi = connectionObject.AddComponent<SkillTreeConnectionUI>();
        connectionUi.Initialize(draft.parentNodeGuid, draft.childNodeGuid);

        var thickness = Mathf.Max(1f, k_ConnectionThickness * uniformScale);
        var sprite = SkillTreeAssetReferenceUtility.Load<Sprite>(
            draft.spriteGuid,
            draft.spriteLocalId);
        for (var i = 0; i < draft.pathPoints.Count - 1; i++)
        {
            var start = ScalePosition(draft.pathPoints[i], positionScale);
            var end = ScalePosition(draft.pathPoints[i + 1], positionScale);
            var segmentObject = CreateImageObject(
                $"Segment_{i}",
                connectionRect,
                out var image);
            var segmentRect = segmentObject.GetComponent<RectTransform>();
            Center(segmentRect);
            segmentRect.anchoredPosition = (start + end) * 0.5f;
            var horizontal = Mathf.Abs(start.x - end.x) >= Mathf.Abs(start.y - end.y);
            var segmentLength = Vector2.Distance(start, end) + thickness;
            segmentRect.sizeDelta = new Vector2(segmentLength, thickness);
            segmentRect.localEulerAngles = horizontal
                ? Vector3.zero : new Vector3(0f, 0f, 90f);
            image.sprite = sprite;
            image.color = draft.color;
            image.type = sprite != null && draft.repeatSprite
                ? Image.Type.Tiled : Image.Type.Simple;
            image.preserveAspect = false;
            if (sprite != null && draft.repeatSprite)
            {
                image.pixelsPerUnitMultiplier = sprite.rect.height
                    / (sprite.pixelsPerUnit * thickness);
            }
            image.raycastTarget = false;
        }
    }

    static Vector2 ScalePosition(Vector2 position, Vector2 scale)
    {
        return new Vector2(position.x * scale.x, -position.y * scale.y);
    }

    static GameObject CreateRectObject(string name, RectTransform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    static GameObject CreateImageObject(
        string name,
        RectTransform parent,
        out Image image)
    {
        var gameObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        gameObject.transform.SetParent(parent, false);
        image = gameObject.GetComponent<Image>();
        return gameObject;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void Center(RectTransform rect)
    {
        rect.anchorMin = Vector2.one * 0.5f;
        rect.anchorMax = Vector2.one * 0.5f;
        rect.pivot = Vector2.one * 0.5f;
    }
}
