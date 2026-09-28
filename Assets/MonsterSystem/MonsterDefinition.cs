using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] float navMeshSpeed = 3.5f;
    [SerializeField] float navMeshAngularSpeed = 120f;
    [SerializeField] ObstacleAvoidanceType navMeshAvoidanceQuality = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
    [SerializeField, Range(0, 99)] int navMeshAvoidancePriority = 50;

    public string MonsterName => monsterName;
    public int BaseHp => baseHp;
    public GameObject ModelPrefab => modelPrefab;
    public Material Material => material;
    public Vector3 ScaleCorrection => scaleCorrection;
    public MonsterMovementType MovementType => movementType;
    public float NavMeshSpeed => navMeshSpeed;
    public float NavMeshAngularSpeed => navMeshAngularSpeed;
    public ObstacleAvoidanceType NavMeshAvoidanceQuality => navMeshAvoidanceQuality;
    public int NavMeshAvoidancePriority => navMeshAvoidancePriority;
}
