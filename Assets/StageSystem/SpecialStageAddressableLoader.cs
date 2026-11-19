using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public sealed class SpecialStageAddressableLoader
{
    AsyncOperationHandle<IList<IResourceLocation>> m_LocationHandle;
    readonly List<AsyncOperationHandle<SpecialStageData>> m_AssetHandles = new();
    bool m_HasLocationHandle;

    public bool Succeeded { get; private set; }

    public IEnumerator LoadAll(List<SpecialStageData> output)
    {
        m_LocationHandle = Addressables.LoadResourceLocationsAsync(
            SpecialStageAddressKeys.Label,
            typeof(SpecialStageData));
        m_HasLocationHandle = true;
        yield return m_LocationHandle;

        if (m_LocationHandle.Status != AsyncOperationStatus.Succeeded)
            yield break;

        foreach (var location in m_LocationHandle.Result)
        {
            var handle = Addressables.LoadAssetAsync<SpecialStageData>(location);
            m_AssetHandles.Add(handle);
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                yield break;

            if (handle.Result != null)
                output.Add(handle.Result);
        }

        Succeeded = true;
    }

    public void Release()
    {
        foreach (var handle in m_AssetHandles)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        m_AssetHandles.Clear();
        if (m_HasLocationHandle && m_LocationHandle.IsValid())
            Addressables.Release(m_LocationHandle);

        m_HasLocationHandle = false;
    }
}
