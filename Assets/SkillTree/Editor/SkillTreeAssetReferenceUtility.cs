using UnityEditor;

public static class SkillTreeAssetReferenceUtility
{
    const string k_DefaultNodeSpritePath =
        "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";

    public static void GetDefaultNodeSpriteReference(out string guid, out long localFileId)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(k_DefaultNodeSpritePath);
        GetReference(sprite, out guid, out localFileId);
    }

    public static void GetReference(
        UnityEngine.Object asset,
        out string guid,
        out long localFileId)
    {
        if (asset == null)
        {
            guid = string.Empty;
            localFileId = 0;
            return;
        }

        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out guid, out localFileId);
    }

    public static T Load<T>(string guid, long localFileId) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(guid))
            return null;

        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (localFileId != 0)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is not T typedAsset)
                    continue;

                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    typedAsset,
                    out _,
                    out long assetLocalFileId);
                if (assetLocalFileId == localFileId)
                    return typedAsset;
            }
        }

        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
