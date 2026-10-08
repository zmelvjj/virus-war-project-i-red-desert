using UnityEngine;

[DisallowMultipleComponent]
public sealed class GeneratedSkillTreeMarker : MonoBehaviour
{
    [SerializeField] string generationId;
    [SerializeField] string assetFolderPath;
    [SerializeField] SkillTreeData skillTreeData;

    public string GenerationId => generationId;
    public string AssetFolderPath => assetFolderPath;
    public SkillTreeData SkillTreeData => skillTreeData;

    public void Initialize(string id, string folderPath, SkillTreeData data)
    {
        generationId = id;
        assetFolderPath = folderPath;
        skillTreeData = data;
    }
}
