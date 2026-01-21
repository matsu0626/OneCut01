#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SafeAreaTools
{
    private const string MenuName = UIConstants.UIExtensionsMenuPath + "Move Selected To SafeArea";

    [MenuItem(MenuName, false, EditorConstants.MenuItemSortOrder.MoveToSafeArea)]
    public static void MoveSelectedToSafeArea()
    {
        var go = Selection.activeGameObject;
        if (!go) { EditorUtility.DisplayDialog("SafeArea", "オブジェクトを選択してください。", "OK"); return; }

        // 親 Canvas を探す（選択が Canvas 自身でもOK）
        var canvas = go.GetComponentInParent<Canvas>(true);
        if (!canvas || !canvas.isRootCanvas)
        {
            EditorUtility.DisplayDialog("SafeArea", "ルート Canvas 配下で実行してください。", "OK");
            return;
        }

        // SafeArea を取得/作成
        var safe = canvas.transform.Find("SafeArea") as RectTransform;
        if (!safe)
        {
            var sa = new GameObject("SafeArea", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(sa, "Create SafeArea");
            safe = sa.transform as RectTransform;
            safe.SetParent(canvas.transform, false);
            // フルストレッチ（SafeAreaFitter が付いていれば後で狭まる）
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
            safe.offsetMin = Vector2.zero; safe.offsetMax = Vector2.zero;
        }

        // 移動
        Undo.SetTransformParent(go.transform, safe, "Move To SafeArea");
        go.transform.SetAsLastSibling();
        EditorGUIUtility.PingObject(go);
    }
}
#endif
