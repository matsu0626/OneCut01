#if UNITY_EDITOR
using System;

using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;



/// <summary>
/// エディタメニュー・Windows ビルド
/// </summary>
public static class WindowsBuild
{
    static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
     

    /// <summary>
    /// ビルドフラグ設定
    /// </summary>
    /// <param name="dev"></param>
    private static void SetDefines(bool dev)
    {
        var nbt = NamedBuildTarget.Standalone;
        var cur = PlayerSettings.GetScriptingDefineSymbols(nbt).Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        void Add(string s) { if (!cur.Contains(s)) cur.Add(s); }
        void Del(string s) { cur.RemoveAll(x => x == s); }
        if (dev)
        {
            Add("_DEBUG");
            Del("_PROD");
        }
        else
        {
            Add("_PROD");
            Del("_DEBUG");
        }
        PlayerSettings.SetScriptingDefineSymbols(nbt, string.Join(";", cur));
    }

    private static string GetBuildPath(bool dev)
    {
        // 日付時刻でフォルダを作成
        var now = DateTime.Now;
        var timeStamp = now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string buildType = dev ? "Debug" : "Release";
        string path = "Build/" + timeStamp + "_" + buildType + "/" + Application.productName + buildType + ".exe";
        return path;
    }

    // 例: yyyyMMdd_HHmmss or yyyyMMdd をディレクトリ名から解釈
    static DateTime? ParseStampFromName(string name)
    {
        // 例: 20251008_134522 / 20251008
        string[] fmts = { "yyyyMMdd_HHmmss", "yyyyMMdd" };
        if (DateTime.TryParseExact(name, fmts, CultureInfo.InvariantCulture,
                                   DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                   out var dt))
            return dt;
        return null; // 合わなければ null（作成日時フォールバック）
    }

    /*
    public static void Clean()
    {
        var buildRootPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Build");

        var dirs = new DirectoryInfo(buildRootPath).GetDirectories();

        // 候補抽出：タイムスタンプ名 or フォールバック。必要ならexe有無で絞る
        var items = dirs
            .Select(d =>
            {
                var ts = ParseStampFromName(d.Name) ?? d.CreationTimeUtc;
                return new { Dir = d, Stamp = ts };
            })
            .Where(x => x.Dir.EnumerateFiles("*.exe", SearchOption.AllDirectories).Any())
            .OrderByDescending(x => x.Stamp) // 新しい順
            .ToList();

        if (items.Count <= maxKeep)
        {
            Debug.Log($"[PruneBuilds] Nothing to prune. Count={items.Count}, keep={maxKeep}");
            return;
        }

        var toDelete = items.Skip(maxKeep).Select(x => x.Dir).ToList();
        foreach (var d in toDelete)
        {
            try
            {
                Debug.Log($"[PruneBuilds] Delete: {d.FullName}");
                d.Delete(true); // 中身ごと削除
            }
            catch (Exception e)
            {
                Debug.LogError($"[PruneBuilds] Failed to delete: {d.FullName}\n{e}");
            }
        }
    }*/

    private static void BuildWindowsDebug(AddressablesBuild.Lang lang)
    {
        AddressablesBuild.Clean();
        AddressablesBuild.Build(true, lang);

        SetDefines(true);
        string path = GetBuildPath(true);
        var opts = new BuildPlayerOptions
        {
            scenes = Scenes,
            target = BuildTarget.StandaloneWindows64,
            locationPathName = path,
            options = BuildOptions.CleanBuildCache |
                      BuildOptions.Development |
                      BuildOptions.AllowDebugging
        };
        var r = BuildPipeline.BuildPlayer(opts);
        if (r.summary.result != BuildResult.Succeeded)
        {
            throw new Exception("Debug build failed");
        }
    }

    /// <summary>
    /// デバッグビルド(ja)
    /// </summary>
    /// <exception cref="System.Exception"></exception>
    [MenuItem("Tools/Build/Windows(ja)(Debug)")]
    private static void BuildWindowsDebug()
    {
        BuildWindowsDebug(AddressablesBuild.Lang.JA);
    }

    /// <summary>
    /// デバッグビルド(en)
    /// </summary>
    /// <exception cref="System.Exception"></exception>
    [MenuItem("Tools/Build/Windows(en)(Debug)")]
    private static void BuildWindowsDebugEN()
    {
        BuildWindowsDebug(AddressablesBuild.Lang.EN);
    }

    private static void BuildWindowsRelease(AddressablesBuild.Lang lang)
    {
        AddressablesBuild.Clean();
        AddressablesBuild.Build(false, lang);

        SetDefines(false);
        string path = GetBuildPath(false);
        var opts = new BuildPlayerOptions
        {
            scenes = Scenes,
            target = BuildTarget.StandaloneWindows64,
            locationPathName = path,
            options = BuildOptions.CleanBuildCache |
                      BuildOptions.None
        };
        var r = BuildPipeline.BuildPlayer(opts);
        if (r.summary.result == BuildResult.Succeeded)
        {
            // マクロ定義をデバッグに戻しておく
            SetDefines(true);
        }
        else
        {
            SetDefines(true);
            throw new System.Exception("Release build failed");
        }
    }

    /// <summary>
    /// リリースビルド(ja)
    /// </summary>
    /// <exception cref="System.Exception"></exception>
    [MenuItem("Tools/Build/Windows(ja)(Release)")]
    public static void BuildWindowsRelease()
    {
        BuildWindowsRelease(AddressablesBuild.Lang.JA);
    }

    /// <summary>
    /// リリースビルド(en)
    /// </summary>
    /// <exception cref="System.Exception"></exception>
    [MenuItem("Tools/Build/Windows(en)(Release)")]
    public static void BuildWindowsReleaseEN()
    {
        BuildWindowsRelease(AddressablesBuild.Lang.EN);
    }

}    
#endif
