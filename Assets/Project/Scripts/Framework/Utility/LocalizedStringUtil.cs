using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

/// <summary>
/// LocalizedStringユーティリティ
/// </summary>
public static class LocalizedStringUtil
{
    /// <summary>
    /// 文字列取得・Collection + Key (+ Smart引数)
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="entryKey"></param>
    /// <param name="smartArgs"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async UniTask<string> GetStringAsync(
        string collectionName,
        string entryKey,
        object[] smartArgs = null,
        CancellationToken token = default)
    {
        var ls = new LocalizedString(collectionName, entryKey);
        if (smartArgs != null && smartArgs.Length > 0)
            ls.Arguments = smartArgs;

        return await GetStringAsync(ls, token);
    }

    /// <summary>
    /// LocalizedString 直接
    /// </summary>
    /// <param name="ls"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async UniTask<string> GetStringAsync(LocalizedString ls, CancellationToken token = default)
    {
        var handle = ls.GetLocalizedStringAsync();  // Addressables経由の非同期
        await handle.Task;                          // ← キャンセルは素直には効かないので待つだけ
        return handle.Result;
    }
}
