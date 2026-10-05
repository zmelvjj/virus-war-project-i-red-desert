using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class MonsterBuilder
{
    readonly MonsterDefinition m_Definition;
    Vector3 m_Position;
    Vector3 m_MovementCenter;
    float m_MovementRange;
    int m_Hp;

    public MonsterBuilder(MonsterDefinition definition)
    {
        m_Definition = definition;
        m_Hp = definition.BaseHp;
    }

    public MonsterBuilder At(Vector3 position)
    {
        m_Position = position;
        return this;
    }

    public MonsterBuilder WithHp(int hp)
    {
        m_Hp = hp;
        return this;
    }

    public MonsterBuilder Within(Vector3 center, float range)
    {
        m_MovementCenter = center;
        m_MovementRange = range;
        return this;
    }

    public MonsterEntity Build()
    {
        var instance = Object.Instantiate(m_Definition.ModelPrefab, m_Position, Quaternion.identity);
        instance.name = m_Definition.MonsterName;
        instance.transform.localScale = Vector3.Scale(instance.transform.localScale, m_Definition.ScaleCorrection);

        PlaceOnGround(instance, m_Position.y);

        var entity = instance.GetComponent<MonsterEntity>();
        if (entity == null)
            entity = instance.AddComponent<MonsterEntity>();

        var movementType = ConfigureMovement(instance);
        entity.Initialize(m_Definition, m_Hp, movementType);
        MonsterManager.Instance.Register(entity);
        return entity;
    }

    MonsterMovementType ConfigureMovement(GameObject instance)
    {
        if (m_Definition.MovementType != MonsterMovementType.NavMesh)
            return MonsterMovementType.Waiting;

        var agents = instance.GetComponentsInChildren<NavMeshAgent>();
        if (agents.Length == 0)
        {
            Debug.LogWarning($"[MonsterBuilder] '{m_Definition.MonsterName}' has no NavMeshAgent. Movement changed to Waiting.", instance);
            return MonsterMovementType.Waiting;
        }

        var agent = instance.GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = agents[0];

        for (var i = 0; i < agents.Length; i++)
        {
            if (agents[i] != agent)
                agents[i].enabled = false;
        }

        agent.speed = m_Definition.NavMeshSpeed;
        agent.angularSpeed = m_Definition.NavMeshAngularSpeed;
        agent.obstacleAvoidanceType = m_Definition.NavMeshAvoidanceQuality;
        agent.avoidancePriority = m_Definition.NavMeshAvoidancePriority;
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        var movement = instance.GetComponent<MonsterNavMeshMovement>();
        if (movement == null)
            movement = instance.AddComponent<MonsterNavMeshMovement>();

        var rotationPoints = ResolveRotationPoints(instance);
        movement.Initialize(
            agent,
            m_MovementCenter,
            m_MovementRange,
            m_Definition.RotationSpeed,
            m_Definition.IsRotated ? m_Definition.Diameter : 0f,
            rotationPoints,
            ResolveRotationCompensationPoints(instance, rotationPoints));
        return MonsterMovementType.NavMesh;
    }

    Transform[] ResolveRotationPoints(GameObject instance)
    {
        if (!m_Definition.IsRotated || m_Definition.RotationPointPaths == null)
            return null;

        var points = new Transform[m_Definition.RotationPointPaths.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = instance.transform.Find(m_Definition.RotationPointPaths[i]);

        return points;
    }

    Transform[] ResolveRotationCompensationPoints(GameObject instance, Transform[] rotationPoints)
    {
        if (!m_Definition.IsRotated || rotationPoints == null || rotationPoints.Length == 0)
            return null;

        var points = new List<Transform>();
        foreach (Transform child in instance.transform)
        {
            var containsRotationPoint = false;
            for (var i = 0; i < rotationPoints.Length; i++)
            {
                var rotationPoint = rotationPoints[i];
                if (rotationPoint != null && (rotationPoint == child || rotationPoint.IsChildOf(child)))
                {
                    containsRotationPoint = true;
                    break;
                }
            }

            if (!containsRotationPoint)
                points.Add(child);
        }

        return points.ToArray();
    }

    static void PlaceOnGround(GameObject instance, float groundY)
    {
        var renderer = instance.GetComponentInChildren<Renderer>();
        if (renderer != null)
            instance.transform.position += Vector3.up * (groundY - renderer.bounds.min.y);
    }
}
