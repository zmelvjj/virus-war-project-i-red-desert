using UnityEngine;
using UnityEngine.AI;

public class MonsterNavMeshMovement : MonoBehaviour
{
    NavMeshAgent m_Agent;
    Vector3 m_AreaCenter;
    float m_AreaRange;
    bool m_HasDestination;
    bool m_IsQueued;

    public void Initialize(NavMeshAgent agent, Vector3 areaCenter, float areaRange)
    {
        m_Agent = agent;
        m_AreaCenter = areaCenter;
        m_AreaRange = areaRange;
        QueueNextPath();
    }

    void Update()
    {
        if (!m_HasDestination || m_Agent.pathPending)
            return;

        if (!m_Agent.hasPath || m_Agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            m_HasDestination = false;
            QueueNextPath();
            return;
        }

        if (m_Agent.remainingDistance > m_Agent.stoppingDistance)
            return;

        m_HasDestination = false;
        QueueNextPath();
    }

    void QueueNextPath()
    {
        if (m_IsQueued)
            return;

        m_IsQueued = true;
        MonsterManager.Instance.QueueNavigation(this);
    }

    public void CalculateNextPath()
    {
        m_IsQueued = false;

        var randomPoint = Random.insideUnitCircle * m_AreaRange;
        var candidate = m_AreaCenter + new Vector3(randomPoint.x, 0f, randomPoint.y);
        if (NavMesh.SamplePosition(candidate, out var hit, m_Agent.height, m_Agent.areaMask) &&
            m_Agent.SetDestination(hit.position))
        {
            m_HasDestination = true;
            return;
        }

        QueueNextPath();
    }
}
