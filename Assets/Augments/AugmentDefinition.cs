using UnityEngine;

public enum AugmentType
{
    InfectionArea = 0,
    InfectionLink = 1,
    ReplicationGauge = 2
}

[CreateAssetMenu(fileName = "AugmentDefinition", menuName = "Augments/Augment Definition")]
public sealed class AugmentDefinition : ScriptableObject
{
    [SerializeField] AugmentType augmentType;
    [SerializeField] GameObject cardUIPrefab;

    public AugmentType AugmentType => augmentType;
    public GameObject CardUIPrefab => cardUIPrefab;
}
