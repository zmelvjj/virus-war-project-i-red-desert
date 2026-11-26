using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Smoothly joins the two Vaccine7 barrier shells while rendering with the selected barrier material.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class Vaccine7ProximityFusionVisual : MonoBehaviour
{
    private const int InitialGridResolution = 40;
    private const int MaximumFusionVertices = 1000;
    private const float BoundsChangeEpsilon = 0.005f;
    private const float UnionWidthChangeEpsilon = 0.005f;
    private const float MeshUpdateInterval = 1f / 10f;

    private static readonly Vector3[] CubeCorners =
    {
        new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
        new Vector3(1f, 1f, 0f), new Vector3(0f, 1f, 0f),
        new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 1f),
        new Vector3(1f, 1f, 1f), new Vector3(0f, 1f, 1f)
    };

    private static readonly int[,] Tetrahedra =
    {
        { 0, 5, 1, 6 }, { 0, 1, 2, 6 }, { 0, 2, 3, 6 },
        { 0, 3, 7, 6 }, { 0, 7, 4, 6 }, { 0, 4, 5, 6 }
    };

    private readonly int[] cornerIndices = new int[8];

    [Header("Barrier Renderers")]
    [SerializeField] private Renderer barrierA;
    [SerializeField] private Renderer barrierB;

    [Header("Fusion Material")]
    [Tooltip("Used when no fusion override is assigned. The source barrier material is the final fallback.")]
    [SerializeField] private Material barrierBridgeMaterial;
    [Tooltip("This material is used directly on the fused mesh, preserving its shader and material properties.")]
    [SerializeField] private Material barrierFusionMaterialOverride;

    [Header("Smooth Union (world units)")]
    [Min(0f)] [SerializeField] private float barrierMergeDistance = 0.45f;
    [Min(0f)] [SerializeField] private float barrierFusionRange = 4.5f;

    private readonly List<Vector3> vertices = new List<Vector3>(MaximumFusionVertices);
    private readonly List<Vector3> normals = new List<Vector3>(MaximumFusionVertices);
    private readonly List<Vector4> tangents = new List<Vector4>(MaximumFusionVertices);
    private readonly List<Vector2> uvs = new List<Vector2>(MaximumFusionVertices);
    private readonly List<int> triangles = new List<int>(6000);
    private readonly Dictionary<long, int> vertexIndices = new Dictionary<long, int>(2048);
    private MaterialPropertyBlock propertyBlock;

    private bool barrierAWasEnabled;
    private bool barrierBWasEnabled;
    private bool initialized;
    private bool fusionActive;
    private float unionWidth;
    private float nextMeshUpdateTime;
    private Bounds lastBoundsA;
    private Bounds lastBoundsB;
    private float lastUnionWidth = -1f;
    private int gridResolution = InitialGridResolution;

    private Transform fusionTransform;
    private Mesh fusionMesh;
    private MeshRenderer fusionRenderer;
    private float[] fieldValues;
    private Vector3[] fieldGradients;
    private Vector3 gridOrigin;
    private Vector3 gridStep;
    private int cellsX;
    private int cellsY;
    private int cellsZ;
    private Vector3 ellipsoidCenterA;
    private Vector3 ellipsoidExtentsA;
    private Vector3 ellipsoidCenterB;
    private Vector3 ellipsoidExtentsB;
    private Vector3 uvCenter;

    private void Awake()
    {
        if (barrierA == null || barrierB == null)
        {
            Debug.LogError("Assign both barrier renderers.", this);
            enabled = false;
            return;
        }

        propertyBlock = new MaterialPropertyBlock();
        barrierAWasEnabled = barrierA.enabled;
        barrierBWasEnabled = barrierB.enabled;
        CreateFusionRenderer();
        initialized = true;
        UpdateFusionState();
    }

    private void LateUpdate()
    {
        if (initialized)
            UpdateFusionState();
    }

    private void OnDisable()
    {
        if (initialized)
            SetFusionVisible(false);
    }

    private void OnEnable()
    {
        if (initialized)
            SetFusionVisible(true);
    }

    private void OnDestroy()
    {
        if (!initialized)
            return;

        SetFusionVisible(false);
        if (fusionMesh != null)
        {
            if (Application.isPlaying)
                Destroy(fusionMesh);
            else
                DestroyImmediate(fusionMesh);
        }

        if (fusionTransform != null)
        {
            if (Application.isPlaying)
                Destroy(fusionTransform.gameObject);
            else
                DestroyImmediate(fusionTransform.gameObject);
        }
    }

    private void CreateFusionRenderer()
    {
        GameObject fusionObject = new GameObject("Vaccine7 Barrier Smooth Union");
        fusionObject.layer = barrierA.gameObject.layer;
        fusionTransform = fusionObject.transform;

        fusionMesh = new Mesh
        {
            name = "Vaccine7 Barrier Smooth Union Mesh",
            indexFormat = IndexFormat.UInt16
        };

        MeshFilter meshFilter = fusionObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = fusionMesh;

        fusionRenderer = fusionObject.AddComponent<MeshRenderer>();
        fusionRenderer.sharedMaterial = GetFusionMaterial();
        fusionRenderer.shadowCastingMode = barrierA.shadowCastingMode;
        fusionRenderer.receiveShadows = barrierA.receiveShadows;
        fusionRenderer.lightProbeUsage = barrierA.lightProbeUsage;
        fusionRenderer.reflectionProbeUsage = barrierA.reflectionProbeUsage;
        fusionRenderer.enabled = false;
    }

    private Material GetFusionMaterial()
    {
        if (barrierFusionMaterialOverride != null)
            return barrierFusionMaterialOverride;
        if (barrierBridgeMaterial != null)
            return barrierBridgeMaterial;
        return barrierA != null ? barrierA.sharedMaterial : null;
    }

    private void UpdateFusionState()
    {
        Bounds boundsA = barrierA.bounds;
        Bounds boundsB = barrierB.bounds;
        float surfaceGap = GetSurfaceGap(boundsA, boundsB);
        unionWidth = GetUnionWidth(boundsA, boundsB, barrierMergeDistance, barrierFusionRange);
        fusionActive = unionWidth > surfaceGap * 2f && unionWidth > 0.0001f;

        if (fusionActive)
        {
            if (fusionRenderer.sharedMaterial != GetFusionMaterial())
                fusionRenderer.sharedMaterial = GetFusionMaterial();

            barrierA.GetPropertyBlock(propertyBlock);
            fusionRenderer.SetPropertyBlock(propertyBlock);
            RebuildMeshIfNeeded(boundsA, boundsB);
        }

        SetFusionVisible(isActiveAndEnabled);
    }

    private void RebuildMeshIfNeeded(Bounds boundsA, Bounds boundsB)
    {
        bool geometryChanged = Mathf.Abs(lastUnionWidth - unionWidth) >= UnionWidthChangeEpsilon ||
                               !BoundsApproximatelyEqual(lastBoundsA, boundsA) ||
                               !BoundsApproximatelyEqual(lastBoundsB, boundsB);
        if (!geometryChanged || Time.unscaledTime < nextMeshUpdateTime)
            return;

        lastBoundsA = boundsA;
        lastBoundsB = boundsB;
        lastUnionWidth = unionWidth;
        ellipsoidCenterA = boundsA.center;
        ellipsoidExtentsA = boundsA.extents;
        ellipsoidCenterB = boundsB.center;
        ellipsoidExtentsB = boundsB.extents;
        uvCenter = (ellipsoidCenterA + ellipsoidCenterB) * 0.5f;

        int resolution = gridResolution;
        do
        {
            BuildFusionMesh(boundsA, boundsB, resolution);
            if (vertices.Count <= MaximumFusionVertices)
            {
                gridResolution = resolution;
                break;
            }

            if (resolution <= 2)
                break;
            resolution = Mathf.Max(2, resolution * 3 / 4);
        }
        while (true);

        fusionMesh.Clear();
        fusionMesh.SetVertices(vertices);
        fusionMesh.SetNormals(normals);
        fusionMesh.SetTangents(tangents);
        fusionMesh.SetUVs(0, uvs);
        fusionMesh.SetTriangles(triangles, 0, true);
        nextMeshUpdateTime = Time.unscaledTime + MeshUpdateInterval;
    }

    private void BuildFusionMesh(Bounds boundsA, Bounds boundsB, int resolution)
    {
        Bounds domain = boundsA;
        domain.Encapsulate(boundsB);
        float largestDimension = Mathf.Max(domain.size.x, Mathf.Max(domain.size.y, domain.size.z));
        float approximateStep = largestDimension / resolution;
        domain.Expand(unionWidth * 2f + approximateStep * 2f);

        largestDimension = Mathf.Max(domain.size.x, Mathf.Max(domain.size.y, domain.size.z));
        cellsX = Mathf.Max(2, Mathf.CeilToInt(domain.size.x / largestDimension * resolution));
        cellsY = Mathf.Max(2, Mathf.CeilToInt(domain.size.y / largestDimension * resolution));
        cellsZ = Mathf.Max(2, Mathf.CeilToInt(domain.size.z / largestDimension * resolution));
        gridStep = new Vector3(domain.size.x / cellsX, domain.size.y / cellsY, domain.size.z / cellsZ);
        gridOrigin = domain.min;

        int nodesX = cellsX + 1;
        int nodesY = cellsY + 1;
        int nodesZ = cellsZ + 1;
        int nodeCount = nodesX * nodesY * nodesZ;
        if (fieldValues == null || fieldValues.Length != nodeCount)
        {
            fieldValues = new float[nodeCount];
            fieldGradients = new Vector3[nodeCount];
        }

        SampleField(nodesX, nodesY, nodesZ);
        Polygonize(nodesX, nodesY);
    }

    private void SampleField(int nodesX, int nodesY, int nodesZ)
    {
        for (int z = 0; z < nodesZ; z++)
        {
            for (int y = 0; y < nodesY; y++)
            {
                for (int x = 0; x < nodesX; x++)
                {
                    int index = GetNodeIndex(x, y, z, nodesX, nodesY);
                    Vector3 point = GetGridPoint(x, y, z);
                    fieldValues[index] = EvaluateField(point, out fieldGradients[index]);
                }
            }
        }
    }

    private void Polygonize(int nodesX, int nodesY)
    {
        vertices.Clear();
        normals.Clear();
        tangents.Clear();
        uvs.Clear();
        triangles.Clear();
        vertexIndices.Clear();

        for (int z = 0; z < cellsZ; z++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                for (int x = 0; x < cellsX; x++)
                {
                    for (int corner = 0; corner < 8; corner++)
                    {
                        int gx = x + (int)CubeCorners[corner].x;
                        int gy = y + (int)CubeCorners[corner].y;
                        int gz = z + (int)CubeCorners[corner].z;
                        cornerIndices[corner] = GetNodeIndex(gx, gy, gz, nodesX, nodesY);
                    }

                    for (int tetra = 0; tetra < 6; tetra++)
                    {
                        PolygonizeTetra(
                            cornerIndices[Tetrahedra[tetra, 0]],
                            cornerIndices[Tetrahedra[tetra, 1]],
                            cornerIndices[Tetrahedra[tetra, 2]],
                            cornerIndices[Tetrahedra[tetra, 3]]);
                    }
                }
            }
        }
    }

    private void PolygonizeTetra(int a, int b, int c, int d)
    {
        int insideCount = 0;
        int outsideCount = 0;
        int i0 = -1, i1 = -1, i2 = -1;
        int o0 = -1, o1 = -1, o2 = -1;
        AddClassifiedNode(a, ref insideCount, ref outsideCount, ref i0, ref i1, ref i2, ref o0, ref o1, ref o2);
        AddClassifiedNode(b, ref insideCount, ref outsideCount, ref i0, ref i1, ref i2, ref o0, ref o1, ref o2);
        AddClassifiedNode(c, ref insideCount, ref outsideCount, ref i0, ref i1, ref i2, ref o0, ref o1, ref o2);
        AddClassifiedNode(d, ref insideCount, ref outsideCount, ref i0, ref i1, ref i2, ref o0, ref o1, ref o2);

        if (insideCount == 0 || insideCount == 4)
            return;

        if (insideCount == 1)
        {
            SurfacePoint p0 = Intersect(i0, o0);
            SurfacePoint p1 = Intersect(i0, o1);
            SurfacePoint p2 = Intersect(i0, o2);
            AddTriangle(p0, p1, p2);
        }
        else if (insideCount == 3)
        {
            SurfacePoint p0 = Intersect(i0, o0);
            SurfacePoint p1 = Intersect(i1, o0);
            SurfacePoint p2 = Intersect(i2, o0);
            AddTriangle(p0, p1, p2);
        }
        else
        {
            SurfacePoint p0 = Intersect(i0, o0);
            SurfacePoint p1 = Intersect(i0, o1);
            SurfacePoint p2 = Intersect(i1, o0);
            SurfacePoint p3 = Intersect(i1, o1);
            AddTriangle(p0, p1, p3);
            AddTriangle(p0, p3, p2);
        }
    }

    private void AddClassifiedNode(int index, ref int insideCount, ref int outsideCount,
        ref int i0, ref int i1, ref int i2, ref int o0, ref int o1, ref int o2)
    {
        if (fieldValues[index] < 0f)
        {
            if (insideCount == 0) i0 = index;
            else if (insideCount == 1) i1 = index;
            else i2 = index;
            insideCount++;
        }
        else
        {
            if (outsideCount == 0) o0 = index;
            else if (outsideCount == 1) o1 = index;
            else o2 = index;
            outsideCount++;
        }
    }

    private SurfacePoint Intersect(int indexA, int indexB)
    {
        float valueA = fieldValues[indexA];
        float valueB = fieldValues[indexB];
        float denominator = valueA - valueB;
        float amount = Mathf.Abs(denominator) < 0.000001f ? 0.5f : valueA / denominator;
        Vector3 positionA = GetGridPointFromIndex(indexA);
        Vector3 positionB = GetGridPointFromIndex(indexB);
        return new SurfacePoint(
            Vector3.LerpUnclamped(positionA, positionB, amount),
            Vector3.Slerp(fieldGradients[indexA], fieldGradients[indexB], amount).normalized,
            GetEdgeKey(indexA, indexB));
    }

    private void AddTriangle(SurfacePoint a, SurfacePoint b, SurfacePoint c)
    {
        Vector3 faceNormal = Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
        Vector3 averageNormal = a.Normal + b.Normal + c.Normal;
        if (Vector3.Dot(faceNormal, averageNormal) < 0f)
        {
            SurfacePoint swap = b;
            b = c;
            c = swap;
        }

        int indexA = AddVertex(a);
        int indexB = AddVertex(b);
        int indexC = AddVertex(c);
        if (indexA == indexB || indexB == indexC || indexC == indexA)
            return;

        triangles.Add(indexA);
        triangles.Add(indexB);
        triangles.Add(indexC);
    }

    private int AddVertex(SurfacePoint point)
    {
        if (vertexIndices.TryGetValue(point.EdgeKey, out int existingIndex))
            return existingIndex;

        int index = vertices.Count;
        vertexIndices.Add(point.EdgeKey, index);
        vertices.Add(point.Position);
        normals.Add(point.Normal);

        Vector3 direction = (point.Position - uvCenter).normalized;
        float longitude = Mathf.Atan2(direction.z, direction.x);
        float u = longitude / (Mathf.PI * 2f) + 0.5f;
        float v = 0.5f - Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) / Mathf.PI;
        uvs.Add(new Vector2(u, v));
        Vector3 tangent = new Vector3(-Mathf.Sin(longitude), 0f, Mathf.Cos(longitude));
        tangent = (tangent - point.Normal * Vector3.Dot(tangent, point.Normal)).normalized;
        tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, -1f));
        return index;
    }

    private static long GetEdgeKey(int indexA, int indexB)
    {
        int minimum = Mathf.Min(indexA, indexB);
        int maximum = Mathf.Max(indexA, indexB);
        return ((long)minimum << 32) | (uint)maximum;
    }

    private float EvaluateField(Vector3 point, out Vector3 gradient)
    {
        float distanceA = EllipsoidDistance(point, ellipsoidCenterA, ellipsoidExtentsA, out Vector3 gradientA);
        float distanceB = EllipsoidDistance(point, ellipsoidCenterB, ellipsoidExtentsB, out Vector3 gradientB);
        if (unionWidth <= 0f)
        {
            gradient = distanceA <= distanceB ? gradientA : gradientB;
            return Mathf.Min(distanceA, distanceB);
        }

        float blend = Mathf.Max(unionWidth - Mathf.Abs(distanceA - distanceB), 0f) / unionWidth;
        float gradientBlend = blend * 0.5f;
        gradient = distanceA <= distanceB
            ? Vector3.Lerp(gradientA, gradientB, gradientBlend).normalized
            : Vector3.Lerp(gradientB, gradientA, gradientBlend).normalized;
        return Mathf.Min(distanceA, distanceB) - blend * blend * unionWidth * 0.25f;
    }

    private static float EllipsoidDistance(Vector3 point, Vector3 center, Vector3 extents, out Vector3 gradient)
    {
        Vector3 safeExtents = new Vector3(
            Mathf.Max(extents.x, 0.0001f),
            Mathf.Max(extents.y, 0.0001f),
            Mathf.Max(extents.z, 0.0001f));
        Vector3 local = point - center;
        Vector3 normalized = new Vector3(
            local.x / safeExtents.x,
            local.y / safeExtents.y,
            local.z / safeExtents.z);
        Vector3 scaled = new Vector3(
            local.x / (safeExtents.x * safeExtents.x),
            local.y / (safeExtents.y * safeExtents.y),
            local.z / (safeExtents.z * safeExtents.z));
        float normalizedLength = normalized.magnitude;
        float scaledLength = Mathf.Max(scaled.magnitude, 0.0001f);
        gradient = scaled.normalized;
        return normalizedLength * (normalizedLength - 1f) / scaledLength;
    }

    private Vector3 GetGridPoint(int x, int y, int z)
    {
        return gridOrigin + Vector3.Scale(new Vector3(x, y, z), gridStep);
    }

    private Vector3 GetGridPointFromIndex(int index)
    {
        int nodesX = cellsX + 1;
        int nodesY = cellsY + 1;
        int z = index / (nodesX * nodesY);
        int remaining = index - z * nodesX * nodesY;
        int y = remaining / nodesX;
        int x = remaining - y * nodesX;
        return GetGridPoint(x, y, z);
    }

    private static int GetNodeIndex(int x, int y, int z, int nodesX, int nodesY)
    {
        return x + nodesX * (y + nodesY * z);
    }

    private static float GetUnionWidth(Bounds boundsA, Bounds boundsB, float maximumWidth, float fusionRange)
    {
        if (maximumWidth <= 0f)
            return 0f;

        float surfaceGap = GetSurfaceGap(boundsA, boundsB);
        float blend = fusionRange > 0f
            ? 1f - Mathf.SmoothStep(0f, fusionRange, surfaceGap)
            : (surfaceGap <= 0f ? 1f : 0f);
        return maximumWidth * blend;
    }

    private static float GetSurfaceGap(Bounds boundsA, Bounds boundsB)
    {
        Vector3 axisGap = new Vector3(
            Mathf.Max(0f, Mathf.Abs(boundsA.center.x - boundsB.center.x) - boundsA.extents.x - boundsB.extents.x),
            Mathf.Max(0f, Mathf.Abs(boundsA.center.y - boundsB.center.y) - boundsA.extents.y - boundsB.extents.y),
            Mathf.Max(0f, Mathf.Abs(boundsA.center.z - boundsB.center.z) - boundsA.extents.z - boundsB.extents.z));
        return axisGap.magnitude;
    }

    private static bool BoundsApproximatelyEqual(Bounds a, Bounds b)
    {
        return (a.center - b.center).sqrMagnitude < BoundsChangeEpsilon * BoundsChangeEpsilon &&
               (a.extents - b.extents).sqrMagnitude < BoundsChangeEpsilon * BoundsChangeEpsilon;
    }

    private void SetFusionVisible(bool visible)
    {
        bool useFusedMesh = visible && fusionActive;
        if (fusionRenderer != null)
            fusionRenderer.enabled = useFusedMesh;

        if (barrierA != null)
            barrierA.enabled = !useFusedMesh && barrierAWasEnabled;
        if (barrierB != null)
            barrierB.enabled = !useFusedMesh && barrierBWasEnabled;
    }

    private readonly struct SurfacePoint
    {
        public readonly Vector3 Position;
        public readonly Vector3 Normal;
        public readonly long EdgeKey;

        public SurfacePoint(Vector3 position, Vector3 normal, long edgeKey)
        {
            Position = position;
            Normal = normal;
            EdgeKey = edgeKey;
        }
    }
}
