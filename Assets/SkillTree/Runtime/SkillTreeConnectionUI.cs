using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillTreeConnectionUI : MonoBehaviour
{
    [SerializeField] string parentNodeGuid;
    [SerializeField] string childNodeGuid;

    public string ParentNodeGuid => parentNodeGuid;
    public string ChildNodeGuid => childNodeGuid;

    public void Initialize(string parentGuid, string childGuid)
    {
        parentNodeGuid = parentGuid;
        childNodeGuid = childGuid;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
