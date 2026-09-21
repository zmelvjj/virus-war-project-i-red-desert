using System;
using System.Collections.Generic;
using Unity.InferenceEngine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class MouthOpenTracker : MonoBehaviour
{
    struct FaceMeshOutputTensors
    {
        public Tensor<float> landmarks;
        public Tensor<float> presence;
    }

    [Header("Pipeline")]
    public FaceDetection faceDetection;
    public ModelAsset faceLandmarkModel;
    public BackendType backendType = BackendType.GPUCompute;

    [Header("Face ROI")]
    public float roiScale = 1.55f;
    public bool flipRoiY = true;
    public bool clampRoiToTextureBounds = true;
    public float minFacePresence = 0.5f;

    [Header("Model Input")]
    public bool normalizeInputToMinusOneToOne = false;

    [Header("Mouth Mapping")]
    public float closedRatio = 0.03f;
    public float openRatio = 0.35f;
    public float smoothingSpeed = 12f;

    [Header("Output")]
    public Animator animator;
    public string animatorParameter = "MouthOpen";
    public UnityEvent<float> mouthOpenChanged;

    [Header("Debug")]
    public bool logMouthOpenEverySecond = true;
    public bool logFaceMeshDetails = true;
    public float logIntervalSeconds = 1f;

    const int k_FaceMeshInputSize = 256;
    const int k_MaxLandmarks = 478;
    const int k_RightEye = 0;
    const int k_LeftEye = 1;

    static readonly int[] s_UpperLip = { 13, 82, 312 };
    static readonly int[] s_LowerLip = { 14, 87, 317 };
    static readonly int[] s_MouthCorners = { 61, 291 };

    Worker m_FaceMeshWorker;
    Tensor<float> m_FaceMeshInput;
    Awaitable m_TrackAwaitable;
    Model m_Model;
    Vector2[] m_Landmarks = new Vector2[k_MaxLandmarks];
    bool m_HasFilteredValue;
    bool m_IsShuttingDown;
    float m_NextLogTime;
    float m_NextDetailLogTime;
    Vector2 m_LastRoiCenter;
    float m_LastRoiSize;
    float m_LastPresenceRaw;
    int m_LastLandmarkLength;
    int m_LastPresenceLength;

    public float MouthOpen { get; private set; }
    public float RawMouthRatio { get; private set; }
    public bool HasFace { get; private set; }
    public float FacePresence { get; private set; }

    async void Start()
    {
        if (faceDetection == null)
            faceDetection = FindFirstObjectByType<FaceDetection>();

        if (faceDetection == null || faceLandmarkModel == null)
        {
            Debug.LogError("MouthOpenTracker requires FaceDetection and face_landmarks_detector.tflite ModelAsset references.");
            enabled = false;
            return;
        }

        var loadedModel = ModelLoader.Load(faceLandmarkModel);
        if (normalizeInputToMinusOneToOne)
        {
            var graph = new FunctionalGraph();
            var input = graph.AddInput(loadedModel, 0);
            var outputs = Functional.Forward(loadedModel, 2 * input - 1);
            m_Model = graph.Compile(outputs);
        }
        else
        {
            m_Model = loadedModel;
        }

        if (m_Model.inputs.Count > 0)
            Debug.Log($"[MouthOpenTracker] ModelInput={m_Model.inputs[0].name} {m_Model.inputs[0].shape}");
        for (var i = 0; i < m_Model.outputs.Count; i++)
            Debug.Log($"[MouthOpenTracker] ModelOutput{i}={m_Model.outputs[i].name}");

        m_FaceMeshWorker = new Worker(m_Model, backendType);
        m_FaceMeshInput = new Tensor<float>(new TensorShape(1, k_FaceMeshInputSize, k_FaceMeshInputSize, 3));

        while (!m_IsShuttingDown)
        {
            try
            {
                m_TrackAwaitable = Track();
                await m_TrackAwaitable;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    async Awaitable Track()
    {
        var texture = faceDetection.CurrentTexture;
        var faces = faceDetection.Results;
        if (texture == null || faces.Count == 0)
        {
            SetMouthOpen(0f, false, 0f);
            await Awaitable.NextFrameAsync();
            return;
        }

        var face = SelectPrimaryFace(faces);
        var roiMatrix = BuildFaceRoiMatrix(face, texture);
        BlazeUtils.SampleImageAffine(texture, m_FaceMeshInput, roiMatrix);

        m_FaceMeshInput.ReadbackRequest();
        while (!m_FaceMeshInput.IsReadbackRequestDone())
        {
            await Awaitable.NextFrameAsync();
            if (m_IsShuttingDown)
                return;
        }

        using var cpuInput = m_FaceMeshInput.ReadbackAndClone();
        m_FaceMeshWorker.Schedule(cpuInput);

        var outputs = await ReadFaceMeshOutputs();
        if (m_IsShuttingDown)
        {
            outputs.landmarks?.Dispose();
            outputs.presence?.Dispose();
            return;
        }

        using var landmarks = outputs.landmarks;
        using var presence = outputs.presence;

        FacePresence = ReadFacePresence(presence);
        var hasLandmarks = landmarks != null && TryReadLandmarks(landmarks, m_Landmarks);
        RawMouthRatio = hasLandmarks ? CalculateMouthRatio(m_Landmarks) : 0f;

        if (FacePresence < minFacePresence || !hasLandmarks)
        {
            LogFaceMeshDetailsIfDue(face);
            SetMouthOpen(0f, false, FacePresence);
            await Awaitable.NextFrameAsync();
            return;
        }

        var normalized = Mathf.InverseLerp(closedRatio, openRatio, RawMouthRatio);
        LogFaceMeshDetailsIfDue(face);
        SetMouthOpen(normalized, true, FacePresence);
        await Awaitable.NextFrameAsync();
    }

    FaceDetectionResult SelectPrimaryFace(IReadOnlyList<FaceDetectionResult> faces)
    {
        var best = faces[0];

        for (var i = 1; i < faces.Count; i++)
        {
            if (faces[i].score <= best.score)
                continue;

            best = faces[i];
        }

        return best;
    }

    float2x3 BuildFaceRoiMatrix(FaceDetectionResult face, Texture texture)
    {
        var roiSize = Mathf.Max(Mathf.Abs(face.size.x), Mathf.Abs(face.size.y)) * roiScale;
        roiSize = Mathf.Max(roiSize, 1f);
        m_LastRoiSize = roiSize;
        var roiCenter = face.center;

        if (clampRoiToTextureBounds && texture != null)
        {
            var halfRoi = 0.5f * roiSize;
            if (roiSize < texture.width)
                roiCenter.x = Mathf.Clamp(roiCenter.x, halfRoi, texture.width - halfRoi);
            else
                roiCenter.x = 0.5f * texture.width;

            if (roiSize < texture.height)
                roiCenter.y = Mathf.Clamp(roiCenter.y, halfRoi, texture.height - halfRoi);
            else
                roiCenter.y = 0.5f * texture.height;
        }

        m_LastRoiCenter = roiCenter;

        var right = Vector2.right;
        if (face.keypoints != null && face.keypoints.Length > k_LeftEye)
        {
            var eyeAxis = face.keypoints[k_LeftEye] - face.keypoints[k_RightEye];
            if (eyeAxis.sqrMagnitude > 0.0001f)
                right = eyeAxis.normalized;
        }

        var up = new Vector2(-right.y, right.x);
        var scale = roiSize / k_FaceMeshInputSize;
        var xColumn = right * scale;
        var yColumn = (flipRoiY ? -up : up) * scale;
        var half = 0.5f * k_FaceMeshInputSize;
        var translation = roiCenter - xColumn * half - yColumn * half;

        return new float2x3(
            xColumn.x, yColumn.x, translation.x,
            xColumn.y, yColumn.y, translation.y);
    }

    async Awaitable<FaceMeshOutputTensors> ReadFaceMeshOutputs()
    {
        var outputTensors = new Tensor<float>[m_Model.outputs.Count];
        for (var i = 0; i < outputTensors.Length; i++)
        {
            outputTensors[i] = m_FaceMeshWorker.PeekOutput(m_Model.outputs[i].name) as Tensor<float>;
            outputTensors[i]?.ReadbackRequest();
        }

        var isReady = false;
        while (!isReady)
        {
            isReady = true;
            for (var i = 0; i < outputTensors.Length; i++)
            {
                if (outputTensors[i] != null && !outputTensors[i].IsReadbackRequestDone())
                {
                    isReady = false;
                    break;
                }
            }

            if (!isReady)
            {
                await Awaitable.NextFrameAsync();
                if (m_IsShuttingDown)
                    return default;
            }
        }

        Tensor<float> landmarks = null;
        Tensor<float> presence = null;
        var landmarkLength = -1;
        var presenceLength = int.MaxValue;

        try
        {
            for (var i = 0; i < outputTensors.Length; i++)
            {
                if (outputTensors[i] == null)
                    continue;

                var cpuOutput = outputTensors[i].ReadbackAndClone();
                var length = cpuOutput.shape.length;
                if (length > landmarkLength)
                {
                    if (landmarks != null)
                    {
                        if (landmarks.shape.length < presenceLength)
                        {
                            presence?.Dispose();
                            presence = landmarks;
                            presenceLength = landmarks.shape.length;
                        }
                        else
                        {
                            landmarks.Dispose();
                        }
                    }

                    landmarks = cpuOutput;
                    landmarkLength = length;
                    m_LastLandmarkLength = length;
                }
                else if (length < presenceLength)
                {
                    presence?.Dispose();
                    presence = cpuOutput;
                    presenceLength = length;
                    m_LastPresenceLength = length;
                }
                else
                {
                    cpuOutput.Dispose();
                }
            }

            return new FaceMeshOutputTensors
            {
                landmarks = landmarks,
                presence = presence
            };
        }
        catch
        {
            landmarks?.Dispose();
            presence?.Dispose();
            throw;
        }
    }

    float ReadFacePresence(Tensor<float> presence)
    {
        if (presence == null || presence.shape.length == 0)
            return 1f;

        var value = presence[0];
        m_LastPresenceRaw = value;
        if (value < 0f || value > 1f)
            value = 1f / (1f + Mathf.Exp(-value));

        return Mathf.Clamp01(value);
    }

    bool TryReadLandmarks(Tensor<float> landmarkTensor, Vector2[] landmarks)
    {
        var count = Mathf.Min(landmarks.Length, landmarkTensor.shape.length / 3);
        if (count <= s_MouthCorners[1])
            return false;

        for (var i = 0; i < count; i++)
        {
            landmarks[i] = new Vector2(
                landmarkTensor[i * 3 + 0],
                landmarkTensor[i * 3 + 1]);
        }

        return true;
    }

    float CalculateMouthRatio(Vector2[] landmarks)
    {
        var width = Vector2.Distance(landmarks[s_MouthCorners[0]], landmarks[s_MouthCorners[1]]);
        if (width <= 0.0001f)
            return 0f;

        var gap = 0f;
        for (var i = 0; i < s_UpperLip.Length; i++)
            gap += Vector2.Distance(landmarks[s_UpperLip[i]], landmarks[s_LowerLip[i]]);

        gap /= s_UpperLip.Length;
        return gap / width;
    }

    void LogFaceMeshDetailsIfDue(FaceDetectionResult face)
    {
        if (!logFaceMeshDetails)
            return;

        var interval = Mathf.Max(0.1f, logIntervalSeconds);
        if (Time.unscaledTime < m_NextDetailLogTime)
            return;

        m_NextDetailLogTime = Time.unscaledTime + interval;
        Debug.Log(
            $"[MouthOpenTracker] FaceScore={face.score:F3}, FaceCenter=({face.center.x:F1},{face.center.y:F1}), FaceSize=({face.size.x:F1},{face.size.y:F1}), RoiCenter=({m_LastRoiCenter.x:F1},{m_LastRoiCenter.y:F1}), RoiSize={m_LastRoiSize:F1}, FlipRoiY={flipRoiY}, PresenceRaw={m_LastPresenceRaw:F3}, RawMouthRatio={RawMouthRatio:F3}, LandmarkLen={m_LastLandmarkLength}, PresenceLen={m_LastPresenceLength}");
    }

    void SetMouthOpen(float target, bool hasFace, float facePresence)
    {
        target = Mathf.Clamp01(target);
        HasFace = hasFace;
        FacePresence = facePresence;

        if (!hasFace)
        {
            m_HasFilteredValue = false;
            RawMouthRatio = 0f;
            MouthOpen = 0f;
        }
        else if (!m_HasFilteredValue)
        {
            m_HasFilteredValue = true;
            MouthOpen = target;
        }
        else
        {
            var alpha = 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
            MouthOpen = Mathf.Lerp(MouthOpen, target, alpha);
        }

        if (animator != null && !string.IsNullOrEmpty(animatorParameter))
            animator.SetFloat(animatorParameter, MouthOpen);

        mouthOpenChanged?.Invoke(MouthOpen);
        LogMouthOpenIfDue();
    }

    void LogMouthOpenIfDue()
    {
        if (!logMouthOpenEverySecond)
            return;

        var interval = Mathf.Max(0.1f, logIntervalSeconds);
        if (Time.unscaledTime < m_NextLogTime)
            return;

        m_NextLogTime = Time.unscaledTime + interval;
        Debug.Log($"[MouthOpenTracker] HasFace={HasFace}, FacePresence={FacePresence:F3}, RawMouthRatio={RawMouthRatio:F3}, MouthOpen={MouthOpen:F3}");
    }

    void OnDestroy()
    {
        m_IsShuttingDown = true;
        m_TrackAwaitable?.Cancel();
        m_FaceMeshInput?.Dispose();
        m_FaceMeshWorker?.Dispose();
    }
}
