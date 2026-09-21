using UnityEngine;

public enum MonsterMovementType
{
    Waiting,
    NavMesh
}

[CreateAssetMenu(fileName = "MonsterDefinition", menuName = "Monsters/Monster Definition")]
public class MonsterDefinition : ScriptableObject
{
    [SerializeField] string monsterName;
    [SerializeField] int baseHp = 100;
    [SerializeField] GameObject modelPrefab;
    [SerializeField] Material material;
    [SerializeField] Vector3 scaleCorrection = Vector3.one;
    [SerializeField] MonsterMovementType movementType;

    public string MonsterName => monsterName;
    public int BaseHp => baseHp;
    public GameObject ModelPrefab => modelPrefab;
    public Material Material => material;
    public Vector3 ScaleCorrection => scaleCorrection;
    public MonsterMovementType MovementType => movementType;
}
