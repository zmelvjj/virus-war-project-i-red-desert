using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SaveDataToolsWindow : EditorWindow
{
    const int k_DNAAmount = 100;

    [MenuItem("Tools/Save Data")]
    static void Open()
    {
        GetWindow<SaveDataToolsWindow>("Save Data");
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        rootVisualElement.Add(new Button(AddDNA) { text = $"DNA +{k_DNAAmount}" });
        rootVisualElement.Add(new Button(ResetSave) { text = "저장 데이터 초기화" });
    }

    void AddDNA()
    {
        if (EditorApplication.isPlaying)
        {
            if (!SaveManager.HasInstance || !SaveManager.Instance.IsLoaded || !DNAWallet.HasInstance)
            {
                ShowNotification(new GUIContent("게임 저장과 DNA 지갑이 로드된 뒤 사용할 수 있습니다."));
                return;
            }

            DNAWallet.Instance.AddDNA(k_DNAAmount);
            SaveManager.Instance.Save();
            ShowNotification(new GUIContent($"DNA {DNAWallet.Instance.CurrentDNA}"));
        }
        else if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            ShowNotification(new GUIContent(SaveManager.AddDNAToSaveFile(k_DNAAmount)
                ? $"DNA +{k_DNAAmount} 저장 완료"
                : "DNA 저장 실패. Console을 확인하세요."));
        }
    }

    void ResetSave()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            ShowNotification(new GUIContent("Play Mode를 종료한 뒤 초기화할 수 있습니다."));
            return;
        }

        if (EditorUtility.DisplayDialog(
            "저장 데이터 초기화",
            "스테이지 진행도, DNA, 스킬트리, 설정 등 현재 저장 데이터를 기본값으로 되돌릴까요?",
            "초기화",
            "취소"))
        {
            ShowNotification(new GUIContent(SaveManager.ResetSaveFile()
                ? "저장 데이터 초기화 완료"
                : "초기화 실패. Console을 확인하세요."));
        }
    }
}
