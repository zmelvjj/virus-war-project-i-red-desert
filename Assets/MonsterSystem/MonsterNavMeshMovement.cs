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
    Quaternion[] m_RotationPointBaseRotations;
    Transform[] m_RotationCompensationPoints;
    Quaternion[] m_CompensationWorldRotations;
    float m_RollAngle;
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
        Transform[] rotationPoints,
        Transform[] rotationCompensationPoints)
    {
        m_Agent = agent;
        m_AreaCenter = areaCenter;
        m_AreaRange = areaRange;
        m_RotationSpeed = rotationSpeed;
        m_Diameter = diameter;
        m_RotationPoints = rotationPoints;
        m_RotationCompensationPoints = rotationCompensationPoints;
        CacheRotationState();
        m_LastPosition = agent.transform.position;
        QueueNextPath();
    }

    void CacheRotationState()
    {
        if (m_RotationPoints != null)
        {
            m_RotationPointBaseRotations = new Quaternion[m_RotationPoints.Length];
            for (var i = 0; i < m_RotationPoints.Length; i++)
            {
                if (m_RotationPoints[i] != null)
                    m_RotationPointBaseRotations[i] = m_RotationPoints[i].localRotation;
            }
        }

        if (m_RotationCompensationPoints != null)
            m_CompensationWorldRotations = new Quaternion[m_RotationCompensationPoints.Length];
    }

    void Update()
    {
        ApplyAgentPosition();
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
        var nextRotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            m_RotationSpeed * Time.deltaTime);
        SetRootRotation(nextRotation);

        if (Quaternion.Angle(transform.rotation, targetRotation) > k_FacingTolerance)
            return;

        SetRootRotation(targetRotation);
        m_IsTurning = false;
        m_Agent.isStopped = false;
    }

    void FaceMovementDirection()
    {
        var direction = m_Agent.desiredVelocity;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            SetRootRotation(Quaternion.LookRotation(direction));
    }

    void ApplyAgentPosition()
    {
        if (!m_Agent.isOnNavMesh)
            return;

        transform.position += m_Agent.nextPosition - m_Agent.transform.position;
    }

    void SetRootRotation(Quaternion rotation)
    {
        CacheCompensationRotations();
        var agentPosition = m_Agent.transform.position;
        transform.rotation = rotation;
        transform.position += agentPosition - m_Agent.transform.position;
        RestoreCompensationRotations();
    }

    void CacheCompensationRotations()
    {
        if (m_RotationCompensationPoints == null)
            return;

        for (var i = 0; i < m_RotationCompensationPoints.Length; i++)
        {
            var point = m_RotationCompensationPoints[i];
            if (point != null)
                m_CompensationWorldRotations[i] = point.rotation;
        }
    }

    void RestoreCompensationRotations()
    {
        if (m_RotationCompensationPoints == null)
            return;

        for (var i = 0; i < m_RotationCompensationPoints.Length; i++)
        {
            var point = m_RotationCompensationPoints[i];
            if (point != null)
                point.rotation = m_CompensationWorldRotations[i];
        }
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
        m_RollAngle = Mathf.Repeat(m_RollAngle + rotationDegrees, 360f);
        for (var i = 0; i < m_RotationPoints.Length; i++)
        {
            var point = m_RotationPoints[i];
            if (point == null)
                continue;

            point.localRotation = m_RotationPointBaseRotations[i] *
                Quaternion.AngleAxis(m_RollAngle, Vector3.right);
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
