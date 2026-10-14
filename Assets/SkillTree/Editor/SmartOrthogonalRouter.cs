using System;
using System.Collections.Generic;
using UnityEngine;

public static class SmartOrthogonalRouter
{
    public const float LeadLength = 28f;

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

    readonly struct RouteCost
    {
        public readonly int bends;
        public readonly float length;

        public RouteCost(int bends, float length)
        {
            this.bends = bends;
            this.length = length;
        }
    }

    public static List<Vector2> Route(
        Rect startRect,
        SkillNodeAnchor startAnchor,
        Rect endRect,
        SkillNodeAnchor endAnchor,
        int requestedBendPointCount = -1)
    {
        var start = GetAnchorPoint(startRect, startAnchor);
        var end = GetAnchorPoint(endRect, endAnchor);
        var startDirection = GetAnchorDirection(startAnchor);
        var endDirection = GetAnchorDirection(endAnchor);
        var path = FindMinimumPath(
            start,
            end,
            GetDirection(startDirection),
            Opposite(GetDirection(endDirection)),
            true);
        path = Simplify(path);
        CenterParallelEndpointSegments(path);

        if (requestedBendPointCount >= 0)
        {
            var minimum = Mathf.Max(0, path.Count - 2);
            path = EnsureBendPointCount(
                path,
                NormalizeRequestedBendPointCount(minimum, requestedBendPointCount));
        }
        return path;
    }

    public static int GetMinimumBendPointCount(
        Rect startRect,
        SkillNodeAnchor startAnchor,
        Rect endRect,
        SkillNodeAnchor endAnchor)
    {
        return Mathf.Max(0, Route(startRect, startAnchor, endRect, endAnchor).Count - 2);
    }

    public static int NormalizeRequestedBendPointCount(int minimum, int requested)
    {
        var normalized = Mathf.Max(minimum, requested);
        if ((normalized - minimum) % 2 != 0)
            normalized++;
        return normalized;
    }

    public static List<Vector2> Preview(
        Rect startRect,
        SkillNodeAnchor startAnchor,
        Vector2 pointerPosition)
    {
        var start = GetAnchorPoint(startRect, startAnchor);
        var direction = GetAnchorDirection(startAnchor);
        var lead = start + direction * LeadLength;
        var middle = FindMinimumPath(
            lead,
            pointerPosition,
            GetDirection(direction),
            TravelDirection.None);
        var path = new List<Vector2> { start };
        path.AddRange(middle);
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
            SkillNodeAnchor.Top => Vector2.down,
            SkillNodeAnchor.Right => Vector2.right,
            SkillNodeAnchor.Bottom => Vector2.up,
            _ => Vector2.left
        };
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

    static List<Vector2> FindMinimumPath(
        Vector2 start,
        Vector2 end,
        TravelDirection initialDirection,
        TravelDirection finalDirection,
        bool requireInitialDirection = false)
    {
        if (Approximately(start, end))
            return new List<Vector2> { start };

        var xCoordinates = new List<float>();
        var yCoordinates = new List<float>();
        AddRouteCoordinates(xCoordinates, start.x, end.x);
        AddRouteCoordinates(yCoordinates, start.y, end.y);
        SortDistinct(xCoordinates);
        SortDistinct(yCoordinates);

        var points = new List<Vector2>();
        foreach (var x in xCoordinates)
        {
            foreach (var y in yCoordinates)
                points.Add(new Vector2(x, y));
        }

        var startIndex = FindPoint(points, start);
        var endIndex = FindPoint(points, end);
        var startState = new RouteState(startIndex, initialDirection);
        var open = new List<RouteState> { startState };
        var costs = new Dictionary<RouteState, RouteCost>
        {
            [startState] = new RouteCost(0, 0f)
        };
        var previous = new Dictionary<RouteState, RouteState>();
        RouteState? completed = null;

        while (open.Count > 0)
        {
            var current = PopLowest(open, costs);
            if (current.pointIndex == endIndex
                && (finalDirection == TravelDirection.None || current.direction == finalDirection))
            {
                completed = current;
                break;
            }

            foreach (var neighborIndex in GetVisibleNeighbors(current.pointIndex, points))
            {
                var direction = GetDirection(points[current.pointIndex], points[neighborIndex]);
                if (requireInitialDirection
                    && current.pointIndex == startIndex
                    && direction != initialDirection)
                {
                    continue;
                }
                if (AreOpposite(current.direction, direction))
                    continue;

                var next = new RouteState(neighborIndex, direction);
                var currentCost = costs[current];
                var nextCost = new RouteCost(
                    currentCost.bends + (current.direction == direction ? 0 : 1),
                    currentCost.length + Vector2.Distance(
                        points[current.pointIndex],
                        points[neighborIndex]));
                if (costs.TryGetValue(next, out var oldCost) && !IsBetter(nextCost, oldCost))
                    continue;

                costs[next] = nextCost;
                previous[next] = current;
                if (!open.Contains(next))
                    open.Add(next);
            }
        }

        if (completed.HasValue)
            return Reconstruct(completed.Value, previous, points);

        return Simplify(new[] { start, new Vector2(end.x, start.y), end });
    }

    static void AddRouteCoordinates(List<float> coordinates, float start, float end)
    {
        coordinates.Add(start);
        coordinates.Add(end);
        coordinates.Add((start + end) * 0.5f);
        coordinates.Add(start - LeadLength);
        coordinates.Add(start + LeadLength);
        coordinates.Add(end - LeadLength);
        coordinates.Add(end + LeadLength);
    }

    static IEnumerable<int> GetVisibleNeighbors(
        int pointIndex,
        IReadOnlyList<Vector2> points)
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

            if (distance >= bestDistances[directionIndex])
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
        IReadOnlyDictionary<RouteState, RouteCost> costs)
    {
        var bestIndex = 0;
        for (var i = 1; i < open.Count; i++)
        {
            if (IsBetter(costs[open[i]], costs[open[bestIndex]]))
                bestIndex = i;
        }

        var best = open[bestIndex];
        open.RemoveAt(bestIndex);
        return best;
    }

    static bool IsBetter(RouteCost candidate, RouteCost current)
    {
        return candidate.bends < current.bends
            || candidate.bends == current.bends
            && candidate.length < current.length - k_Epsilon;
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

    static List<Vector2> EnsureBendPointCount(
        List<Vector2> path,
        int requestedBendPointCount)
    {
        var result = new List<Vector2>(path);
        var requested = Mathf.Max(0, requestedBendPointCount);

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
                if (previousLength < k_Epsilon || nextLength < k_Epsilon)
                    continue;

                var previousDirection = (previous - corner).normalized;
                var nextDirection = (next - corner).normalized;
                var outerLength = Mathf.Min(previousLength, nextLength) * 0.5f;
                var first = corner + previousDirection * (previousLength - outerLength);
                var third = corner + nextDirection * (nextLength - outerLength);
                var second = new Vector2(first.x, third.y);
                if (Mathf.Abs(first.x - corner.x) < k_Epsilon)
                    second = new Vector2(third.x, first.y);

                result.RemoveAt(i);
                result.InsertRange(i, new[] { first, second, third });
                inserted = true;
                break;
            }

            if (!inserted)
                break;
        }

        return Simplify(result);
    }

    static void CenterParallelEndpointSegments(List<Vector2> path)
    {
        if (path.Count != 4)
            return;

        var firstDirection = (path[1] - path[0]).normalized;
        var lastDirection = (path[^1] - path[^2]).normalized;
        if (Vector2.Dot(firstDirection, lastDirection) < 0.99f)
            return;

        if (Mathf.Abs(firstDirection.x) < k_Epsilon)
        {
            var centerY = (path[0].y + path[^1].y) * 0.5f;
            path[1] = new Vector2(path[0].x, centerY);
            path[2] = new Vector2(path[^1].x, centerY);
        }
        else
        {
            var centerX = (path[0].x + path[^1].x) * 0.5f;
            path[1] = new Vector2(centerX, path[0].y);
            path[2] = new Vector2(centerX, path[^1].y);
        }
    }

    static TravelDirection GetDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x < 0f ? TravelDirection.Left : TravelDirection.Right;

        return direction.y < 0f ? TravelDirection.Up : TravelDirection.Down;
    }

    static TravelDirection GetDirection(Vector2 start, Vector2 end)
    {
        if (Mathf.Abs(start.y - end.y) < k_Epsilon)
            return end.x < start.x ? TravelDirection.Left : TravelDirection.Right;

        return end.y < start.y ? TravelDirection.Up : TravelDirection.Down;
    }

    static TravelDirection Opposite(TravelDirection direction)
    {
        return direction switch
        {
            TravelDirection.Left => TravelDirection.Right,
            TravelDirection.Right => TravelDirection.Left,
            TravelDirection.Up => TravelDirection.Down,
            TravelDirection.Down => TravelDirection.Up,
            _ => TravelDirection.None
        };
    }

    static bool AreOpposite(TravelDirection first, TravelDirection second)
    {
        return Opposite(first) == second;
    }

    static bool AreCollinear(Vector2 a, Vector2 b, Vector2 c)
    {
        return Mathf.Abs(a.x - b.x) < k_Epsilon && Mathf.Abs(b.x - c.x) < k_Epsilon
            || Mathf.Abs(a.y - b.y) < k_Epsilon && Mathf.Abs(b.y - c.y) < k_Epsilon;
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
