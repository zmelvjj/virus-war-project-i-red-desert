using System;
using System.Collections.Generic;
using UnityEngine;

public static class SmartOrthogonalRouter
{
    public const float LeadLength = 28f;
    public const float ObstaclePadding = 14f;

    const float k_BendPenalty = 48f;
    const float k_CrossingPenalty = 80f;
    const float k_PreviewMinimumSegmentLength = 18f;
    const float k_Epsilon = 0.01f;

    enum TravelDirection
    {
        None,
        Left,
        Right,
        Up,
        Down
    }

    readonly struct RouteState : IEquatable<RouteState>
    {
        public readonly int pointIndex;
        public readonly TravelDirection direction;

        public RouteState(int pointIndex, TravelDirection direction)
        {
            this.pointIndex = pointIndex;
            this.direction = direction;
        }

        public bool Equals(RouteState other)
        {
            return pointIndex == other.pointIndex && direction == other.direction;
        }

        public override bool Equals(object obj)
        {
            return obj is RouteState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(pointIndex, (int)direction);
        }
    }

    public static List<Vector2> Route(
        Rect startRect,
        SkillNodeAnchor startAnchor,
        Rect endRect,
        SkillNodeAnchor endAnchor,
        IReadOnlyList<Rect> obstacles,
        IReadOnlyList<SkillConnectionDraftData> existingConnections,
        int requestedBendPointCount = -1)
    {
        var start = GetAnchorPoint(startRect, startAnchor);
        var end = GetAnchorPoint(endRect, endAnchor);
        var startDirection = GetAnchorDirection(startAnchor);
        var endDirection = GetAnchorDirection(endAnchor);
        var startLead = start + startDirection * LeadLength;
        var endLead = end + endDirection * LeadLength;
        var routingObstacles = new List<Rect>(obstacles.Count + 2);
        routingObstacles.AddRange(obstacles);
        routingObstacles.Add(Expand(startRect, ObstaclePadding));
        routingObstacles.Add(Expand(endRect, ObstaclePadding));
        var middle = FindPath(
            startLead,
            endLead,
            routingObstacles,
            existingConnections,
            GetDirection(Vector2.zero, startDirection),
            GetDirection(endDirection, Vector2.zero));
        var path = new List<Vector2> { start, startLead };
        for (var i = 1; i < middle.Count - 1; i++)
            path.Add(middle[i]);
        path.Add(endLead);
        path.Add(end);
        path = Simplify(path);

        if (requestedBendPointCount >= 0)
            path = EnsureBendPointCount(path, requestedBendPointCount, obstacles);
        return path;
    }

    static Rect Expand(Rect rect, float amount)
    {
        return new Rect(
            rect.xMin - amount,
            rect.yMin - amount,
            rect.width + amount * 2f,
            rect.height + amount * 2f);
    }

    public static List<Vector2> Preview(
        Rect startRect,
        SkillNodeAnchor startAnchor,
        Vector2 pointerPosition)
    {
        var start = GetAnchorPoint(startRect, startAnchor);
        var direction = GetAnchorDirection(startAnchor);
        var perpendicular = new Vector2(-direction.y, direction.x);
        var offset = pointerPosition - start;
        var forwardDistance = Vector2.Dot(offset, direction);
        var sideDistance = Vector2.Dot(offset, perpendicular);
        var path = new List<Vector2> { start };

        if (forwardDistance >= LeadLength
            && (Mathf.Abs(sideDistance) < k_Epsilon
                || Mathf.Abs(sideDistance) >= k_PreviewMinimumSegmentLength))
        {
            path.Add(start + direction * forwardDistance);
            path.Add(pointerPosition);
            return Simplify(path);
        }

        var turnDistance = LeadLength;
        if (Mathf.Abs(forwardDistance - turnDistance) < k_PreviewMinimumSegmentLength)
            turnDistance = Mathf.Max(LeadLength, forwardDistance + k_PreviewMinimumSegmentLength);

        var firstSide = sideDistance + k_PreviewMinimumSegmentLength;
        var secondSide = sideDistance - k_PreviewMinimumSegmentLength;
        var routeSide = Mathf.Abs(firstSide) >= k_PreviewMinimumSegmentLength
            && (Mathf.Abs(secondSide) < k_PreviewMinimumSegmentLength
                || Mathf.Abs(firstSide) <= Mathf.Abs(secondSide))
            ? firstSide
            : secondSide;

        path.Add(start + direction * turnDistance);
        path.Add(start + direction * turnDistance + perpendicular * routeSide);
        path.Add(start + direction * forwardDistance + perpendicular * routeSide);
        path.Add(pointerPosition);
        return Simplify(path);
    }

    public static Vector2 GetAnchorPoint(Rect rect, SkillNodeAnchor anchor)
    {
        return anchor switch
        {
            SkillNodeAnchor.Top => new Vector2(rect.center.x, rect.yMin),
            SkillNodeAnchor.Right => new Vector2(rect.xMax, rect.center.y),
            SkillNodeAnchor.Bottom => new Vector2(rect.center.x, rect.yMax),
            _ => new Vector2(rect.xMin, rect.center.y)
        };
    }

    public static Vector2 GetAnchorDirection(SkillNodeAnchor anchor)
    {
        return anchor switch
        {
            SkillNodeAnchor.Top => Vector2.up * -1f,
            SkillNodeAnchor.Right => Vector2.right,
            SkillNodeAnchor.Bottom => Vector2.up,
            _ => Vector2.left
        };
    }

    public static bool PathIntersectsObstacles(
        IReadOnlyList<Vector2> path,
        IReadOnlyList<Rect> obstacles)
    {
        for (var i = 0; i < path.Count - 1; i++)
        {
            if (SegmentBlocked(path[i], path[i + 1], obstacles))
                return true;
        }

        return false;
    }

    public static List<Vector2> Simplify(IReadOnlyList<Vector2> path)
    {
        var simplified = new List<Vector2>();
        foreach (var point in path)
        {
            if (simplified.Count > 0 && Vector2.Distance(simplified[^1], point) < k_Epsilon)
                continue;

            while (simplified.Count >= 2
                && AreCollinear(simplified[^2], simplified[^1], point))
            {
                simplified.RemoveAt(simplified.Count - 1);
            }

            simplified.Add(point);
        }

        return simplified;
    }

    static List<Vector2> EnsureBendPointCount(
        List<Vector2> path,
        int requestedBendPointCount,
        IReadOnlyList<Rect> obstacles)
    {
        var result = new List<Vector2>(path);
        var requested = Mathf.Max(0, requestedBendPointCount);
        if ((requested - (result.Count - 2)) % 2 != 0)
            requested++;

        while (result.Count - 2 < requested)
        {
            var inserted = false;
            for (var i = 1; i < result.Count - 1; i++)
            {
                var previous = result[i - 1];
                var corner = result[i];
                var next = result[i + 1];
                var previousLength = Vector2.Distance(previous, corner);
                var nextLength = Vector2.Distance(corner, next);
                if (previousLength < 24f || nextLength < 24f)
                    continue;

                var previousDirection = (previous - corner).normalized;
                var nextDirection = (next - corner).normalized;
                var offset = Mathf.Min(16f, previousLength / 3f, nextLength / 3f);
                var first = corner + previousDirection * offset;
                var third = corner + nextDirection * offset;
                var second = new Vector2(first.x, third.y);
                if (Mathf.Abs(first.x - corner.x) < k_Epsilon)
                    second = new Vector2(third.x, first.y);

                var candidate = new List<Vector2>(result);
                candidate.RemoveAt(i);
                candidate.InsertRange(i, new[] { first, second, third });
                if (PathIntersectsObstacles(candidate, obstacles))
                    continue;

                result = candidate;
                inserted = true;
                break;
            }

            if (!inserted)
                break;
        }

        return Simplify(result);
    }

    static List<Vector2> FindPath(
        Vector2 start,
        Vector2 end,
        IReadOnlyList<Rect> obstacles,
        IReadOnlyList<SkillConnectionDraftData> existingConnections,
        TravelDirection initialDirection,
        TravelDirection finalDirection)
    {
        var xCoordinates = new List<float> { start.x, end.x };
        var yCoordinates = new List<float> { start.y, end.y };
        foreach (var obstacle in obstacles)
        {
            xCoordinates.Add(obstacle.xMin);
            xCoordinates.Add(obstacle.xMax);
            yCoordinates.Add(obstacle.yMin);
            yCoordinates.Add(obstacle.yMax);
        }

        SortDistinct(xCoordinates);
        SortDistinct(yCoordinates);

        var points = new List<Vector2>();
        foreach (var x in xCoordinates)
        {
            foreach (var y in yCoordinates)
            {
                var point = new Vector2(x, y);
                if (!IsInsideObstacle(point, obstacles) || Approximately(point, start) || Approximately(point, end))
                    points.Add(point);
            }
        }

        var startIndex = FindPoint(points, start);
        var endIndex = FindPoint(points, end);
        var startState = new RouteState(startIndex, initialDirection);
        var open = new List<RouteState> { startState };
        var costs = new Dictionary<RouteState, float> { [startState] = 0f };
        var previous = new Dictionary<RouteState, RouteState>();
        RouteState? completed = null;

        while (open.Count > 0)
        {
            var current = PopLowest(open, costs, points, end);
            if (current.pointIndex == endIndex)
            {
                completed = current;
                break;
            }

            foreach (var neighborIndex in GetVisibleNeighbors(current.pointIndex, points, obstacles))
            {
                var direction = GetDirection(points[current.pointIndex], points[neighborIndex]);
                var next = new RouteState(neighborIndex, direction);
                var distance = Vector2.Distance(points[current.pointIndex], points[neighborIndex]);
                var bendCost = current.direction != TravelDirection.None && current.direction != direction
                    ? k_BendPenalty
                    : 0f;
                if (neighborIndex == endIndex && direction != finalDirection)
                    bendCost += k_BendPenalty;
                var crossingCost = CountCrossings(
                    points[current.pointIndex],
                    points[neighborIndex],
                    existingConnections) * k_CrossingPenalty;
                var newCost = costs[current] + distance + bendCost + crossingCost;

                if (costs.TryGetValue(next, out var oldCost) && oldCost <= newCost)
                    continue;

                costs[next] = newCost;
                previous[next] = current;
                if (!open.Contains(next))
                    open.Add(next);
            }
        }

        if (completed.HasValue)
            return Reconstruct(completed.Value, previous, points);

        var horizontalCorner = new Vector2(end.x, start.y);
        if (!SegmentBlocked(start, horizontalCorner, obstacles)
            && !SegmentBlocked(horizontalCorner, end, obstacles))
        {
            return Simplify(new[] { start, horizontalCorner, end });
        }

        return Simplify(new[] { start, new Vector2(start.x, end.y), end });
    }

    static IEnumerable<int> GetVisibleNeighbors(
        int pointIndex,
        IReadOnlyList<Vector2> points,
        IReadOnlyList<Rect> obstacles)
    {
        var origin = points[pointIndex];
        var bestIndices = new[] { -1, -1, -1, -1 };
        var bestDistances = new[] { float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue };

        for (var i = 0; i < points.Count; i++)
        {
            if (i == pointIndex)
                continue;

            var candidate = points[i];
            int directionIndex;
            float distance;
            if (Mathf.Abs(candidate.y - origin.y) < k_Epsilon)
            {
                directionIndex = candidate.x < origin.x ? 0 : 1;
                distance = Mathf.Abs(candidate.x - origin.x);
            }
            else if (Mathf.Abs(candidate.x - origin.x) < k_Epsilon)
            {
                directionIndex = candidate.y < origin.y ? 2 : 3;
                distance = Mathf.Abs(candidate.y - origin.y);
            }
            else
            {
                continue;
            }

            if (distance >= bestDistances[directionIndex]
                || SegmentBlocked(origin, candidate, obstacles))
            {
                continue;
            }

            bestIndices[directionIndex] = i;
            bestDistances[directionIndex] = distance;
        }

        foreach (var index in bestIndices)
        {
            if (index >= 0)
                yield return index;
        }
    }

    static RouteState PopLowest(
        List<RouteState> open,
        IReadOnlyDictionary<RouteState, float> costs,
        IReadOnlyList<Vector2> points,
        Vector2 end)
    {
        var bestIndex = 0;
        var bestScore = float.MaxValue;
        for (var i = 0; i < open.Count; i++)
        {
            var state = open[i];
            var score = costs[state]
                + Mathf.Abs(points[state.pointIndex].x - end.x)
                + Mathf.Abs(points[state.pointIndex].y - end.y);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestIndex = i;
        }

        var best = open[bestIndex];
        open.RemoveAt(bestIndex);
        return best;
    }

    static List<Vector2> Reconstruct(
        RouteState completed,
        IReadOnlyDictionary<RouteState, RouteState> previous,
        IReadOnlyList<Vector2> points)
    {
        var path = new List<Vector2>();
        var current = completed;
        path.Add(points[current.pointIndex]);
        while (previous.TryGetValue(current, out var parent))
        {
            current = parent;
            path.Add(points[current.pointIndex]);
        }

        path.Reverse();
        return Simplify(path);
    }

    static bool SegmentBlocked(Vector2 start, Vector2 end, IReadOnlyList<Rect> obstacles)
    {
        var horizontal = Mathf.Abs(start.y - end.y) < k_Epsilon;
        foreach (var obstacle in obstacles)
        {
            if (horizontal)
            {
                if (start.y <= obstacle.yMin + k_Epsilon || start.y >= obstacle.yMax - k_Epsilon)
                    continue;

                if (RangesOverlap(start.x, end.x, obstacle.xMin, obstacle.xMax))
                    return true;
            }
            else
            {
                if (start.x <= obstacle.xMin + k_Epsilon || start.x >= obstacle.xMax - k_Epsilon)
                    continue;

                if (RangesOverlap(start.y, end.y, obstacle.yMin, obstacle.yMax))
                    return true;
            }
        }

        return false;
    }

    static bool IsInsideObstacle(Vector2 point, IReadOnlyList<Rect> obstacles)
    {
        foreach (var obstacle in obstacles)
        {
            if (point.x > obstacle.xMin + k_Epsilon
                && point.x < obstacle.xMax - k_Epsilon
                && point.y > obstacle.yMin + k_Epsilon
                && point.y < obstacle.yMax - k_Epsilon)
            {
                return true;
            }
        }

        return false;
    }

    static int CountCrossings(
        Vector2 start,
        Vector2 end,
        IReadOnlyList<SkillConnectionDraftData> connections)
    {
        var count = 0;
        foreach (var connection in connections)
        {
            for (var i = 0; i < connection.pathPoints.Count - 1; i++)
            {
                if (SegmentsCross(start, end, connection.pathPoints[i], connection.pathPoints[i + 1]))
                    count++;
            }
        }

        return count;
    }

    static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var firstHorizontal = Mathf.Abs(a.y - b.y) < k_Epsilon;
        var secondHorizontal = Mathf.Abs(c.y - d.y) < k_Epsilon;
        if (firstHorizontal == secondHorizontal)
            return false;

        var horizontalStart = firstHorizontal ? a : c;
        var horizontalEnd = firstHorizontal ? b : d;
        var verticalStart = firstHorizontal ? c : a;
        var verticalEnd = firstHorizontal ? d : b;
        return Between(verticalStart.x, horizontalStart.x, horizontalEnd.x)
            && Between(horizontalStart.y, verticalStart.y, verticalEnd.y);
    }

    static bool AreCollinear(Vector2 a, Vector2 b, Vector2 c)
    {
        var aligned = Mathf.Abs(a.x - b.x) < k_Epsilon && Mathf.Abs(b.x - c.x) < k_Epsilon
            || Mathf.Abs(a.y - b.y) < k_Epsilon && Mathf.Abs(b.y - c.y) < k_Epsilon;
        return aligned && Vector2.Dot(b - a, c - b) >= 0f;
    }

    static TravelDirection GetDirection(Vector2 a, Vector2 b)
    {
        if (Mathf.Abs(a.y - b.y) < k_Epsilon)
            return b.x < a.x ? TravelDirection.Left : TravelDirection.Right;

        return b.y < a.y ? TravelDirection.Up : TravelDirection.Down;
    }

    static bool RangesOverlap(float a, float b, float min, float max)
    {
        var segmentMin = Mathf.Min(a, b);
        var segmentMax = Mathf.Max(a, b);
        return segmentMax > min + k_Epsilon && segmentMin < max - k_Epsilon;
    }

    static bool Between(float value, float a, float b)
    {
        return value >= Mathf.Min(a, b) - k_Epsilon && value <= Mathf.Max(a, b) + k_Epsilon;
    }

    static bool Approximately(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b) < k_Epsilon;
    }

    static int FindPoint(IReadOnlyList<Vector2> points, Vector2 target)
    {
        for (var i = 0; i < points.Count; i++)
        {
            if (Approximately(points[i], target))
                return i;
        }

        return -1;
    }

    static void SortDistinct(List<float> values)
    {
        values.Sort();
        for (var i = values.Count - 1; i > 0; i--)
        {
            if (Mathf.Abs(values[i] - values[i - 1]) < k_Epsilon)
                values.RemoveAt(i);
        }
    }
}

public static class SkillConnectionRules
{
    public static bool CanCreate(
        IReadOnlyList<SkillConnectionDraftData> connections,
        string parentGuid,
        string childGuid)
    {
        if (parentGuid == childGuid)
            return false;

        foreach (var connection in connections)
        {
            if (connection.parentNodeGuid == parentGuid
                && connection.childNodeGuid == childGuid)
            {
                return false;
            }
        }

        var pending = new Stack<string>();
        var visited = new HashSet<string>();
        pending.Push(childGuid);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current))
                continue;
            if (current == parentGuid)
                return false;

            foreach (var connection in connections)
            {
                if (connection.parentNodeGuid == current)
                    pending.Push(connection.childNodeGuid);
            }
        }

        return true;
    }
}
