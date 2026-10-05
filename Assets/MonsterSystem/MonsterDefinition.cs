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
    [SerializeField] Vector3 scaleCorrection = Vector3.one;
    [SerializeField] float rotationSpeed = 1f;
    [SerializeField] MonsterMovementType movementType;
    [SerializeField] float navMeshSpeed = 3.5f;
    [SerializeField] float navMeshAngularSpeed = 120f;
    [SerializeField] ObstacleAvoidanceType navMeshAvoidanceQuality = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
    [SerializeField, Range(0, 99)] int navMeshAvoidancePriority = 50;
    [SerializeField] bool isRotated = false;
    [SerializeField, HideInInspector] string[] rotationPointPaths;
    [SerializeField] float diameter = 1f;

    public string MonsterName => monsterName;
    public int BaseHp => baseHp;
    public GameObject ModelPrefab => modelPrefab;
    public Vector3 ScaleCorrection => scaleCorrection;
    public float RotationSpeed => rotationSpeed;
    public MonsterMovementType MovementType => movementType;
    public float NavMeshSpeed => navMeshSpeed;
    public float NavMeshAngularSpeed => navMeshAngularSpeed;
    public ObstacleAvoidanceType NavMeshAvoidanceQuality => navMeshAvoidanceQuality;
    public int NavMeshAvoidancePriority => navMeshAvoidancePriority;
    public bool IsRotated => isRotated;
    public float Diameter => diameter;
    public string[] RotationPointPaths => rotationPointPaths;
}
