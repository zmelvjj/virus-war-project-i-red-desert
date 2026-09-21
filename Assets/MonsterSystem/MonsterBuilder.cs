using UnityEngine;

public sealed class MonsterBuilder
{
    readonly MonsterDefinition m_Definition;
    Vector3 m_Position;
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

        entity.Initialize(m_Definition, m_Hp);
        MonsterManager.Instance.Register(entity);
        return entity;
    }

    static void PlaceOnGround(GameObject instance, float groundY)
    {
        var renderer = instance.GetComponentInChildren<Renderer>();
        if (renderer != null)
            instance.transform.position += Vector3.up * (groundY - renderer.bounds.min.y);
    }
}
