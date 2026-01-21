#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab Mode を開いた間だけ、Canvas(Environment) の CanvasScaler を
/// 「実行時の RootCanvas(UI)」か「UIConstants 既定」に一時同期する。
/// 閉じる際に必ず元値へ復元する（脆さを最小化）
/// </summary>
[InitializeOnLoad]
public static class PrefabEnvScalerSync
{
    // 同期対象のバックアップ
    private struct ScalerBackup
    {
        public bool found;
        public Canvas envCanvas;
        public CanvasScaler scaler;
        public bool scalerWasAdded;

        public CanvasScaler.ScaleMode uiScaleMode;
        public Vector2 referenceResolution;
        public CanvasScaler.ScreenMatchMode screenMatchMode;
        public float matchWidthOrHeight;
    }

    private static ScalerBackup s_backup;

    static PrefabEnvScalerSync()
    {
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
    }

    private static void OnPrefabStageOpened(PrefabStage stage)
    {
        // 1) Environment 側 Canvas を取得（prefabContentsRoot 配下は除外）
        var env = FindEnvironmentCanvas(stage);
        if (!env) { s_backup = default; return; }

        // 2) CanvasScaler を確保（なければ一時的に追加）
        var scaler = env.GetComponent<CanvasScaler>();
        bool added = false;
        if (!scaler)
        {
            scaler = Undo.AddComponent<CanvasScaler>(env.gameObject);
            added = true;
        }

        // 3) 元値をバックアップ
        s_backup = new ScalerBackup
        {
            found = true,
            envCanvas = env,
            scaler = scaler,
            scalerWasAdded = added,
            uiScaleMode = scaler.uiScaleMode,
            referenceResolution = scaler.referenceResolution,
            screenMatchMode = scaler.screenMatchMode,
            matchWidthOrHeight = scaler.matchWidthOrHeight
        };

        // 4) 実行時 RootCanvas(UI) の設定を優先的に同期、無ければ UIConstants にフォールバック
        var runtime = FindRuntimeRootCanvasUI();
        if (runtime)
        {
            var runScaler = runtime.GetComponent<CanvasScaler>();
            if (runScaler && runScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                ApplyScaler(scaler,
                    CanvasScaler.ScaleMode.ScaleWithScreenSize,
                    runScaler.referenceResolution,
                    runScaler.screenMatchMode,
                    runScaler.matchWidthOrHeight);
                return;
            }
        }

        // Fallback（プロジェクト既定）
        ApplyScaler(scaler,
            CanvasScaler.ScaleMode.ScaleWithScreenSize,
            new Vector2(UIConstants.ScreenWidth, UIConstants.ScreenHeight),
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,
            UIConstants.MatchWidthOrHeight);
    }

    private static void OnPrefabStageClosing(PrefabStage stage)
    {
        if (!s_backup.found) return;

        // 同じステージに属するかを確認（まれに多重起動対策）
        if (!s_backup.envCanvas ||
            PrefabStageUtility.GetPrefabStage(s_backup.envCanvas.gameObject) != stage)
        {
            s_backup = default;
            return;
        }

        // 1) スケーラーの元値を復元
        if (s_backup.scaler)
        {
            s_backup.scaler.uiScaleMode = s_backup.uiScaleMode;
            s_backup.scaler.referenceResolution = s_backup.referenceResolution;
            s_backup.scaler.screenMatchMode = s_backup.screenMatchMode;
            s_backup.scaler.matchWidthOrHeight = s_backup.matchWidthOrHeight;
        }

        // 2) 追加していた場合は削除
        if (s_backup.scalerWasAdded && s_backup.scaler)
        {
            Object.DestroyImmediate(s_backup.scaler, allowDestroyingAssets: false);
        }

        s_backup = default;
    }

    // === helpers ===

    private static void ApplyScaler(CanvasScaler scaler,
        CanvasScaler.ScaleMode mode, Vector2 refRes,
        CanvasScaler.ScreenMatchMode matchMode, float match)
    {
        scaler.uiScaleMode = mode;
        scaler.referenceResolution = refRes;
        scaler.screenMatchMode = matchMode;
        scaler.matchWidthOrHeight = Mathf.Clamp01(match);
        EditorUtility.SetDirty(scaler);
    }

    /// <summary>
    /// Prefab モードの Environment 側 Canvas を取得。
    /// （prefabContentsRoot 配下＝プレハブ本体は除外）
    /// </summary>
    private static Canvas FindEnvironmentCanvas(PrefabStage stage)
    {
        if (stage == null) return null;

        foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            var go = c.gameObject;
            var owner = PrefabStageUtility.GetPrefabStage(go);
            if (owner != stage) continue; // 別ステージは除外

            // プレハブ本体（prefabContentsRoot配下）は除外 → Environment 側のみ対象
            if (stage.prefabContentsRoot != null &&
                go.transform.IsChildOf(stage.prefabContentsRoot.transform))
                continue;

            return c;
        }
        return null;
    }

    /// <summary>
    /// 実行時に使う RootCanvas(UI) を探索（sortingOrder==UI の root）。
    /// </summary>
    private static Canvas FindRuntimeRootCanvasUI()
    {
        const int targetOrder = UIConstants.CanvasSortOrder.UI;
        var all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (!c) continue;
            if (c.isRootCanvas && c.sortingOrder == targetOrder)
                return c.rootCanvas;
        }
        return null;
    }
}
#endif
