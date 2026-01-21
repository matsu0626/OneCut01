#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SceneView に UI の外枠 & SafeArea をオーバーレイ描画し、
/// 左上ツールバーから最小化やカメラの“枠フィット”を行うエディタ補助。
/// - 外枠は RectTransform の実寸を使用（Environment に依存しない）
/// - メニューは Enable のみ（ラベルON/OFFはツールバーで）
/// - 最小化状態は SessionState で保持（Play/Stop 往復でも維持）
/// </summary>
[InitializeOnLoad]
public static class UIEditView
{
    // ====== Menu ======
    private const string M_EDIT_ENABLED = "View/UI Edit View/Enabled";

    // ====== Pref Keys ======
    private const string KEY_MIN = "UIEditView.Min";
    private const string KEY_LABELS = "UIEditView.Labels";
    private const string KEY_ENABLED = "UIEditView.Enabled";

    // 設定（EditorPrefs 永続）
    private static bool s_enabled = true;
    private static bool s_showLabel = true;

    // UI状態（SessionState：Editor起動中は維持）
    private static bool s_minimized
    {
        get => SessionState.GetBool(KEY_MIN, false);
        set
        {
            SessionState.SetBool(KEY_MIN, value);
            EditorPrefs.SetBool(KEY_MIN, value); // 次回起動の初期値にも反映したい場合
        }
    }

    // ====== 見た目調整 ======
    private const float kMargin = 12f; // 画面端マージン
    private const float kPad = 6f;  // ツールバー内 左右パディング
    private const float kHeaderH = 22f; // 1行目（最小化ボタンのみ）
    private const float kMiniBtnSize = 20f; // 最小化ボタンサイズ
    private const float kBtnSpace = 6f;     // ボタン間隔

    // カメラの寄せ係数（1.0=ジャスト、<1でさらに寄せる）
    private const float kFitMarginFactor = 0.5f;

    static UIEditView()
    {
        // 設定復元
        s_enabled = EditorPrefs.GetBool(KEY_ENABLED, true);
        s_showLabel = EditorPrefs.GetBool(KEY_LABELS, true);

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem(M_EDIT_ENABLED)]
    private static void ToggleEnabled()
    {
        s_enabled = !s_enabled;
        EditorPrefs.SetBool(KEY_ENABLED, s_enabled);
        SceneView.RepaintAll();
    }

    private static void OnPlayModeChanged(PlayModeStateChange st)
    {
        if (st == PlayModeStateChange.EnteredEditMode ||
            st == PlayModeStateChange.EnteredPlayMode)
        {
            SceneView.RepaintAll();
        }
    }

    // ====== Main ======
    private static void OnSceneGUI(SceneView sv)
    {
        if (!s_enabled) return;

        var canvas = FindRootCanvasUI();
        if (!canvas) { DrawToolbar(sv, false, Vector3.zero, Vector2.zero); return; }

        var rt = canvas.GetComponent<RectTransform>();
        if (!rt) { DrawToolbar(sv, false, Vector3.zero, Vector2.zero); return; }

        // --- 外枠（RectTransform 実寸） ---
        var worldCorners = new Vector3[4]; // 0:LB,1:LT,2:RT,3:RB
        rt.GetWorldCorners(worldCorners);

        var prevCol = Handles.color;
        Handles.color = Color.white;
        DrawPolyRect(worldCorners);

        // --- SafeArea（Editor 用の正規化値 → ローカル → ワールド）---
        var cr = rt.rect;
        var norm = IsPortrait(rt) ? UIConstants.EditorSafeAreaPortrait
                                  : UIConstants.EditorSafeAreaLandscape;

        var localSafe = new Rect(
            cr.xMin + cr.width * norm.x,
            cr.yMin + cr.height * norm.y,
            cr.width * norm.width,
            cr.height * norm.height
        );

        var safe = new Vector3[4]
        {
            rt.TransformPoint(new Vector3(localSafe.xMin, localSafe.yMin, 0)),
            rt.TransformPoint(new Vector3(localSafe.xMin, localSafe.yMax, 0)),
            rt.TransformPoint(new Vector3(localSafe.xMax, localSafe.yMax, 0)),
            rt.TransformPoint(new Vector3(localSafe.xMax, localSafe.yMin, 0)),
        };

        Handles.color = Color.yellow;
        DrawPolyRect(safe);
        Handles.color = prevCol;

        // --- ラベル ---
        if (s_showLabel)
        {
            Handles.BeginGUI();
            var p = HandleUtility.WorldToGUIPoint(worldCorners[1]); // 左上
            GUI.Label(new Rect(p.x + 6, p.y + 6, 520, 18),
                $"CanvasRect:{(int)cr.width}x{(int)cr.height}  Safe(norm):{norm.x:0.##},{norm.y:0.##},{norm.width:0.##},{norm.height:0.##}",
                EditorStyles.miniLabel);
            Handles.EndGUI();
        }

        // --- ツールバー ---
        DrawToolbar(sv, true, rt.TransformPoint(cr.center), new Vector2(cr.width, cr.height));
    }

    private static void DrawPolyRect(Vector3[] c)
    {
        Handles.DrawAAPolyLine(2f, c[0], c[1]);
        Handles.DrawAAPolyLine(2f, c[1], c[2]);
        Handles.DrawAAPolyLine(2f, c[2], c[3]);
        Handles.DrawAAPolyLine(2f, c[3], c[0]);
    }

    // ====== ツールバー（左上／最小化ボタンは左上行、ボタン列は2行目） ======
    private static void DrawToolbar(SceneView sv, bool hasTarget, Vector3 worldCenter, Vector2 canvasSize)
    {
        Handles.BeginGUI();

        // コンパクトなパネルスタイル（上寄せ）
        var panelStyle = new GUIStyle(GUI.skin.window)
        {
            padding = new RectOffset(6, 6, 2, 10) // 上下左右の隙間
        };

        // 2行目のボタン実サイズ（計測は GUI.skin.button ベース）
        float btnH = Mathf.Ceil(EditorGUIUtility.singleLineHeight) + 4f;
        var resetContent = new GUIContent("Reset Camera (Fit)");
        var labelContent = new GUIContent(s_showLabel ? "Labels: On" : "Labels: Off");
        float btnResetW = GUI.skin.button.CalcSize(resetContent).x;
        float btnLblsW = GUI.skin.button.CalcSize(labelContent).x;

        // 内側コンテンツのちょうどよい幅（左右パディングは別）
        float row2InnerW = Mathf.Ceil(btnResetW) + kBtnSpace + Mathf.Ceil(btnLblsW);

        // パネルの幅・高さ（最小化時は最小限）
        float panelWExpanded =
            panelStyle.padding.left + kPad + row2InnerW + kPad + panelStyle.padding.right; // = 左Pad + 内側 + 右Pad
        float panelWMinimized =
            panelStyle.padding.left + 6f + kMiniBtnSize + 6f + panelStyle.padding.right;     // 最小化はボタンだけ

        float panelW = s_minimized ? panelWMinimized : Mathf.Max(160f, panelWExpanded);

        float headerH = kHeaderH;
        float bodyH = s_minimized ? 0f : btnH;
        float panelH = headerH + bodyH + panelStyle.padding.top + panelStyle.padding.bottom;

        // 左上固定（切れ防止・画面端クランプ）
        float x = kMargin, y = kMargin;
        float maxW = Mathf.Max(120f, sv.position.width - kMargin * 2f);
        panelW = Mathf.Min(panelW, maxW);

        var area = new Rect(x, y, panelW, panelH);
        GUILayout.BeginArea(area, GUIContent.none, panelStyle);
        {
            // === 1行目：最小化ボタン（左上） ===
            using (new GUILayout.HorizontalScope(GUILayout.Height(headerH)))
            {
                if (GUILayout.Button(s_minimized ? "⤢" : "—",
                        GUILayout.Width(kMiniBtnSize), GUILayout.Height(kMiniBtnSize)))
                {
                    ToggleMin();
                }
                GUILayout.FlexibleSpace();
            }

            // === 2行目：ボタン群（最小化時は非表示） ===
            if (!s_minimized)
            {
                using (new GUILayout.HorizontalScope(GUILayout.Height(btnH)))
                {
                    GUILayout.Space(kPad);

                    using (new EditorGUI.DisabledScope(!hasTarget))
                    {
                        if (GUILayout.Button(resetContent, GUILayout.Height(btnH), GUILayout.Width(btnResetW)))
                            ResetCameraFitToCanvas(worldCenter, canvasSize);
                    }

                    GUILayout.Space(kBtnSpace);

                    if (GUILayout.Button(labelContent, GUILayout.Height(btnH), GUILayout.Width(btnLblsW)))
                    {
                        s_showLabel = !s_showLabel;
                        EditorPrefs.SetBool(KEY_LABELS, s_showLabel);
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Space(kPad);
                }
            }
        }
        GUILayout.EndArea();

        Handles.EndGUI();
    }

    private static void ToggleMin()
    {
        s_minimized = !s_minimized;
        SceneView.RepaintAll();
    }

    // ====== カメラを“外枠ほぼ一杯”にフィット＆中心へ ======
    private static void ResetCameraFitToCanvas(Vector3 worldCenter, Vector2 canvasSize)
    {
        var sv = SceneView.lastActiveSceneView;
        if (!sv) return;

        sv.in2DMode = true;
        sv.orthographic = true;
        sv.rotation = Quaternion.identity; // 正面固定
        sv.pivot = worldCenter;         // 中心へ

        // SceneViewウィンドウの実アスペクトから必要 size を算出（size は垂直半径）
        var view = sv.position;
        float viewW = Mathf.Max(1f, view.width - 2f * kMargin);
        float viewH = Mathf.Max(1f, view.height - 2f * kMargin);
        float aspect = viewW / viewH;

        float halfH = canvasSize.y * 0.5f;
        float halfW = canvasSize.x * 0.5f;

        float sizeByH = halfH;                              // 縦で収める必要サイズ
        float sizeByW = halfW / Mathf.Max(0.0001f, aspect); // 横で収める必要サイズ

        float fit = Mathf.Max(sizeByH, sizeByW);
        sv.size = Mathf.Max(1f, fit * kFitMarginFactor); // 0.4 でグッと寄せる
        sv.Repaint();
    }

    // ====== Utilities ======
    private static Canvas FindRootCanvasUI()
    {
        const int order = UIConstants.CanvasSortOrder.UI;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // 最優先：root && sortingOrder == UI
        var c1 = canvases.FirstOrDefault(c => c && c.isRootCanvas && c.sortingOrder == order);
        if (c1) return c1.rootCanvas;

        // 次点：選択の親が rootCanvas
        var active = Selection.activeGameObject;
        if (active)
        {
            var cInParents = active.GetComponentInParent<Canvas>(true);
            if (cInParents && cInParents.isRootCanvas) return cInParents.rootCanvas;
        }
        return null;
    }

    private static bool IsPortrait(RectTransform rt)
    {
        var r = rt.rect;
        return r.height >= r.width;
    }
}
#endif
