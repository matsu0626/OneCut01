using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


/// <summary>
/// Assetグループ読み込み
/// </summary>
public class AssetGroupLoader
{
    private CancellationToken m_token;
    private string m_label;
    private AsyncOperationHandle<IList<UnityEngine.Object>> m_handle;
    private Dictionary<string, UnityEngine.Object> m_dict = new();
    
    /// <summary>
    /// ラベル指定でAsset一括読み込み
    /// </summary>
    /// <param name="label"></param>
    public AssetGroupLoader LoadAssetsAsync(CancellationToken token, string label, Action<AssetGroupLoader> onCompleted = null)
    {
        m_token = token;
        m_label = label;
        m_handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(label,
            obj =>
            {
                if (m_dict.ContainsKey(obj.name))
                {
                    AppDebug.LogWarning($"重複: {obj.name}");
                    return;
                }

                m_dict[obj.name] = obj;
            });
        m_handle.Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                 onCompleted?.Invoke(this);
            }
            else
            {
                AppDebug.LogError($"AssetGroupLoader: {m_label} 読み込みに失敗しました.");
            }
        };

        m_handle.Task.AsUniTask().AttachExternalCancellation(m_token);
        return this;
    }

    /// <summary>
    /// 読み込み待ち
    /// </summary>
    /// <param name="progress"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async UniTask Wait(IProgress<float> progress = null)
    {
        while (!m_handle.IsDone)
        {
            progress?.Report(m_handle.PercentComplete);
            await UniTask.Yield(PlayerLoopTiming.Update, m_token);
        }
    }



    /// <summary>
    /// ファイル名(拡張子なし)と型指定してAsset取得
    /// ※Addressablesキー名ではないので注意
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public T GetAsset<T>(string name) where T : UnityEngine.Object
    {
        if (m_dict.TryGetValue(name, out var obj))
        {
            return obj as T;
        }
        AppDebug.Assert(false, $"Assetが見つかりません: {name}");
        return null;
    }

    /// <summary>
    ///読み込んだアセット数取得
    /// </summary>
    /// <returns></returns>
    public int GetAssetCount()
    {
        return m_handle.Result.Count;
    }

    /// <summary>
    /// 読み込んだファイル名リスト取得
    /// </summary>
    /// <returns></returns>
    public List<string> GetFileNameList()
    {
        var list = new List<string>(m_dict.Keys);
        // 大文字小文字を区別する場合：Ordinal、区別しない場合：OrdinalIgnoreCase
        list.Sort(StringComparer.Ordinal); // or StringComparer.OrdinalIgnoreCase
        return list;
    }
    

    /// <summary>
    /// 解放
    /// </summary>
    public void Release()
    {
        Addressables.Release(m_handle);
    }

#if _DEBUG   
    /// <summary>
    /// アセット一覧をダンプ
    /// </summary>
    [Conditional("_DEBUG")]
    public void DumpAllAssets()
    {
        AppDebug.Log($"### AssetGroupLoader - DumpAllAssets: {m_label}");
        foreach (var kv in m_dict.OrderBy(kv => kv.Key))
        {
            AppDebug.Log($"   {kv.Key} : {kv.Value.GetType().Name}");
        }
    }
#endif
    

}
