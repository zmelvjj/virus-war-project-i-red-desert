using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RangeDamageDealer : MonoBehaviour
{
    [SerializeField] Material hitMaterial;

    BoxCollider m_RangeCollider;
    readonly HashSet<MonsterEntity> m_Targets = new();

    IEnumerator Start()
    {
        m_RangeCollider = GetComponent<BoxCollider>();

        while (true)
        {
            yield return new WaitForSeconds(1f / PlayerStats.Instance.AttackSpeed);
            if (GameFlowManager.IsMainGame)
                Attack();
        }
    }

    void Attack()
    {
        var center = transform.TransformPoint(m_RangeCollider.center);
        var halfExtents = Vector3.Scale(m_RangeCollider.size * 0.5f, Abs(transform.lossyScale));
        var colliders = Physics.OverlapBox(center, halfExtents, transform.rotation);

        m_Targets.Clear();
        foreach (var hit in colliders)
        {
            var monster = hit.GetComponentInParent<MonsterEntity>();
            if (monster != null)
                m_Targets.Add(monster);
        }

        var damage = Mathf.RoundToInt(PlayerStats.Instance.Damage);
        foreach (var monster in m_Targets)
        {
            var hitPosition = monster.transform.position;
            monster.TakeDamage(damage, hitMaterial);
            BasicAttackEvents.Raise(new BasicAttackHit(monster, hitPosition, damage));
        }
    }

    static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
