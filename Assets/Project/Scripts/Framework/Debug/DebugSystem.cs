#if _DEBUG
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;

/// <summary>
/// デバッグ・システム統合（CPU/FPS/メモリHUD）
/// ・SingletonMonoBehaviour 基底で重複破棄＆DDOLは基底に依存
/// ・メモリはMB固定閾値ではなく「%」のみで評価
/// ・HUDは DebugPrint へ出力（最下段1行）
/// </summary>
/*
●メモリ指標の見方
・Used(%)
「このUnityプロセスが端末の物理メモリに対してどのくらい使っているか」の概算比率。
中身は“総使用メモリ（マネージド＋ネイティブ＋一部ドライバ側の確保）”を物理メモリで割った値。
目安：80%超が続くとOSに押し出されやすく、スワップ／強制終了のリスクが上がる。

・GC
C#（Mono/IL2CPP）のマネージドヒープ使用量。
new したC#オブジェクトや配列など、ガベコレ対象の領域。
スパイク（突然の増減）はGC実行の合図になりやすい。頻繁な増加は短命オブジェクトの多用を疑う。

・Gfx
グラフィクスドライバが確保しているメモリの概算。テクスチャ・メッシュ・バッファ等に紐づくドライバ側の確保を指す。
注意：ここはGPUのVRAMそのもの≠。プラットフォームによってはドライバのシステムメモリ側確保が中心で、表示値と実VRAMは必ずしも一致しない。

●使い方のコツ
・Used(%)が高止まり → 生成/破棄の多い箇所、巨大アセットの常駐、アセットの重複ロード（Addressablesの参照漏れ）をチェック。
・GCが波打つ → 一時配列/文字列結合/Boxing、LINQやforeachのキャプチャなど短命アロケーションの削減を検討。
・Gfxが増え続ける → テクスチャ/メッシュ/マテリアルの生存管理、Resources.UnloadUnusedAssets()やAddressablesのRelease*のタイミング、圧縮/ミップ設定を見直す。
*/
public sealed class DebugSystem : SingletonMonoBehaviour<DebugSystem>
{
    // ====== CPU ======
    [Header("CPU")]
    [SerializeField] private float m_cpuInterval = 1f; // sec
    private float m_cpuAccum;
    private Process m_proc;
    private TimeSpan m_lastCpu;
    private double m_lastWall;
    private int m_logicalCores;
    private float m_cpuPercent; // 0-100

    // ====== FPS ======
    [Header("FPS")]
    [SerializeField] private float m_fpsWindow = 1f; // sec
    private float m_fpsDelta;
    private int m_fpsCount;
    private float m_fps;

    // ====== Memory ======
    [Header("Memory")]
    [SerializeField] private float m_memInterval = 1f; // sec
    [Range(0, 100)][SerializeField] private int m_warnPercent = 80;
    [Range(0, 100)][SerializeField] private int m_dangerPercent = 90;
    [SerializeField] private bool m_showHud = true;
    [SerializeField] private bool m_reportAddressablesLeaks = true;

    /// <summary>
    /// HUD表示の有無
    /// </summary>
    public bool ShowHud
    {
        get { return m_showHud; }
        set { m_showHud = value; }
    }

    private float m_memAccum;
    private int m_usedMB, m_gcMB, m_gfxMB, m_pctUsed;
    private int m_lastMemLevel; // 0:normal / 1:warn / 2:danger

    // ProfilerRecorders（取得できなければ fallback）
    private ProfilerRecorder m_totalUsed;
    private ProfilerRecorder m_gcUsed;
    private ProfilerRecorder m_gfxUsed;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        // CPU init
        m_proc = Process.GetCurrentProcess();
        m_lastCpu = m_proc.TotalProcessorTime;
        m_lastWall = Time.realtimeSinceStartupAsDouble;
        m_logicalCores = Math.Max(1, Environment.ProcessorCount);
        m_cpuAccum = 0f;
        m_cpuPercent = 0f;

        // FPS init
        m_fpsDelta = 0f;
        m_fpsCount = 0;
        m_fps = 0f;

        // Memory init
        m_memAccum = 0f;
        m_lastMemLevel = 0;

        TryStart(ref m_totalUsed, ProfilerCategory.Memory, "Total Used Memory");
        TryStart(ref m_gcUsed, ProfilerCategory.Memory, "GC Used Memory");
        TryStart(ref m_gfxUsed, ProfilerCategory.Memory, "Gfx Used Memory");

        Application.lowMemory += OnLowMemory;
    }

    private void OnDisable()
    {
        Application.lowMemory -= OnLowMemory;

        m_totalUsed.Dispose();
        m_gcUsed.Dispose();
        m_gfxUsed.Dispose();
    }

    private void OnLowMemory()
    {
        AppDebug.LogWarning("[DebugSystem] lowMemory detected → UnloadUnusedAssets + GC.Collect");
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    private void Update()
    {
        // ===== FPS =====
        m_fpsCount++;
        m_fpsDelta += Time.unscaledDeltaTime;
        if (m_fpsDelta >= Mathf.Max(0.2f, m_fpsWindow))
        {
            // 簡易平均FPS
            m_fps = 1f / (m_fpsDelta / Mathf.Max(1, m_fpsCount));
            m_fpsDelta = 0f;
            m_fpsCount = 0;
        }

        // ===== CPU =====
        m_cpuAccum += Time.unscaledDeltaTime;
        if (m_cpuAccum >= Mathf.Max(0.1f, m_cpuInterval))
        {
            m_cpuAccum = 0f;

            var nowCpu = m_proc.TotalProcessorTime;
            var nowWall = Time.realtimeSinceStartupAsDouble;

            var deltaCpuMs = (nowCpu - m_lastCpu).TotalMilliseconds;
            var deltaWallMs = (nowWall - m_lastWall) * 1000.0;

            m_cpuPercent = 0f;
            if (deltaWallMs > 0)
            {
                var ratio = (float)(deltaCpuMs / (deltaWallMs * m_logicalCores));
                m_cpuPercent = Mathf.Clamp01(ratio) * 100f;
            }
            m_lastCpu = nowCpu;
            m_lastWall = nowWall;
        }

        // ===== Memory =====
        m_memAccum += Time.unscaledDeltaTime;
        if (m_memAccum >= Mathf.Max(0.1f, m_memInterval))
        {
            m_memAccum = 0f;

            m_usedMB = ToMB(Get(m_totalUsed, Profiler.GetTotalAllocatedMemoryLong()));
            m_gcMB = ToMB(Get(m_gcUsed, GC.GetTotalMemory(false)));
            m_gfxMB = ToMB(Get(m_gfxUsed, 0));

            var sysMB = SystemInfo.systemMemorySize; // 物理メモリ(MB)
            m_pctUsed = (sysMB > 0) ? Mathf.Clamp(m_usedMB * 100 / sysMB, 0, 100) : 0;

            var level = (m_pctUsed >= m_dangerPercent) ? 2 : (m_pctUsed >= m_warnPercent ? 1 : 0);
            if (level != m_lastMemLevel)
            {
                if (level == 2) AppDebug.LogWarning($"[DebugSystem] MEM DANGER: Used {m_usedMB}MB ({m_pctUsed}%)");
                else if (level == 1) AppDebug.Log($"[DebugSystem] MEM WARN: Used {m_usedMB}MB ({m_pctUsed}%)");
                m_lastMemLevel = level;
            }

            
            // Addressables リークの周期報告
            if (m_reportAddressablesLeaks)
            {
                try { AddressablesLeakTracker.ReportPeriodically(10f); } catch { /* 存在しない場合も安全 */ }
            }
            
        }

        // ===== HUD =====
        if (m_showHud && DebugPrint.Instance != null)
        {
            // カラーはメモリレベルで決定（CPUが高負荷でもメモリ優先で色付け）
            Color col = (m_lastMemLevel == 2) ? Color.red : (m_lastMemLevel == 1 ? new Color(1f, 0.8f, 0f) : Color.white);

            // 1行に集約（下段・左寄せ）
            // 例: CPU:35.2%/8C | FPS: 58.3 | Used:1234MB(62%) GC:120MB Gfx:256MB
            DebugPrint.Instance.PrintLineBottom(
                posX: 4f,
                lineFromBottom: 0,
                text: $"CPU:{m_cpuPercent:F1}%/{m_logicalCores}C  |  FPS:{m_fps:F1}  |  Used:{m_usedMB}MB({m_pctUsed}%)  GC:{m_gcMB}MB  Gfx:{m_gfxMB}MB",
                color: col
            );
        }
    }

    // ===== Helpers =====
    private static void TryStart(ref ProfilerRecorder r, ProfilerCategory cat, string name)
    {
        try { r = ProfilerRecorder.StartNew(cat, name, 1); } catch { /* Not available */ }
    }
    private static long Get(ProfilerRecorder r, long fallback) => r.Valid ? r.LastValue : fallback;
    private static int ToMB(long bytes) => (int)(bytes / (1024L * 1024L));
}
#endif
