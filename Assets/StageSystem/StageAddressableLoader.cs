using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class StageAddressableLoader
{
    AsyncOperationHandle<IList<StageData>> m_Handle;
    bool m_HasHandle;

    public bool Succeeded { get; private set; }

    public IEnumerator LoadAll(List<StageData> output)
    {
        m_Handle = Addressables.LoadAssetsAsync<StageData>(
            StageAddressKeys.Label,
            null);
        m_HasHandle = true;
        yield return m_Handle;

        Succeeded = m_Handle.Status == AsyncOperationStatus.Succeeded;
        if (!Succeeded)
            yield break;

        foreach (var stage in m_Handle.Result)
        {
            if (stage != null)
                output.Add(stage);
        }
    }

    public void Release()
    {
        if (!m_HasHandle)
            return;

        Addressables.Release(m_Handle);
        m_HasHandle = false;
    }
}
