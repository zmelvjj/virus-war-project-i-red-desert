using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.InferenceEngine;
using UnityEngine;

public struct FaceDetectionResult
{
    public float score;
    public Vector2 center;
    public Vector2 size;
    public Vector2[] keypoints;
}

public class FaceDetection : MonoBehaviour
{
    public FacePreview[] facePreviews;
    public ImagePreview imagePreview;
    public WebcamProvider webcamProvider;
    public FaceImagePreprocessor imagePreprocessor;
    public Texture2D imageTexture;
    public ModelAsset faceDetector;
    public TextAsset anchorsCSV;

    public float iouThreshold = 0.3f;
    public float scoreThreshold = 0.5f;

    [Header("Debug")]
    public bool logFaceCountEverySecond = true;
    public float faceCountLogIntervalSeconds = 1f;
    public bool useTextureConverterInput = true;
    public bool normalizeInputToMinusOneToOne = false;

    const int k_NumAnchors = 896;
    float[,] m_Anchors;

    const int k_NumKeypoints = 6;
    const int detectorInputSize = 128;

    Worker m_FaceDetectorWorker;
    Tensor<float> m_DetectorInput;
    Awaitable m_DetectAwaitable;
    readonly List<FaceDetectionResult> m_Results = new();
    bool m_IsShuttingDown;
    float m_NextFaceCountLogTime;

    float m_TextureWidth;
    float m_TextureHeight;

    public IReadOnlyList<FaceDetectionResult> Results => m_Results;
    public Texture CurrentTexture { get; private set; }
    public float TextureWidth => m_TextureWidth;
    public float TextureHeight => m_TextureHeight;

    public async void Start()
    {
        if (faceDetector == null || anchorsCSV == null)
        {
            Debug.LogError("FaceDetection requires a BlazeFace model and anchors CSV.");
            return;
        }

        m_Anchors = BlazeUtils.LoadAnchors(anchorsCSV.text, k_NumAnchors);
        if (imagePreprocessor == null)
            imagePreprocessor = GetComponent<FaceImagePreprocessor>();

        var faceDetectorModel = ModelLoader.Load(faceDetector);
        if (faceDetectorModel.inputs.Count > 0)
            Debug.Log($"[BlazeFace] ModelInput={faceDetectorModel.inputs[0].name} {faceDetectorModel.inputs[0].shape}");
        if (faceDetectorModel.outputs.Count >= 2)
            Debug.Log($"[BlazeFace] ModelOutputs=0:{faceDetectorModel.outputs[0].name}, 1:{faceDetectorModel.outputs[1].name}");

        // post process the model to filter scores + nms select the best faces
        var graph = new FunctionalGraph();
        var input = graph.AddInput(faceDetectorModel, 0);
        var normalizeInput = imagePreprocessor != null
            ? imagePreprocessor.ShouldNormalizeToMinusOneToOne()
            : normalizeInputToMinusOneToOne;
        var modelInput = normalizeInput ? 2 * input - 1 : input;
        var outputs = Functional.Forward(faceDetectorModel, modelInput);
        var boxes = outputs[0]; // (1, 896, 16)
        var scores = outputs[1]; // (1, 896, 1)
        var anchorsData = new float[k_NumAnchors * 4];
        Buffer.BlockCopy(m_Anchors, 0, anchorsData, 0, anchorsData.Length * sizeof(float));
        var anchors = Functional.Constant(new TensorShape(k_NumAnchors, 4), anchorsData);
        var idx_scores_boxes = BlazeUtils.NMSFiltering(boxes, scores, anchors, detectorInputSize, iouThreshold, scoreThreshold);
        faceDetectorModel = graph.Compile(idx_scores_boxes.Item1, idx_scores_boxes.Item2, idx_scores_boxes.Item3);

        m_FaceDetectorWorker = new Worker(faceDetectorModel, BackendType.GPUCompute);

        m_DetectorInput = new Tensor<float>(imagePreprocessor != null
            ? imagePreprocessor.InputShape
            : new TensorShape(1, detectorInputSize, detectorInputSize, 3));

        while (!m_IsShuttingDown)
        {
            try
            {
                var sourceTexture = GetSourceTexture();
                if (sourceTexture == null || sourceTexture.width <= 16 || sourceTexture.height <= 16)
                {
                    await Awaitable.NextFrameAsync();
                    continue;
                }

                m_DetectAwaitable = Detect(sourceTexture);
                await m_DetectAwaitable;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        m_FaceDetectorWorker?.Dispose();
        m_DetectorInput?.Dispose();
    }

    Vector3 ImageToWorld(Vector2 position)
    {
        return (position - 0.5f * new Vector2(m_TextureWidth, m_TextureHeight)) / m_TextureHeight;
    }

    async Awaitable Detect(Texture texture)
    {
        CurrentTexture = texture;
        m_TextureWidth = texture.width;
        m_TextureHeight = texture.height;
        if (imagePreview != null)
            imagePreview.SetTexture(texture);

        var size = Mathf.Max(texture.width, texture.height);

        float2x3 M;
        float2x3 resultMatrix;
        if (imagePreprocessor != null)
        {
            M = imagePreprocessor.WriteToTensor(texture, m_DetectorInput);
            resultMatrix = imagePreprocessor.BuildTensorToSourceMatrix(texture);
        }
        else if (useTextureConverterInput)
        {
            TextureConverter.ToTensor(
                texture,
                m_DetectorInput,
                new TextureTransform()
                    .SetTensorLayout(TensorLayout.NHWC)
                    .SetCoordOrigin(CoordOrigin.TopLeft));

            M = BlazeUtils.ScaleMatrix(new Vector2(
                texture.width / (float)detectorInputSize,
                texture.height / (float)detectorInputSize));
            resultMatrix = M;
        }
        else
        {
            // The affine transformation matrix to go from tensor coordinates to image coordinates
            var scale = size / (float)detectorInputSize;
            M = BlazeUtils.mul(BlazeUtils.TranslationMatrix(0.5f * (new Vector2(texture.width, texture.height) + new Vector2(-size, size))), BlazeUtils.ScaleMatrix(new Vector2(scale, -scale)));
            BlazeUtils.SampleImageAffine(texture, m_DetectorInput, M);
            resultMatrix = M;
        }

        m_FaceDetectorWorker.Schedule(m_DetectorInput);

        var outputIndicesAwaitable = (m_FaceDetectorWorker.PeekOutput(0) as Tensor<int>).ReadbackAndCloneAsync();
        var outputScoresAwaitable = (m_FaceDetectorWorker.PeekOutput(1) as Tensor<float>).ReadbackAndCloneAsync();
        var outputBoxesAwaitable = (m_FaceDetectorWorker.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync();

        using var outputIndices = await outputIndicesAwaitable;
        using var outputScores = await outputScoresAwaitable;
        using var outputBoxes = await outputBoxesAwaitable;

        var numFaces = outputIndices.shape.length;
        m_Results.Clear();

        var previewCount = facePreviews == null ? 0 : facePreviews.Length;
        for (var i = 0; i < previewCount; i++)
        {
            if (facePreviews[i] != null)
                facePreviews[i].SetActive(i < numFaces);
        }

        for (var i = 0; i < numFaces; i++)
        {
            var idx = outputIndices[i];

            var anchorPosition = detectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);

            var box_ImageSpace = BlazeUtils.mul(resultMatrix, anchorPosition + new float2(outputBoxes[0, i, 0], outputBoxes[0, i, 1]));
            var boxTopRight_ImageSpace = BlazeUtils.mul(resultMatrix, anchorPosition + new float2(outputBoxes[0, i, 0] + 0.5f * outputBoxes[0, i, 2], outputBoxes[0, i, 1] + 0.5f * outputBoxes[0, i, 3]));

            var boxSize = 2f * (boxTopRight_ImageSpace - box_ImageSpace);
            boxSize = new Vector2(Mathf.Abs(boxSize.x), Mathf.Abs(boxSize.y));
            var keypoints = new Vector2[k_NumKeypoints];

            if (i < previewCount && facePreviews[i] != null)
                facePreviews[i].SetBoundingBox(true, ImageToWorld(box_ImageSpace), boxSize / texture.height);

            for (var j = 0; j < k_NumKeypoints; j++)
            {
                var position_ImageSpace = BlazeUtils.mul(resultMatrix, anchorPosition + new float2(outputBoxes[0, i, 4 + 2 * j + 0], outputBoxes[0, i, 4 + 2 * j + 1]));
                keypoints[j] = position_ImageSpace;

                if (i < previewCount && facePreviews[i] != null)
                    facePreviews[i].SetKeypoint(j, true, ImageToWorld(position_ImageSpace));
            }

            m_Results.Add(new FaceDetectionResult
            {
                score = Sigmoid(outputScores[0, i, 0]),
                center = box_ImageSpace,
                size = boxSize,
                keypoints = keypoints
            });
        }

        LogFaceCountIfDue(numFaces, outputScores);
        await Awaitable.NextFrameAsync();
    }

    void LogFaceCountIfDue(int numFaces, Tensor<float> outputScores)
    {
        if (!logFaceCountEverySecond)
            return;

        var interval = Mathf.Max(0.1f, faceCountLogIntervalSeconds);
        if (Time.unscaledTime < m_NextFaceCountLogTime)
            return;

        m_NextFaceCountLogTime = Time.unscaledTime + interval;
        var bestScore = 0f;
        for (var i = 0; i < numFaces; i++)
            bestScore = Mathf.Max(bestScore, Sigmoid(outputScores[0, i, 0]));

        if (m_Results.Count > 0)
        {
            var face = m_Results[0];
            Debug.Log($"[BlazeFace] FaceCount={numFaces}, BestScore={bestScore:F3}, Center=({face.center.x:F1},{face.center.y:F1}), Size=({face.size.x:F1},{face.size.y:F1})");
            return;
        }

        Debug.Log($"[BlazeFace] FaceCount={numFaces}, BestScore={bestScore:F3}");
    }

    float Sigmoid(float value)
    {
        return 1f / (1f + Mathf.Exp(-value));
    }

    Texture GetSourceTexture()
    {
        if (webcamProvider != null && webcamProvider.Webcam != null && webcamProvider.Webcam.isPlaying)
            return webcamProvider.Webcam;

        return imageTexture;
    }

    void OnDestroy()
    {
        m_IsShuttingDown = true;
        m_DetectAwaitable?.Cancel();
    }
}
