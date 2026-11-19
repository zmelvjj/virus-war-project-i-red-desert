using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using System.Collections.Generic;

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
    [FormerlySerializedAs("rewardDNA")]
    [SerializeField] int minimumRewardDNA = 5;
    [SerializeField] int maximumRewardDNA = 5;
    [SerializeField] List<MonsterTraitType> traits = new();
    [SerializeField] GameObject modelPrefab;
    [SerializeField] Vector3 scaleCorrection = Vector3.one;
    [SerializeField] float rotationSpeed = 1f;
    [SerializeField] MonsterMovementType movementType;
    [SerializeField] float navMeshSpeed = 3.5f;
    [SerializeField] float navMeshAngularSpeed = 120f;
    [SerializeField] ObstacleAvoidanceType navMeshAvoidanceQuality = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
    [SerializeField, Range(0, 99)] int navMeshAvoidancePriority = 50;
    [SerializeField, Min(0.05f)] float stuckCheckInterval = 0.75f;
    [SerializeField, Min(0.1f)] float stuckDuration = 4f;
    [SerializeField, Min(0f)] float stuckMinimumMovement = 0.1f;
    [SerializeField, Min(0f)] float repathCooldown = 3f;
    [SerializeField, Range(0f, 1f)] float blockedMovementRatio = 0.15f;
    [SerializeField, Min(0.1f)] float blockedArrivalDuration = 1f;
    [SerializeField] bool isRotated = false;
    [SerializeField, HideInInspector] string[] rotationPointPaths;
    [SerializeField] float diameter = 1f;

    public string MonsterName => monsterName;
    public int BaseHp => baseHp;
    public int MinimumRewardDNA => minimumRewardDNA;
    public int MaximumRewardDNA => maximumRewardDNA;
    public IReadOnlyList<MonsterTraitType> Traits => traits;
    public GameObject ModelPrefab => modelPrefab;
    public Vector3 ScaleCorrection => scaleCorrection;
    public float RotationSpeed => rotationSpeed;
    public MonsterMovementType MovementType => movementType;
    public float NavMeshSpeed => navMeshSpeed;
    public float NavMeshAngularSpeed => navMeshAngularSpeed;
    public ObstacleAvoidanceType NavMeshAvoidanceQuality => navMeshAvoidanceQuality;
    public int NavMeshAvoidancePriority => navMeshAvoidancePriority;
    public float StuckCheckInterval => stuckCheckInterval;
    public float StuckDuration => stuckDuration;
    public float StuckMinimumMovement => stuckMinimumMovement;
    public float RepathCooldown => repathCooldown;
    public float BlockedMovementRatio => blockedMovementRatio;
    public float BlockedArrivalDuration => blockedArrivalDuration;
    public bool IsRotated => isRotated;
    public float Diameter => diameter;
    public string[] RotationPointPaths => rotationPointPaths;
}
