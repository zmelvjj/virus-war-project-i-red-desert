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

        if (m_Definition.Material != null)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                renderer.sharedMaterial = m_Definition.Material;
        }

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

        var movement = instance.GetComponent<MonsterNavMeshMovement>();
        if (movement == null)
            movement = instance.AddComponent<MonsterNavMeshMovement>();

        movement.Initialize(agent, m_MovementCenter, m_MovementRange);
        return MonsterMovementType.NavMesh;
    }

    static void PlaceOnGround(GameObject instance, float groundY)
    {
        var renderer = instance.GetComponentInChildren<Renderer>();
        if (renderer != null)
            instance.transform.position += Vector3.up * (groundY - renderer.bounds.min.y);
    }
}
