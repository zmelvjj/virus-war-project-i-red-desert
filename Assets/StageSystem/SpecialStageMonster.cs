using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpecialStageMonster : MonoBehaviour
{
    public string SpecialStageId { get; private set; }

    public void Initialize(string specialStageId)
    {
        SpecialStageId = specialStageId;
    }
}
