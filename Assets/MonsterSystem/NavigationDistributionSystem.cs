using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class NavigationDistributionSystem
{
    sealed class NavigationArea
    {
        public readonly Vector3 Center;
        public readonly float Range;
        public readonly List<NavigationSlot> Slots;

        public NavigationArea(Vector3 center, float range, List<NavigationSlot> slots)
        {
            Center = center;
            Range = range;
            Slots = slots;
        }
    }

    sealed class NavigationSlot
    {
        public readonly Vector3 Position;
        public MonsterNavMeshMovement ReservedBy;

        public NavigationSlot(Vector3 position)
        {
            Position = position;
        }
    }

    readonly struct Candidate
    {
        public readonly NavigationSlot Slot;
        public readonly float Score;

        public Candidate(NavigationSlot slot, float score)
        {
            Slot = slot;
            Score = score;
        }
    }

    readonly int m_SlotCount;
    readonly float m_SlotMinSpacing;
    readonly float m_DensityRadius;
    readonly float m_MinimumTravelDistanceRatio;
    readonly float m_PreferredTravelDistanceRatio;
    readonly float m_MaximumPreferredDistanceRatio;
    readonly int m_TopCandidateCount;
    readonly List<NavigationArea> m_Areas = new();
    readonly List<MonsterNavMeshMovement> m_Movements = new();
    readonly Dictionary<MonsterNavMeshMovement, NavigationSlot> m_Reservations = new();
    readonly List<Candidate> m_Candidates = new();

    public NavigationDistributionSystem(
        int slotCount,
        float slotMinSpacing,
        float densityRadius,
        float minimumTravelDistanceRatio,
        float preferredTravelDistanceRatio,
        float maximumPreferredDistanceRatio,
        int topCandidateCount)
    {
        m_SlotCount = Mathf.Max(1, slotCount);
        m_SlotMinSpacing = Mathf.Max(0f, slotMinSpacing);
        m_DensityRadius = Mathf.Max(0.01f, densityRadius);
        m_MinimumTravelDistanceRatio = Mathf.Clamp01(minimumTravelDistanceRatio);
        m_PreferredTravelDistanceRatio = Mathf.Clamp(
            preferredTravelDistanceRatio,
            m_MinimumTravelDistanceRatio,
            1f);
        m_MaximumPreferredDistanceRatio = Mathf.Clamp(
            maximumPreferredDistanceRatio,
            m_PreferredTravelDistanceRatio,
            1f);
        m_TopCandidateCount = Mathf.Max(1, topCandidateCount);
    }

    public void Register(MonsterNavMeshMovement movement)
    {
        if (!m_Movements.Contains(movement))
            m_Movements.Add(movement);
    }

    public void Unregister(MonsterNavMeshMovement movement)
    {
        Release(movement);
        m_Movements.Remove(movement);
    }

    public void Release(MonsterNavMeshMovement movement)
    {
        if (!m_Reservations.Remove(movement, out var slot))
            return;

        slot.ReservedBy = null;
    }

    public void Reset()
    {
        foreach (var reservation in m_Reservations)
            reservation.Value.ReservedBy = null;

        m_Reservations.Clear();
        m_Movements.Clear();
    }

    public bool TryRequestDestination(
        MonsterNavMeshMovement movement,
        out Vector3 destination,
        out NavMeshPath path)
    {
        destination = default;
        path = movement.CandidatePath;
        Release(movement);

        var agent = movement.Agent;
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return false;

        var area = GetOrCreateArea(movement.AreaCenter, movement.AreaRange);
        BuildCandidates(movement, area, false);
        if (TryReserveCandidate(movement, out destination, path))
            return true;

        BuildCandidates(movement, area, true);
        return TryReserveCandidate(movement, out destination, path);
    }

    bool TryReserveCandidate(
        MonsterNavMeshMovement movement,
        out Vector3 destination,
        NavMeshPath path)
    {
        destination = default;
        while (m_Candidates.Count > 0)
        {
            var candidateIndex = SelectWeightedTopCandidate();
            var candidate = m_Candidates[candidateIndex];
            m_Candidates.RemoveAt(candidateIndex);

            if (!TryBuildPath(movement, candidate.Slot.Position, path, out destination))
                continue;

            candidate.Slot.ReservedBy = movement;
            m_Reservations.Add(movement, candidate.Slot);
            return true;
        }

        return false;
    }

    NavigationArea GetOrCreateArea(Vector3 center, float range)
    {
        for (var i = 0; i < m_Areas.Count; i++)
        {
            var area = m_Areas[i];
            if ((area.Center - center).sqrMagnitude <= 0.0001f
                && Mathf.Approximately(area.Range, range))
            {
                return area;
            }
        }

        var newArea = new NavigationArea(center, range, GenerateSlots(center, range));
        m_Areas.Add(newArea);
        return newArea;
    }

    List<NavigationSlot> GenerateSlots(Vector3 center, float range)
    {
        var slots = new List<NavigationSlot>(m_SlotCount);
        var minSpacingSqr = m_SlotMinSpacing * m_SlotMinSpacing;
        var maxAttempts = m_SlotCount * 40;

        for (var attempt = 0; attempt < maxAttempts && slots.Count < m_SlotCount; attempt++)
        {
            var point = Random.insideUnitCircle * range;
            var position = center + new Vector3(point.x, 0f, point.y);
            var hasEnoughSpacing = true;

            for (var i = 0; i < slots.Count; i++)
            {
                if ((slots[i].Position - position).sqrMagnitude >= minSpacingSqr)
                    continue;

                hasEnoughSpacing = false;
                break;
            }

            if (hasEnoughSpacing)
                slots.Add(new NavigationSlot(position));
        }

        return slots;
    }

    void BuildCandidates(
        MonsterNavMeshMovement movement,
        NavigationArea area,
        bool minimumDistanceFallback)
    {
        m_Candidates.Clear();
        var currentPosition = movement.Agent.transform.position;
        var minimumDistance = area.Range * m_MinimumTravelDistanceRatio;
        var preferredDistance = Mathf.Max(
            minimumDistance,
            area.Range * m_PreferredTravelDistanceRatio);
        var maximumPreferredDistance = Mathf.Max(
            preferredDistance,
            area.Range * m_MaximumPreferredDistanceRatio);

        for (var i = 0; i < area.Slots.Count; i++)
        {
            var slot = area.Slots[i];
            if (slot.ReservedBy != null)
                continue;

            var travelDistance = HorizontalDistance(currentPosition, slot.Position);
            if (!minimumDistanceFallback && travelDistance < minimumDistance)
                continue;
            if (minimumDistanceFallback && travelDistance >= minimumDistance)
                continue;

            CalculateDensity(
                movement,
                slot.Position,
                out var agentDensity,
                out var reservationDensity);
            var distanceScore = CalculateDistanceScore(
                travelDistance,
                preferredDistance,
                maximumPreferredDistance);
            var score = distanceScore
                / ((1f + agentDensity * 2f)
                    * (1f + reservationDensity * 1.5f));
            m_Candidates.Add(new Candidate(slot, score));
        }

        m_Candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
    }

    void CalculateDensity(
        MonsterNavMeshMovement requester,
        Vector3 position,
        out float agentDensity,
        out float reservationDensity)
    {
        agentDensity = 0f;
        reservationDensity = 0f;

        for (var i = 0; i < m_Movements.Count; i++)
        {
            var movement = m_Movements[i];
            if (movement == null || movement == requester || !movement.isActiveAndEnabled)
                continue;

            agentDensity += DensityWeight(position, movement.transform.position);
        }

        foreach (var reservation in m_Reservations)
        {
            if (reservation.Key != requester)
                reservationDensity += DensityWeight(position, reservation.Value.Position);
        }
    }

    static float CalculateDistanceScore(
        float distance,
        float preferredDistance,
        float maximumPreferredDistance)
    {
        if (distance <= preferredDistance)
        {
            return Mathf.Max(
                0.05f,
                distance / Mathf.Max(0.01f, preferredDistance));
        }

        if (distance <= maximumPreferredDistance)
        {
            return Mathf.Lerp(
                1f,
                0.5f,
                Mathf.InverseLerp(
                    preferredDistance,
                    maximumPreferredDistance,
                    distance));
        }

        var excessDistance = distance - maximumPreferredDistance;
        return 0.5f / (1f + excessDistance / Mathf.Max(0.01f, maximumPreferredDistance));
    }

    float DensityWeight(Vector3 origin, Vector3 other)
    {
        var distance = HorizontalDistance(origin, other);
        return distance >= m_DensityRadius
            ? 0f
            : 1f - distance / m_DensityRadius;
    }

    int SelectWeightedTopCandidate()
    {
        var selectionCount = Mathf.Min(m_TopCandidateCount, m_Candidates.Count);
        var totalWeight = 0f;
        for (var i = 0; i < selectionCount; i++)
            totalWeight += m_Candidates[i].Score;

        var value = Random.value * totalWeight;
        for (var i = 0; i < selectionCount; i++)
        {
            value -= m_Candidates[i].Score;
            if (value <= 0f)
                return i;
        }

        return selectionCount - 1;
    }

    static bool TryBuildPath(
        MonsterNavMeshMovement movement,
        Vector3 candidate,
        NavMeshPath path,
        out Vector3 destination)
    {
        destination = default;
        var agent = movement.Agent;
        if (!NavMesh.SamplePosition(
                candidate,
                out var hit,
                agent.height,
                agent.areaMask))
        {
            return false;
        }

        if (HorizontalDistance(movement.AreaCenter, hit.position) > movement.AreaRange)
            return false;

        if (!agent.CalculatePath(hit.position, path)
            || path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        destination = hit.position;
        return true;
    }

    static float HorizontalDistance(Vector3 left, Vector3 right)
    {
        var offset = left - right;
        offset.y = 0f;
        return offset.magnitude;
    }
}
