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
    NavMeshPath m_CandidatePath;
    float m_RollAngle;
    Vector3 m_Destination;
    Vector3 m_LastPosition;
    bool m_HasDestination;
    bool m_IsQueued;
    bool m_IsTurning;
    bool m_IsInitialized;
    float m_StuckCheckInterval;
    float m_StuckDuration;
    float m_StuckMinimumMovement;
    float m_RepathCooldown;
    float m_BlockedMovementRatio;
    float m_BlockedArrivalDuration;
    float m_LastStuckCheckTime;
    float m_StuckElapsed;
    float m_RepathCooldownUntil;
    float m_BlockedMovementElapsed;
    Vector3 m_LastStuckCheckPosition;
    Vector3 m_LastFramePosition;

    public NavMeshAgent Agent => m_Agent;
    public Vector3 AreaCenter => m_AreaCenter;
    public float AreaRange => m_AreaRange;
    public NavMeshPath CandidatePath => m_CandidatePath;

    public void Initialize(
        NavMeshAgent agent,
        Vector3 areaCenter,
        float areaRange,
        float rotationSpeed,
        float diameter,
        Transform[] rotationPoints,
        Transform[] rotationCompensationPoints,
        float stuckCheckInterval,
        float stuckDuration,
        float stuckMinimumMovement,
        float repathCooldown,
        float blockedMovementRatio,
        float blockedArrivalDuration)
    {
        m_CandidatePath = new NavMeshPath();
        m_Agent = agent;
        m_AreaCenter = areaCenter;
        m_AreaRange = areaRange;
        m_RotationSpeed = rotationSpeed;
        m_Diameter = diameter;
        m_RotationPoints = rotationPoints;
        m_RotationCompensationPoints = rotationCompensationPoints;
        m_StuckCheckInterval = stuckCheckInterval;
        m_StuckDuration = stuckDuration;
        m_StuckMinimumMovement = stuckMinimumMovement;
        m_RepathCooldown = repathCooldown;
        m_BlockedMovementRatio = blockedMovementRatio;
        m_BlockedArrivalDuration = blockedArrivalDuration;
        CacheRotationState();
        m_LastPosition = agent.transform.position;
        m_IsInitialized = true;
        MonsterManager.Instance.RegisterNavigation(this);
        ResetStuckObservation();
        ResetBlockedMovementObservation();
        QueueNextPath();
    }

    void OnEnable()
    {
        if (m_IsInitialized && MonsterManager.HasInstance)
            MonsterManager.Instance.RegisterNavigation(this);
    }

    void OnDisable()
    {
        if (m_IsInitialized && MonsterManager.HasInstance)
            MonsterManager.Instance.UnregisterNavigation(this);

        m_IsQueued = false;
        m_HasDestination = false;
        m_IsTurning = false;

        if (m_Agent != null && m_Agent.isOnNavMesh)
        {
            m_Agent.isStopped = true;
            m_Agent.ResetPath();
        }
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
        if (!GameFlowManager.IsMainGame)
        {
            if (m_Agent.isOnNavMesh)
                m_Agent.isStopped = true;

            ResetStuckObservation();
            ResetBlockedMovementObservation();
            return;
        }

        if (m_HasDestination && !m_IsTurning && m_Agent.isOnNavMesh)
            m_Agent.isStopped = false;

        ApplyAgentPosition();
        UpdateRolling();

        if (!m_HasDestination || m_Agent.pathPending)
            return;

        if (!m_Agent.hasPath || m_Agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            AbandonDestination();
            QueueNextPath();
            return;
        }

        if (m_IsTurning)
        {
            ResetStuckObservation();
            ResetBlockedMovementObservation();
            RotateBeforeMoving();
            return;
        }

        FaceMovementDirection();

        if (m_Agent.remainingDistance <= m_Agent.stoppingDistance)
        {
            m_Agent.isStopped = true;
            m_HasDestination = false;
            ResetStuckObservation();
            ResetBlockedMovementObservation();
            QueueNextPath();
            return;
        }

        if (DetectBlockedMovement())
            return;

        DetectStuck();
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

        if (MonsterManager.Instance.TryAssignNavigationDestination(
                this,
                out var destination,
                out var path)
            && m_Agent.SetPath(path))
        {
            m_Destination = destination;
            m_Agent.isStopped = true;
            m_HasDestination = true;
            m_IsTurning = true;
            ResetStuckObservation();
            ResetBlockedMovementObservation();
            return;
        }

        MonsterManager.Instance.ReleaseNavigationDestination(this);
        QueueNextPath();
    }

    void DetectStuck()
    {
        if (Time.time < m_LastStuckCheckTime + m_StuckCheckInterval)
            return;

        var elapsed = Time.time - m_LastStuckCheckTime;
        var movedDistance = Vector3.Distance(
            m_LastStuckCheckPosition,
            m_Agent.transform.position);
        m_LastStuckCheckTime = Time.time;
        m_LastStuckCheckPosition = m_Agent.transform.position;

        if (movedDistance >= m_StuckMinimumMovement)
        {
            m_StuckElapsed = 0f;
            return;
        }

        m_StuckElapsed += elapsed;
        if (m_StuckElapsed < m_StuckDuration || Time.time < m_RepathCooldownUntil)
            return;

        m_RepathCooldownUntil = Time.time + m_RepathCooldown;
        AbandonDestination();
        QueueNextPath();
    }

    bool DetectBlockedMovement()
    {
        var currentPosition = m_Agent.transform.position;
        var actualDelta = currentPosition - m_LastFramePosition;
        actualDelta.y = 0f;
        m_LastFramePosition = currentPosition;

        var expectedDelta = m_Agent.speed * Time.deltaTime;
        if (expectedDelta <= 0f
            || actualDelta.magnitude >= expectedDelta * m_BlockedMovementRatio)
        {
            m_BlockedMovementElapsed = 0f;
            return false;
        }

        m_BlockedMovementElapsed += Time.deltaTime;
        if (m_BlockedMovementElapsed < m_BlockedArrivalDuration)
            return false;

        AbandonDestination();
        QueueNextPath();
        return true;
    }

    void AbandonDestination()
    {
        MonsterManager.Instance.ReleaseNavigationDestination(this);
        if (m_Agent.isOnNavMesh)
        {
            m_Agent.isStopped = true;
            m_Agent.ResetPath();
        }

        m_HasDestination = false;
        m_IsTurning = false;
        ResetStuckObservation();
        ResetBlockedMovementObservation();
    }

    void ResetStuckObservation()
    {
        m_LastStuckCheckTime = Time.time;
        m_LastStuckCheckPosition = m_Agent != null
            ? m_Agent.transform.position
            : transform.position;
        m_StuckElapsed = 0f;
    }

    void ResetBlockedMovementObservation()
    {
        m_LastFramePosition = m_Agent != null
            ? m_Agent.transform.position
            : transform.position;
        m_BlockedMovementElapsed = 0f;
    }
}
