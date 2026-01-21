using Cysharp.Threading.Tasks;
using System;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Addressables の「共通ラベル」アセットをアプリ全体で常駐管理するクラス。
/// ・ラベルは UIConstants.CommonAssetLabel を参照
/// ・起動時に自動ロード
/// ・アプリ終了まで解放しない（Release しない）
/// </summary>
public sealed class CommonAssetManager : SingletonMonoBehaviour<CommonAssetManager>
{
    // Addressables 一括ロード用ヘルパ
    private readonly AssetGroupLoader m_loader = new();

    // ライフタイム用キャンセル
    private CancellationTokenSource m_cts;

    private bool m_isLoaded;
    private bool m_isLoading;

    /// <summary>ロード済みかどうか。</summary>
    public bool IsLoaded => m_isLoaded;

    /// <summary>ロード中かどうか。</summary>
    public bool IsLoading => m_isLoading;

    /// <summary>使用している Addressables ラベル。</summary>
    public string Label => UIConstants.CommonAssetLabel;

    protected override void Awake()
    {
        base.Awake();
        m_cts = new CancellationTokenSource();
    }

    private void Start()
    {
        // 起動時に必ず自動ロード
        LoadAsync().Forget();
    }

    /// <summary>
    /// Common ラベルのアセットを一括ロード。
    /// すでにロード済み or ロード中の場合は何もしない。
    /// </summary>
    public async UniTask LoadAsync(IProgress<float> progress = null)
    {
        if (m_isLoaded || m_isLoading)
        {
            return;
        }

        m_isLoading = true;
        var token = m_cts?.Token ?? CancellationToken.None;

        // AssetGroupLoader に依頼
        m_loader.LoadAssetsAsync(
            token,
            UIConstants.CommonAssetLabel,
            _ =>
            {
                m_isLoaded = true;
                m_isLoading = false;

#if _DEBUG
                AppDebug.Log($"[CommonAssetManager] Loaded label='{UIConstants.CommonAssetLabel}', count={m_loader.GetAssetCount()}");
                m_loader.DumpAllAssets();
#endif
            });

        try
        {
            await m_loader.Wait(progress);
        }
        catch (OperationCanceledException)
        {
#if _DEBUG
            AppDebug.LogWarning("[CommonAssetManager] LoadAsync canceled.");
#endif
            m_isLoading = false;
        }
    }

    /// <summary>
    /// ファイル名（拡張子なし）＋型指定でアセット取得。
    /// ロード前に呼ばれた場合は警告を出すが、そのまま検索は試みる。
    /// </summary>
    public T Get<T>(string name) where T : UnityEngine.Object
    {
#if _DEBUG
        if (!m_isLoaded)
        {
            AppDebug.LogWarning($"[CommonAssetManager] Get<{typeof(T).Name}>({name}) : Common assets not loaded yet.");
        }
#endif
        return m_loader.GetAsset<T>(name);
    }

    /// <summary>
    /// ファイル名（拡張子なし）＋型指定で TryGet。
    /// ロード前は false を返す。
    /// </summary>
    public bool TryGet<T>(string name, out T asset) where T : UnityEngine.Object
    {
        asset = null;

        if (!m_isLoaded)
        {
            return false;
        }

        var tmp = m_loader.GetAsset<T>(name);
        if (tmp == null)
        {
            return false;
        }

        asset = tmp;
        return true;
    }

    /// <summary>
    /// ロード済みアセット数（デバッグ用）。
    /// </summary>
    public int GetAssetCount()
    {
        return m_loader.GetAssetCount();
    }

    private void OnDestroy()
    {

        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }

        // アプリ終了まで常駐させる前提なので、
        // Addressables の Release はここでは呼ばない。
        // m_loader.Release();
        m_isLoaded = false;
        m_isLoading = false;
    }

#if _DEBUG
    /// <summary>
    /// DebugMenu 用：Common ラベルのロード状況をテキスト化。
    /// </summary>
    public string BuildDebugText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Resident Assets (Common) ===");

        if (!m_isLoaded)
        {
            sb.AppendLine($"Label: {Label}  (not loaded)");
            return sb.ToString();
        }

        int count = m_loader.GetAssetCount();
        sb.AppendLine($"Label: {Label}  Count: {count}");

        foreach (var name in m_loader.GetFileNameList())
        {
            sb.AppendLine($"  - {name}");
        }

        return sb.ToString();
    }
#endif
}
