using UnityEngine;
using UnityEngine.AI;

public class MonsterNavMeshMovement : MonoBehaviour
{
    const float k_FacingTolerance = 0.1f;

    NavMeshAgent m_Agent;
    Vector3 m_AreaCenter;
    float m_AreaRange;
    float m_RotationSpeed;
    float m_Diameter;
    Transform[] m_RotationPoints;
    Vector3 m_Destination;
    Vector3 m_LastPosition;
    bool m_HasDestination;
    bool m_IsQueued;
    bool m_IsTurning;

    public void Initialize(
        NavMeshAgent agent,
        Vector3 areaCenter,
        float areaRange,
        float rotationSpeed,
        float diameter,
        Transform[] rotationPoints)
    {
        m_Agent = agent;
        m_AreaCenter = areaCenter;
        m_AreaRange = areaRange;
        m_RotationSpeed = rotationSpeed;
        m_Diameter = diameter;
        m_RotationPoints = rotationPoints;
        m_LastPosition = agent.transform.position;
        QueueNextPath();
    }

    void Update()
    {
        UpdateRolling();

        if (!m_HasDestination || m_Agent.pathPending)
            return;

        if (!m_Agent.hasPath || m_Agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            m_Agent.isStopped = true;
            m_HasDestination = false;
            m_IsTurning = false;
            QueueNextPath();
            return;
        }

        if (m_IsTurning)
        {
            RotateBeforeMoving();
            return;
        }

        FaceMovementDirection();

        if (m_Agent.remainingDistance > m_Agent.stoppingDistance)
            return;

        m_Agent.isStopped = true;
        m_HasDestination = false;
        QueueNextPath();
    }

    void RotateBeforeMoving()
    {
        var direction = GetPathDirection();
        if (direction.sqrMagnitude <= 0.0001f)
        {
            m_IsTurning = false;
            m_Agent.isStopped = false;
            return;
        }

        var targetRotation = Quaternion.LookRotation(direction);
        m_Agent.transform.rotation = Quaternion.RotateTowards(
            m_Agent.transform.rotation,
            targetRotation,
            m_RotationSpeed * Time.deltaTime);

        if (Quaternion.Angle(m_Agent.transform.rotation, targetRotation) > k_FacingTolerance)
            return;

        m_Agent.transform.rotation = targetRotation;
        m_IsTurning = false;
        m_Agent.isStopped = false;
    }

    void FaceMovementDirection()
    {
        var direction = m_Agent.desiredVelocity;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            m_Agent.transform.rotation = Quaternion.LookRotation(direction);
    }

    Vector3 GetPathDirection()
    {
        var direction = m_Agent.steeringTarget - m_Agent.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = m_Destination - m_Agent.transform.position;
            direction.y = 0f;
        }

        return direction.normalized;
    }

    void UpdateRolling()
    {
        var currentPosition = m_Agent.transform.position;
        var movement = currentPosition - m_LastPosition;
        movement.y = 0f;
        m_LastPosition = currentPosition;

        if (m_Diameter <= 0f || m_RotationPoints == null)
            return;

        var rotationDegrees = movement.magnitude / (Mathf.PI * m_Diameter) * 360f;
        for (var i = 0; i < m_RotationPoints.Length; i++)
        {
            var point = m_RotationPoints[i];
            if (point == null)
                continue;

            var eulerAngles = point.localEulerAngles;
            eulerAngles.x = Mathf.Repeat(eulerAngles.x + rotationDegrees, 360f);
            point.localEulerAngles = eulerAngles;
        }
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
            m_Destination = hit.position;
            m_Agent.isStopped = true;
            m_HasDestination = true;
            m_IsTurning = true;
            return;
        }

        QueueNextPath();
    }
}
