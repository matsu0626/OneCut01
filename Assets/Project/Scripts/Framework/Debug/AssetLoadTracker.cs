#if _DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AssetLoadHelper 経由でロードされた「単品アセット/インスタンス」を追跡するトラッカー。
/// ・シーン名でタグ付け
/// ・DebugMenu から「非常駐の現在ロード中アセット一覧」を見る用途
/// </summary>
public static class AssetLoadTracker
{
    public sealed class Entry
    {
        /// <summary>ロードキー（Addressablesキーやパスなど）</summary>
        public string Key;

        /// <summary>アセットの型（Sprite / AudioClip / GameObject など）</summary>
        public Type AssetType;

        /// <summary>実際のアセット参照（アセット or インスタンス）</summary>
        public UnityEngine.Object AssetRef;

        /// <summary>ロード時のシーン名</summary>
        public string SceneName;

        /// <summary>ロード時刻</summary>
        public DateTime LoadedAt;
    }

    // Assetインスタンス → Entry
    private static readonly Dictionary<UnityEngine.Object, Entry> s_entries = new();

    /// <summary>
    /// ロード完了時に呼ぶ。
    /// シーン名は常に現在のアクティブシーン名でタグ付けする。
    /// </summary>
    public static void RegisterLoad(string key, UnityEngine.Object asset)
    {
        if (!asset)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;

        var entry = new Entry
        {
            Key = key,
            AssetType = asset.GetType(),
            AssetRef = asset,
            SceneName = sceneName,
            LoadedAt = DateTime.Now,
        };

        s_entries[asset] = entry;
    }

    /// <summary>
    /// Release / Dispose 時に呼ぶ。
    /// </summary>
    public static void RegisterRelease(UnityEngine.Object asset)
    {
        if (!asset)
        {
            return;
        }

        s_entries.Remove(asset);
    }

    /// <summary>
    /// 現在のエントリ一覧を取得（必要なら DebugMenu 側で加工）。
    /// </summary>
    public static IReadOnlyList<Entry> GetSnapshot()
    {
        return s_entries.Values.ToList();
    }

    /// <summary>
    /// DebugMenu 用の一覧テキストを構築するユーティリティ。
    /// currentSceneOnly = true なら現在シーン分のみ。
    /// </summary>
    public static string BuildDebugText(bool currentSceneOnly)
    {
        if (s_entries.Count == 0)
        {
            return "Non-Resident Assets (via AssetLoadHelper): (none)";
        }

        string currentScene = SceneManager.GetActiveScene().name;

        IEnumerable<Entry> entries = s_entries.Values;
        if (currentSceneOnly)
        {
            entries = entries.Where(e => e.SceneName == currentScene);
        }

        var groups = entries
            .OrderBy(e => e.SceneName)
            .ThenBy(e => e.Key)
            .GroupBy(e => e.SceneName);

        var sb = new StringBuilder();
        sb.AppendLine("=== Non-Resident Assets (via AssetLoadHelper) ===");

        foreach (var group in groups)
        {
            sb.AppendLine($"[Scene: {group.Key}]");
            foreach (var e in group)
            {
                sb.AppendLine($"  {e.Key}  ({e.AssetType.Name})  LoadedAt={e.LoadedAt:HH:mm:ss}");
            }
        }

        return sb.ToString();
    }
}
#endif
