using Unity.InferenceEngine;
using Unity.Mathematics;
using UnityEngine;

public enum FacePreprocessResizeMode
{
    Letterbox,
    CenterCropShortSide
}

public enum FacePreprocessValueRange
{
    ZeroToOne,
    MinusOneToOne
}

public class FaceImagePreprocessor : MonoBehaviour
{
    [Header("Model Input")]
    public int inputWidth = 128;
    public int inputHeight = 128;
    public FacePreprocessResizeMode resizeMode = FacePreprocessResizeMode.Letterbox;
    public FacePreprocessValueRange outputValueRange = FacePreprocessValueRange.ZeroToOne;

    [Header("Coordinate Space")]
    public bool flipY = true;

    public TensorShape InputShape => new(1, inputHeight, inputWidth, 3);

    public float2x3 WriteToTensor(Texture source, Tensor<float> destination)
    {
        var matrix = BuildTensorSamplingMatrix(source);
        BlazeUtils.SampleImageAffine(source, destination, matrix);
        return matrix;
    }

    public float2x3 BuildTensorToSourceMatrix(Texture source)
    {
        if (resizeMode == FacePreprocessResizeMode.CenterCropShortSide)
            return BuildCenterCropMatrix(source);

        return BuildLetterboxMatrix(source);
    }

    public bool ShouldNormalizeToMinusOneToOne()
    {
        return outputValueRange == FacePreprocessValueRange.MinusOneToOne;
    }

    float2x3 BuildTensorSamplingMatrix(Texture source)
    {
        return resizeMode == FacePreprocessResizeMode.CenterCropShortSide
            ? BuildCenterCropMatrix(source)
            : BuildLetterboxMatrix(source);
    }

    float2x3 BuildCenterCropMatrix(Texture source)
    {
        var cropSize = Mathf.Min(source.width, source.height);
        var offset = 0.5f * new Vector2(source.width - cropSize, source.height - cropSize);
        var scale = new Vector2(cropSize / (float)inputWidth, cropSize / (float)inputHeight);

        if (flipY)
        {
            return BlazeUtils.mul(
                BlazeUtils.TranslationMatrix(new float2(offset.x, offset.y + cropSize)),
                BlazeUtils.ScaleMatrix(new float2(scale.x, -scale.y)));
        }

        return BlazeUtils.mul(
            BlazeUtils.TranslationMatrix(new float2(offset.x, offset.y)),
            BlazeUtils.ScaleMatrix(new float2(scale.x, scale.y)));
    }

    float2x3 BuildLetterboxMatrix(Texture source)
    {
        var squareSize = Mathf.Max(source.width, source.height);
        var offset = 0.5f * new Vector2(source.width - squareSize, source.height - squareSize);
        var scale = new Vector2(squareSize / (float)inputWidth, squareSize / (float)inputHeight);

        if (flipY)
        {
            return BlazeUtils.mul(
                BlazeUtils.TranslationMatrix(new float2(offset.x, offset.y + squareSize)),
                BlazeUtils.ScaleMatrix(new float2(scale.x, -scale.y)));
        }

        return BlazeUtils.mul(
            BlazeUtils.TranslationMatrix(new float2(offset.x, offset.y)),
            BlazeUtils.ScaleMatrix(new float2(scale.x, scale.y)));
    }

}
