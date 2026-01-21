#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UIScrollTextViewMenu
{
    // 自前拡張は Extensions 配下に置くルール
    private const string MenuPath = UIConstants.UIExtensionsMenuPath + "UIScrollTextView (TMP)";

    [MenuItem(MenuPath, false, EditorConstants.MenuItemSortOrder.ScrollTextView)]
    public static void Create(MenuCommand menuCommand)
    {
        CreateInternal(menuCommand);
    }

    private static void CreateInternal(MenuCommand menuCommand)
    {
        // 親は「選択中のオブジェクト」or 右クリックしたコンテキスト
        var stage = StageUtility.GetCurrentStageHandle();
        var parent = (menuCommand.context as GameObject) ?? Selection.activeGameObject;

        // ステージ不一致や未選択は中断
        if (parent == null || StageUtility.GetStageHandle(parent) != stage)
        {
            EditorUtility.DisplayDialog(
                "Create UIScrollTextView",
                "子として作成する親オブジェクトを選択してください（同一ステージ上）。",
                "OK");
            return;
        }

        // ルート作成
        var root = new GameObject("UIScrollTextView", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Create UIScrollTextView");
        StageUtility.PlaceGameObjectInCurrentStage(root);
        GameObjectUtility.SetParentAndAlign(root, parent);

        var rootRect = root.GetComponent<RectTransform>();
        // 親いっぱいにフィット
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;

        // 背景（任意・半透明）
        var bgImage = root.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.3f);

        // ScrollRect
        var scrollRect = root.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;

        // Viewport
        var viewportGO = new GameObject("Viewport", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(viewportGO, "Create UIScrollTextView Viewport");
        GameObjectUtility.SetParentAndAlign(viewportGO, root);

        var viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.sizeDelta = Vector2.zero;

        var viewportImage = viewportGO.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f); // 完全透明
        viewportImage.raycastTarget = true;

        viewportGO.AddComponent<RectMask2D>();

        scrollRect.viewport = viewportRect;

        // Content
        var contentGO = new GameObject("Content", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(contentGO, "Create UIScrollTextView Content");
        GameObjectUtility.SetParentAndAlign(contentGO, viewportGO);

        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        scrollRect.content = contentRect;

        // Text
        var textGO = new GameObject("Text", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(textGO, "Create UIScrollTextView Text");
        GameObjectUtility.SetParentAndAlign(textGO, contentGO);

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "UIScrollTextView";
        tmp.fontSize = 20f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        // 旧 enableWordWrapping ではなく textWrappingMode を使う方針
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = true;

        // UIScrollTextView コンポーネントをアタッチし、参照を配線
        var scrollTextView = root.AddComponent<UIScrollTextView>();
        var so = new SerializedObject(scrollTextView);
        so.FindProperty("m_scrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("m_text").objectReferenceValue = tmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 最後に選択
        Selection.activeGameObject = root;

        // シーンを Dirty に
        EditorSceneManager.MarkSceneDirty(root.scene);
    }
}
#endif
