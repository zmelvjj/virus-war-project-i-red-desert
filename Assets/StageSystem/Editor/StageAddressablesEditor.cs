using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class StageAddressablesEditor
{
    [MenuItem("Tools/Stage/Register Selected StageData")]
    static void RegisterSelected()
    {
        RegisterAssets(Selection.objects.OfType<StageData>());
    }

    [MenuItem("Tools/Stage/Register Selected StageData", true)]
    static bool ValidateRegisterSelected()
    {
        return Selection.objects.OfType<StageData>().Any();
    }

    [MenuItem("Tools/Stage/Register StageData Folder")]
    static void RegisterSelectedFolder()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!AssetDatabase.IsValidFolder(path))
            path = Path.GetDirectoryName(path)?.Replace('\\', '/');

        RegisterFolder(path);
    }

    public static void RegisterFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)
            || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError("[StageAddressables] 유효한 StageData 폴더를 선택하세요.");
            return;
        }

        var assets = AssetDatabase.FindAssets("t:StageData", new[] { folderPath })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<StageData>);
        RegisterAssets(assets);
    }

    public static void RegisterAssets(IEnumerable<StageData> assets)
    {
        var stageData = assets.Where(stage => stage != null).ToList();
        if (stageData.Count == 0)
        {
            Debug.LogError("[StageAddressables] 등록할 StageData가 없습니다.");
            return;
        }

        var duplicate = stageData
            .GroupBy(stage => stage.StageIndex)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate != null)
        {
            Debug.LogError($"[StageAddressables] 중복 StageIndex: {duplicate.Key}");
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        var group = settings.FindGroup(StageAddressKeys.GroupName)
            ?? settings.CreateGroup(
                StageAddressKeys.GroupName,
                false,
                false,
                true,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));

        foreach (var stage in stageData)
        {
            var path = AssetDatabase.GetAssetPath(stage);
            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = StageAddressKeys.GetAddress(stage.StageIndex);
            entry.SetLabel(StageAddressKeys.Label, true, true);
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"[StageAddressables] StageData {stageData.Count}개 등록 완료.");
    }
}
