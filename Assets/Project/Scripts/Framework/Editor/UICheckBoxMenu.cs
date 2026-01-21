#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public static class UICheckBoxMenu
{
    private const string MenuWithLabel = UIConstants.UIExtensionsMenuPath + "UICheckBox（TMP）";
    private const string MenuWithoutLabel = UIConstants.UIExtensionsMenuPath + "UICheckBox (No Label)";

    [MenuItem(MenuWithLabel, false, EditorConstants.MenuItemSortOrder.UICheckBoxTMP)]
    public static void CreateWithLabel(MenuCommand menuCommand)
    {
        CreateInternal(menuCommand, withLabel: true);
    }

    [MenuItem(MenuWithoutLabel, false, EditorConstants.MenuItemSortOrder.UICheckBoxNoLabel)]
    public static void CreateWithoutLabel(MenuCommand menuCommand)
    {
        CreateInternal(menuCommand, withLabel: false);
    }

    private static void CreateInternal(MenuCommand menuCommand, bool withLabel)
    {
        // 親は「選択中」or 右クリックコンテキスト
        var stage = StageUtility.GetCurrentStageHandle();
        var parent = (menuCommand.context as GameObject) ?? Selection.activeGameObject;

        // ステージ不一致や未選択は中断
        if (parent == null || StageUtility.GetStageHandle(parent) != stage)
        {
            EditorUtility.DisplayDialog(
                "Create UI CheckBox",
                "子として作成する親オブジェクトを選択してください（同一ステージ上）。",
                "OK");
            return;
        }

        // Root: UICheckBox
        var go = new GameObject("UICheckBox", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create UICheckBox");
        StageUtility.PlaceGameObjectInCurrentStage(go);
        GameObjectUtility.SetParentAndAlign(go, parent);

        var rootRect = go.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(200f, 40f);

        // UICheckBox 本体
        var checkBox = go.AddComponent<UICheckBox>();
        checkBox.isOn = true;

        // ==== BG ====
        var bgGO = new GameObject("BG", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(bgGO, "Create UICheckBox BG");
        GameObjectUtility.SetParentAndAlign(bgGO, go);

        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(0f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(24f, 24f);
        bgRect.anchoredPosition = new Vector2(16f, 0f);

        var bgImage = bgGO.AddComponent<Image>();
        bgImage.raycastTarget = true;

        // デフォルトの Baclground スプライトを割り当てる
        var bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bgImage.sprite = bgSprite;
        bgImage.type = Image.Type.Simple;

        // ==== Check ====
        var checkGO = new GameObject("Check", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(checkGO, "Create UICheckBox Check");
        GameObjectUtility.SetParentAndAlign(checkGO, bgGO);

        var checkRect = checkGO.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(20f, 20f);
        checkRect.anchoredPosition = Vector2.zero;

        var checkImage = checkGO.AddComponent<Image>();
        checkImage.raycastTarget = false; // ヒットは BG 側で拾う想定

        // デフォルトの Checkmark スプライトを割り当てる
        var checkSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        checkImage.sprite = checkSprite;
        checkImage.type = Image.Type.Simple;


        // Toggle の参照をセット
        checkBox.targetGraphic = bgImage;
        checkBox.graphic = checkImage;

        // ==== Label（任意） ====
        if (withLabel)
        {
            var labelGO = new GameObject("Label", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(labelGO, "Create UICheckBox Label");
            GameObjectUtility.SetParentAndAlign(labelGO, go);

            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            // 左に BG + 余白ぶんあける
            labelRect.offsetMin = new Vector2(40f, 0f);
            labelRect.offsetMax = new Vector2(0f, 0f);

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "UICheckBox";
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.fontSize = 24f;

            // LocalizeStringEvent 初期配線
            var lse = labelGO.AddComponent<LocalizeStringEvent>();
            LSEWireUtil.SetLocalizeString(lse, UIConstants.StringDefaultGroupName, UIConstants.StringDefaultGroupKey);

            UnityAction<string> act = tmp.SetText;
            UnityEventTools.AddPersistentListener(lse.OnUpdateString, act);
        }

        Selection.activeGameObject = go;
    }
}
#endif
