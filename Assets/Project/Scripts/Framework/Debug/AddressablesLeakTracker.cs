#if _DEBUG
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables インスタンスのリークを検出するための軽量トラッカー（_DEBUG専用）
/// - Track(...) で生成直後に登録
/// - Untrack(...) を Release/Dispose タイミングで呼ぶ（AssetLoadHelper から自動で行う前提）
/// - ReportPeriodically(sec) で一定間隔ごとに概要をログ出力
/// - ReportNow() で即時ダンプ
/// 
/// 仕組み：WeakReference<GameObject> を保持し、Untrack が来ないまま
/// 参照が生きている（＝シーン上に残り続けている）ものを「要注意」として報告。
/// </summary>
public static class AddressablesLeakTracker
{
    private sealed class Entry
    {
        public int InstanceId;                       // GameObject.GetInstanceID()
        public WeakReference<GameObject> WeakGO;     // 生存確認用
        public string Key;                           // Addressablesキー（不明なら空）
        public string Where;                         // 呼び出し元スタック先頭（簡易）
        public DateTime TrackedAtUtc;
        public float Seconds(float now) => now - _startUpTime; // 表示用
    }

    // ID -> Entry
    private static readonly Dictionary<int, Entry> s_entries = new();

    // Report 間隔制御
    private static float s_timer;
    private static float s_interval = 10f;
    private static float _startUpTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RIL_Reset()
    {
        s_entries.Clear();
        s_timer = 0f;
        _startUpTime = Time.realtimeSinceStartup;
    }

    /// <summary>Addressables.Instantiate 直後に登録（Key が分かる時）</summary>
    public static void Track(GameObject go, string key = "", AsyncOperationHandle? handle = null)
    {
        if (!go) return;
        var id = go.GetInstanceID();

        var where = GetCallerTopFrame(2);
        s_entries[id] = new Entry
        {
            InstanceId = id,
            WeakGO = new WeakReference<GameObject>(go),
            Key = key ?? string.Empty,
            Where = where,
            TrackedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>内部ラッパ（Instantiated）からの登録用。key/handle を渡せる場合はこちらでもOK</summary>
    public static void Track(object instOwner, GameObject go, string key = "", AsyncOperationHandle? handle = null)
        => Track(go, key, handle);

    /// <summary>Release/Dispose タイミングで必ず呼ぶ</summary>
    public static void Untrack(GameObject go)
    {
        if (!go) return;
        s_entries.Remove(go.GetInstanceID());
    }

    /// <summary>Instantiated ラッパから Untrack（ownerは未使用だがオーバーロードとして用意）</summary>
    public static void Untrack(object instOwner, GameObject go) => Untrack(go);

    /// <summary>定期レポート。例：Update 内で AddressablesLeakTracker.ReportPeriodically(10f);</summary>
    public static void ReportPeriodically(float seconds)
    {
        s_interval = Mathf.Max(1f, seconds);
        s_timer += Time.unscaledDeltaTime;
        if (s_timer < s_interval) return;
        s_timer = 0f;
        if (s_entries.Count == 0) return;

        // 死んでる（Weak 参照切れ）の掃除
        SweepDead();

        if (s_entries.Count > 0)
        {
            Debug.Log(BuildSummaryString(limitDetails: 5));
        }
    }

    /// <summary>今すぐダンプ</summary>
    public static void ReportNow()
    {
        SweepDead();
        Debug.Log(BuildSummaryString(limitDetails: 20));
    }

    // ---- internals ----

    private static void SweepDead()
    {
        var dead = ListPool<int>.Get();
        foreach (var kv in s_entries)
        {
            if (!kv.Value.WeakGO.TryGetTarget(out var go) || go == null)
                dead.Add(kv.Key);
        }
        foreach (var id in dead) s_entries.Remove(id);
        ListPool<int>.Release(dead);
    }

    private static string BuildSummaryString(int limitDetails)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[AddressablesLeakTracker] tracked objects: {s_entries.Count}");
        int count = 0;
        foreach (var e in s_entries.Values)
        {
            if (count++ >= limitDetails) break;
            var alive = e.WeakGO.TryGetTarget(out var go) && go != null;
            var name = alive ? go.name : "(dead)";
            var age = (DateTime.UtcNow - e.TrackedAtUtc).TotalSeconds;
            sb.AppendLine($"  - #{e.InstanceId} \"{name}\" key=\"{e.Key}\" age={age:F1}s where={e.Where}");
        }
        if (s_entries.Count > limitDetails)
        {
            sb.AppendLine($"  ... and {s_entries.Count - limitDetails} more");
        }
        return sb.ToString();
    }

    private static string GetCallerTopFrame(int skipFrames)
    {
        try
        {
            var st = new System.Diagnostics.StackTrace(skipFrames, true);
            var f = st.GetFrame(0);
            if (f == null) return "";
            var m = f.GetMethod();
            return $"{m?.DeclaringType?.Name}.{m?.Name}({System.IO.Path.GetFileName(f.GetFileName())}:{f.GetFileLineNumber()})";
        }
        catch { return ""; }
    }

    // --- very small ListPool for GC-friendliness in _DEBUG ---
    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new();
        public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>(8);
        public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
    }
}
#endif
