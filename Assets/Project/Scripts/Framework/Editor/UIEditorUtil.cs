#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public static class UIEditorUtil
{
    /// <summary>
    /// 置き場所として最適なUI親Transformを返す。
    /// 1) 選択中がUI(=Canvas配下)ならそのTransform
    /// 2) そうでなければ、最寄りのCanvas
    /// 3) 見つからなければCanvas(+EventSystem)を新規作成してそのtransform
    /// </summary>
    public static Transform GetBestUiParent(bool createCanvasIfMissing = true)
    {
        var active = Selection.activeTransform;

        // Prefab編集モード対応（Prefab Stage内の選択を優先）
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null && active == null)
        {
            active = prefabStage.prefabContentsRoot != null
                ? prefabStage.prefabContentsRoot.transform
                : null;
        }

        // 1) 選択がUI配下ならそこ
        if (active != null && active.GetComponentInParent<Canvas>() != null)
            return active;

        // 2) 最寄りのCanvas直下
        var canvas = active != null ? active.GetComponentInParent<Canvas>() : Object.FindFirstObjectByType<Canvas>();
        if (canvas != null) return canvas.transform;

        // 3) 無ければ作成
        if (!createCanvasIfMissing) return null;
        return CreateCanvasWithEventSystem().transform;
    }

    /// <summary>Canvasが無ければ作成（ScreenSpaceOverlay）＋EventSystemも補完。</summary>
    public static Canvas EnsureCanvasAndEventSystem()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) canvas = CreateCanvasWithEventSystem();
        else EnsureEventSystem();
        return canvas;
    }

    /// <summary>EventSystemが無ければ作成。</summary>
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    /// <summary>新規Canvasを作成して返す（便利プリセット付き）。</summary>
    public static Canvas CreateCanvasWithEventSystem()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();
        return canvas;
    }

    /// <summary>Panel等を親にフルフィットで配置（9slice背景とかに便利）。</summary>
    public static void StretchToParent(RectTransform rt, Transform parent, Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        if (rt == null || parent is not RectTransform parentRt) return;
        rt.SetParent(parentRt, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin ?? Vector2.zero;
        rt.offsetMax = offsetMax ?? Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
#endif
