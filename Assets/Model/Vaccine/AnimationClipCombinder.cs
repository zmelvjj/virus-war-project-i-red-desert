using UnityEngine;
using UnityEditor;
using System.Linq;
 
public static class AnimationClipCombiner
{
    [MenuItem("Tools/Animation/Combine Selected Clips")]
    private static void CombineSelectedClips()
    {
        // Project 창에서 선택한 AnimationClip 수집
        AnimationClip[] clips = Selection.objects
            .OfType<AnimationClip>()
            .ToArray();

        if (clips.Length < 2)
        {
            Debug.LogWarning("두 개 이상의 AnimationClip을 선택해 줘.");
            return;
        }

        AnimationClip combined = new AnimationClip
        {
            name = "Death_Combined",
            frameRate = clips.Max(c => c.frameRate)
        };

        foreach (AnimationClip clip in clips)
        {
            // 위치, 회전, 스케일 등의 애니메이션 커브
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(
                    clip, binding);

                if (curve != null)
                {
                    AnimationUtility.SetEditorCurve(
                        combined, binding, curve);
                }
            }

            // Sprite 등 오브젝트 참조 애니메이션
            foreach (var binding in
                     AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var curve = AnimationUtility.GetObjectReferenceCurve(
                    clip, binding);

                AnimationUtility.SetObjectReferenceCurve(
                    combined, binding, curve);
            }
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Combined Animation",
            "Death_Combined",
            "anim",
            "합쳐진 애니메이션을 저장할 위치를 선택해 줘.");

        if (string.IsNullOrEmpty(path))
        {
            Object.DestroyImmediate(combined);
            return;
        }

        AssetDatabase.CreateAsset(combined, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = combined;

        Debug.Log($"애니메이션 합치기 완료: {path}");
    }
}