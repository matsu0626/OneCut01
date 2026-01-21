#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public static class UIButtonMenu
{
    private const string MenuWithLabel = UIConstants.UIExtensionsMenuPath + "UIButton（TMP）";
    private const string MenuWithoutLabel = UIConstants.UIExtensionsMenuPath + "UIButton (No Label)";


    [MenuItem(MenuWithLabel, false, EditorConstants.MenuItemSortOrder.UIButtonTMP)]
    private static void CreateUIButtonTMP(MenuCommand cmd) => CreateUIButtonInternal(cmd, withTMP: true);

    [MenuItem(MenuWithoutLabel, false, EditorConstants.MenuItemSortOrder.UIButtonNoLabel)]
    private static void CreateUIButtonNoLabel(MenuCommand cmd) => CreateUIButtonInternal(cmd, withTMP: false);

    private static void CreateUIButtonInternal(MenuCommand menuCommand, bool withTMP)
    {
        // 親は「選択中のオブジェクト」or 右クリックしたコンテキスト
        var stage = StageUtility.GetCurrentStageHandle();
        var parent = (menuCommand.context as GameObject) ?? Selection.activeGameObject;

        // ステージ不一致や未選択は中断
        if (parent == null || StageUtility.GetStageHandle(parent) != stage)
        {
            EditorUtility.DisplayDialog("Create UI Button",
                "子として作成する親オブジェクトを選択してください（同一ステージ上）。", "OK");
            return;
        }

        // 子に RectTransform を作る（親は Transform でも RectTransform でもOK）
        var go = new GameObject(withTMP ? "UIButton_TMP" : "UIButton", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create UI Button");
        StageUtility.PlaceGameObjectInCurrentStage(go);

        GameObjectUtility.SetParentAndAlign(go, parent);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 40);

        var img = go.AddComponent<Image>();
        img.raycastTarget = true;

        go.AddComponent<UIButton>();

        if (withTMP)
        {
            // ラベル（フルストレッチ）
            var labelGO = new GameObject("Label", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(labelGO, "Create Label");
            StageUtility.PlaceGameObjectInCurrentStage(labelGO);
            GameObjectUtility.SetParentAndAlign(labelGO, go);

            var lr = labelGO.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Button";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // LocalizeStringEvent 初期配線
            var lse = labelGO.AddComponent<LocalizeStringEvent>();
            LSEWireUtil.SetLocalizeString(lse, UIConstants.StringDefaultGroupName, UIConstants.StringDefaultGroupKey);

            UnityAction<string> act = tmp.SetText;
            UnityEventTools.AddPersistentListener(lse.OnUpdateString, act);
        }

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }
}
#endif
