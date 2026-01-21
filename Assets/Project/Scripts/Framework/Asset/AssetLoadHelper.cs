using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AssetLoadHelper
{
    // ----------- LoadedAsset<T> -----------
    public readonly struct LoadedAsset<T> : IDisposable where T : UnityEngine.Object
    {
        public readonly AsyncOperationHandle<T> Handle;
        public readonly CancellationToken m_token;

        public T Asset => Handle.IsValid() ? Handle.Result : null;

        internal LoadedAsset(AsyncOperationHandle<T> handle, CancellationToken token = default)
        {
            Handle = handle;
            m_token = token;
        }

        public async UniTask Wait()
        {
            while (!Handle.IsDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, m_token);
            }
        }

        public void Dispose()
        {
#if _DEBUG
            // ロード成功していればトラッカーから解除
            if (Handle.IsValid() && Handle.Status == AsyncOperationStatus.Succeeded && Handle.Result)
            {
                AssetLoadTracker.RegisterRelease(Handle.Result);
            }
#endif
            if (Handle.IsValid())
            {
                Addressables.Release(Handle);
            }
        }
    }

    // ----------- Instantiated -----------
    public readonly struct Instantiated : IDisposable
    {
        public readonly AsyncOperationHandle<GameObject> Handle;
        public readonly CancellationToken m_token;

        private readonly GameObject m_instance;
        public GameObject Instance => Handle.IsValid() ? (m_instance ? m_instance : Handle.Result) : null;

        internal Instantiated(AsyncOperationHandle<GameObject> handle, GameObject instance, CancellationToken token = default)
        {
            Handle = handle;
            m_instance = instance;
            m_token = token;
        }

        public async UniTask Wait()
        {
            while (!Handle.IsDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, m_token);
            }
        }

        public void Dispose()
        {
#if _DEBUG
            try
            {
                if (m_instance)
                {
                    // 非常駐トラッカーから解除
                    AssetLoadTracker.RegisterRelease(m_instance);
                    // AddressablesLeakTracker からも解除
                    AddressablesLeakTracker.Untrack(this, m_instance);
                }
            }
            catch
            {
                // ignore
            }
#endif
            if (Handle.IsValid())
            {
                Addressables.ReleaseInstance(Handle);
            }
            else if (m_instance)
            {
                UnityEngine.Object.Destroy(m_instance);
            }
        }
    }

    // ----------- Load: key -----------
    public static async UniTask<LoadedAsset<T>> LoadAsync<T>(
        string key, CancellationToken token = default) where T : UnityEngine.Object
    {
        var h = Addressables.LoadAssetAsync<T>(key);
        try
        {
            await h.Task.AsUniTask().AttachExternalCancellation(token);

#if _DEBUG
            if (h.Status == AsyncOperationStatus.Succeeded && h.Result)
            {
                AssetLoadTracker.RegisterLoad(key, h.Result);
            }
#endif
            return new LoadedAsset<T>(h, token);
        }
        catch
        {
            if (h.IsValid() && h.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(h);
            }
            throw;
        }
    }

    // ----------- Load: AssetReferenceT -----------
    public static async UniTask<LoadedAsset<T>> LoadAsync<T>(
        AssetReferenceT<T> reference, CancellationToken token = default) where T : UnityEngine.Object
    {
        var h = reference.LoadAssetAsync();
        try
        {
            await h.Task.AsUniTask().AttachExternalCancellation(token);

#if _DEBUG
            if (h.Status == AsyncOperationStatus.Succeeded && h.Result)
            {
                string key = reference.RuntimeKey?.ToString() ?? string.Empty;
                AssetLoadTracker.RegisterLoad(key, h.Result);
            }
#endif
            return new LoadedAsset<T>(h, token);
        }
        catch
        {
            if (h.IsValid() && h.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(h);
            }
            throw;
        }
    }

    // ----------- Instantiate: key（親省略版） -----------
    public static UniTask<Instantiated> InstantiateAsync(
        string key, Transform parent = null, CancellationToken token = default)
        => InstantiateAsync(key, Vector3.zero, Quaternion.identity, parent, token);

    // ----------- Instantiate: key -----------
    public static async UniTask<Instantiated> InstantiateAsync(
        string key,
        Vector3 position,
        Quaternion rotation,
        Transform parent = null,
        CancellationToken token = default)
    {
        var h = Addressables.InstantiateAsync(key, position, rotation, parent);
        GameObject go = null;
        try
        {
            await h.Task.AsUniTask().AttachExternalCancellation(token);
            go = h.Result;

#if _DEBUG
            try
            {
                if (go)
                {
                    AddressablesLeakTracker.Track(go, key, h);
                    AssetLoadTracker.RegisterLoad(key, go);
                }
            }
            catch
            {
                // ignore
            }
#endif
            return new Instantiated(h, go, token);
        }
        catch
        {
            if (h.IsValid() && h.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(h);
            }
            else if (go)
            {
                UnityEngine.Object.Destroy(go);
            }
            throw;
        }
    }

    // ----------- Instantiate: AssetReferenceGameObject -----------
    public static async UniTask<Instantiated> InstantiateAsync(
        AssetReferenceGameObject reference,
        Vector3 position,
        Quaternion rotation,
        Transform parent = null,
        CancellationToken token = default)
    {
        var h = reference.InstantiateAsync(position, rotation, parent);
        GameObject go = null;
        try
        {
            await h.Task.AsUniTask().AttachExternalCancellation(token);
            go = h.Result;

#if _DEBUG
            string key = reference.RuntimeKey?.ToString() ?? string.Empty;
            try
            {
                if (go)
                {
                    AddressablesLeakTracker.Track(go, key, h);
                    AssetLoadTracker.RegisterLoad(key, go);
                }
            }
            catch
            {
                // ignore
            }
#endif
            return new Instantiated(h, go, token);
        }
        catch
        {
            if (h.IsValid() && h.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(h);
            }
            else if (go)
            {
                UnityEngine.Object.Destroy(go);
            }
            throw;
        }
    }

    // ----------- Instantiate and Get<T> -----------
    public static async UniTask<(Instantiated handle, T comp)> InstantiateAndGetAsync<T>(
        string key,
        Vector3 position,
        Quaternion rotation,
        Transform parent = null,
        CancellationToken token = default)
        where T : Component
    {
        var inst = await InstantiateAsync(key, position, rotation, parent, token);
        var c = inst.Instance ? inst.Instance.GetComponent<T>() : null;
        if (c)
        {
            return (inst, c);
        }

        inst.Dispose(); // 見つからなければ即解放（このときトラッカーからも解除）
        throw new InvalidOperationException($"{key}: {typeof(T).Name} が見つかりません。");
    }
}
