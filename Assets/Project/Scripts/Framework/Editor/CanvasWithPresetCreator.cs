#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/*
●通常UIのプレハブ構造
 ・Canvas(Preset)             ルートキャンバス
    ├─ BG [Image]           BG
    └─ SafeArea             セーフエリア    
       └─ UIオブジェクト    
             Image/TMP/CustomBtton/共通UIプレハブなど
                ：                
                ：
*/
public static class CanvasWithPresetCreator
{
    private const string MenuCreateNoES = UIConstants.UIExtensionsMenuPath + "Canvas (Preset)";
    private const string MenuCreateWithES = UIConstants.UIExtensionsMenuPath + "Canvas (Preset + EventSystem)";
    private const string MenuAttachPreset = UIConstants.UIExtensionsMenuPath + "Add Preset to Selected Canvas";

    [MenuItem(MenuCreateNoES, false, EditorConstants.MenuItemSortOrder.CanvasWithPreset)]
    public static void CreateCanvasWithPreset_NoEventSystem(MenuCommand cmd)
        => CreateCanvasWithPresetImpl(cmd, makeEventSystem: false);

    [MenuItem(MenuCreateWithES, false, EditorConstants.MenuItemSortOrder.CanvasWithPresetES)]
    public static void CreateCanvasWithPreset_WithEventSystem(MenuCommand cmd)
        => CreateCanvasWithPresetImpl(cmd, makeEventSystem: true);

    private static void CreateCanvasWithPresetImpl(MenuCommand cmd, bool makeEventSystem)
    {
        var stage = StageUtility.GetCurrentStageHandle();

        var go = new GameObject("Canvas (Preset)");
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas (Preset)");
        StageUtility.PlaceGameObjectInCurrentStage(go);

        var parent = cmd.context as GameObject;
        if (parent == null)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null) parent = prefabStage.prefabContentsRoot;
        }
        if (parent != null) GameObjectUtility.SetParentAndAlign(go, parent);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(UIConstants.ScreenWidth, UIConstants.ScreenHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = UIConstants.MatchWidthOrHeight;

        go.AddComponent<GraphicRaycaster>();

        var preset = go.AddComponent<CanvasSortOrderPreset>();
        // オフセット未指定の SetSortOrderByConst がある想定
        preset.SetSortOrderByConst(UIConstants.CanvasSortOrder.UI);
        preset.SendMessage("Apply", SendMessageOptions.DontRequireReceiver);

        // SafeArea
        var safeArea = EnsureSafeArea(go.transform);

        if (makeEventSystem)
            EnsureEventSystemInStage(stage);

        // SafeArea を選択状態に（編集しやすく）
        Selection.activeObject = safeArea != null ? safeArea.gameObject : go;
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    [MenuItem(MenuAttachPreset, false, EditorConstants.MenuItemSortOrder.AddSortOrderToCanvas)]
    public static void AttachPresetToSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Attach Preset", "Canvas を選択してください。", "OK");
            return;
        }

        var canvas = go.GetComponent<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Attach Preset", "選択に Canvas がありません。", "OK");
            return;
        }

        var preset = go.GetComponent<CanvasSortOrderPreset>();
        if (preset == null) preset = Undo.AddComponent<CanvasSortOrderPreset>(go);
        preset.SetSortOrderByConst(UIConstants.CanvasSortOrder.UI);
        preset.SendMessage("Apply", SendMessageOptions.DontRequireReceiver);

        // SafeArea
        var safeArea = EnsureSafeArea(go.transform);

        EditorUtility.SetDirty(go);
        Selection.activeObject = safeArea != null ? safeArea.gameObject : go;
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    // ===== SafeArea 生成/再利用 =====
    private static RectTransform EnsureSafeArea(Transform canvasTransform)
    {
        if (canvasTransform == null) return null;

        // 既存を再利用
        var rt = canvasTransform.Find("SafeArea") as RectTransform;
        if (rt == null)
        {
            var sa = new GameObject("SafeArea", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(sa, "Create SafeArea");
            rt = (RectTransform)sa.transform;
            rt.SetParent(canvasTransform, false);
        }

        // フルストレッチ
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        // SafeAreaFitter を必ず付与
        if (rt.GetComponent<SafeAreaFitter>() == null)
            Undo.AddComponent<SafeAreaFitter>(rt.gameObject);

        return rt;
    }

    // ===== ステージ安全な EventSystem 生成 =====
    private static void EnsureEventSystemInStage(StageHandle stage)
    {
        var systems = GetComponentsInStage<EventSystem>(stage);
        if (systems.Count > 0) return;

        var es = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        StageUtility.PlaceGameObjectInCurrentStage(es);
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>(); // 新Inputなら InputSystemUIInputModule に差し替え可
    }

    private static List<T> GetComponentsInStage<T>(StageHandle stage) where T : Component
    {
        var list = new List<T>();
        var all = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (var c in all)
            if (StageUtility.GetStageHandle(c.gameObject) == stage)
                list.Add(c);
        return list;
    }
}
#endif
