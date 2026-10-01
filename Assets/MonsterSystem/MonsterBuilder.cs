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

        var agent = instance.GetComponentInChildren<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogWarning($"[MonsterBuilder] '{m_Definition.MonsterName}' has no NavMeshAgent. Movement changed to Waiting.", instance);
            return MonsterMovementType.Waiting;
        }

        agent.speed = m_Definition.NavMeshSpeed;
        agent.angularSpeed = m_Definition.NavMeshAngularSpeed;
        agent.obstacleAvoidanceType = m_Definition.NavMeshAvoidanceQuality;
        agent.avoidancePriority = m_Definition.NavMeshAvoidancePriority;
        agent.updateRotation = false;

        var movement = instance.GetComponent<MonsterNavMeshMovement>();
        if (movement == null)
            movement = instance.AddComponent<MonsterNavMeshMovement>();

        movement.Initialize(
            agent,
            m_MovementCenter,
            m_MovementRange,
            m_Definition.RotationSpeed,
            m_Definition.IsRotated ? m_Definition.Diameter : 0f,
            ResolveRotationPoints(instance));
        return MonsterMovementType.NavMesh;
    }

    Transform[] ResolveRotationPoints(GameObject instance)
    {
        if (!m_Definition.IsRotated || m_Definition.RotationPoints == null)
            return null;

        var points = new Transform[m_Definition.RotationPoints.Length];
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = ResolvePrefabTransform(
                m_Definition.RotationPoints[i],
                m_Definition.ModelPrefab.transform,
                instance.transform);
        }

        return points;
    }

    static Transform ResolvePrefabTransform(Transform source, Transform prefabRoot, Transform instanceRoot)
    {
        if (source == null)
            return null;

        var siblingIndices = new List<int>();
        var current = source;
        while (current != null && current != prefabRoot)
        {
            siblingIndices.Add(current.GetSiblingIndex());
            current = current.parent;
        }

        if (current != prefabRoot)
            return null;

        var result = instanceRoot;
        for (var i = siblingIndices.Count - 1; i >= 0; i--)
            result = result.GetChild(siblingIndices[i]);

        return result;
    }

    static void PlaceOnGround(GameObject instance, float groundY)
    {
        var renderer = instance.GetComponentInChildren<Renderer>();
        if (renderer != null)
            instance.transform.position += Vector3.up * (groundY - renderer.bounds.min.y);
    }
}
