using UnityEngine;

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
        nodeImage.color = draft.nodeColor;
        nodeImage.preserveAspect = false;
        nodeImage.raycastTarget = false;
        nodeImage.enabled = nodeImage.sprite != null;

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
    }

    static void CreateConnection(
        SkillConnectionDraftData draft,
        int connectionIndex,
        RectTransform parent,
        Vector2 positionScale,
        float uniformScale)
    {
        var thickness = Mathf.Max(1f, k_ConnectionThickness * uniformScale);
        for (var i = 0; i < draft.pathPoints.Count - 1; i++)
        {
            var start = ScalePosition(draft.pathPoints[i], positionScale);
            var end = ScalePosition(draft.pathPoints[i + 1], positionScale);
            var segmentObject = CreateImageObject(
                $"Connection_{connectionIndex}_Segment_{i}",
                parent,
                out var image);
            var segmentRect = segmentObject.GetComponent<RectTransform>();
            Center(segmentRect);
            segmentRect.anchoredPosition = (start + end) * 0.5f;
            segmentRect.sizeDelta = Mathf.Abs(start.x - end.x) >= Mathf.Abs(start.y - end.y)
                ? new Vector2(Mathf.Abs(start.x - end.x), thickness)
                : new Vector2(thickness, Mathf.Abs(start.y - end.y));
            image.color = new Color(0.68f, 0.72f, 0.78f);
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
        out UnityEngine.UI.Image image)
    {
        var gameObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image));
        gameObject.transform.SetParent(parent, false);
        image = gameObject.GetComponent<UnityEngine.UI.Image>();
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
