#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using static EditorConstants;

public static class CreateLocalizedTMP
{
    private const string MenuPath = "GameObject/UI/Text - TextMeshPro (Localized)";

    [MenuItem(MenuPath, false, MenuItemSortOrder.TMPLocalized)] // 既存の"Text - TextMeshPro"の直下くらいに出す
    public static void Create()
    {
        // 1) 置き場所（Canvas）を用意
        var parent = UIEditorUtil.GetBestUiParent();

        // 2) 本体（TMP + LSE）を作成
        var go = new GameObject("Text (Localized TMP)", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LocalizeStringEvent));
        Undo.RegisterCreatedObjectUndo(go, "Create Localized TMP");

        // 3) 親子付け＆レイアウトは「普通のTMPに準拠」寄り（最小限）
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(200f, 50f); // デフォっぽいサイズ
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        // 4) TMP 初期値（既存のTMPと近い感じ）
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = "New Text";
        tmp.fontSize = 36f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = true;

        // 5) LSE を TMP に紐付け（OnUpdateString => tmp.SetText）
        var lse = go.GetComponent<LocalizeStringEvent>();
        UnityEventTools.AddPersistentListener(lse.OnUpdateString, new UnityAction<string>(tmp.SetText));
        LSEWireUtil.SetLocalizeString(lse, UIConstants.StringDefaultGroupName, UIConstants.StringDefaultGroupKey);

        // 6) 選択してフォーカス
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    // 既存の選択から親を推測。UIの下ならそこ、なければ null
    private static Transform GetBestParentTransform()
    {
        var active = Selection.activeTransform;
        if (active == null) return null;

        // 選択がRectTransform系UIならその下に
        if (active.GetComponentInParent<Canvas>() != null)
            return active;

        // そうでなければ最寄りのCanvasの直下
        var canvas = active.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.transform : null;
    }

    // 既存のTMPに LSEだけ後付けしたい用
    [MenuItem("GameObject/UI/Add LocalizeStringEvent to Selected TMP", false, MenuItemSortOrder.AddLseToTMP)]
    public static void AddToSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null) return;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            EditorUtility.DisplayDialog("Add LSE", "選択オブジェクトに TextMeshProUGUI がありません。", "OK");
            return;
        }

        var lse = go.GetComponent<LocalizeStringEvent>();
        if (lse == null) lse = Undo.AddComponent<LocalizeStringEvent>(go);

        // 二重登録防止（同じメソッドが既に登録されていたらスキップ）
        bool hasListener = false;
        var ev = lse.OnUpdateString;
        for (int i = 0; i < ev.GetPersistentEventCount(); i++)
        {
            if (ev.GetPersistentTarget(i) == (Object)tmp &&
                ev.GetPersistentMethodName(i) == nameof(TextMeshProUGUI.SetText))
            {
                hasListener = true;
                break;
            }
        }
        if (!hasListener)
        {
            UnityEventTools.AddPersistentListener(ev, new UnityAction<string>(tmp.SetText));
        }
            
               
        LSEWireUtil.SetLocalizeString(lse, UIConstants.StringDefaultGroupName, UIConstants.StringDefaultGroupKey);

        EditorUtility.SetDirty(go);
    }
}
#endif
