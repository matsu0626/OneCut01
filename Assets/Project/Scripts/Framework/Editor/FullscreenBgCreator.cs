#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FullscreenBgCreator
{
    private const string MenuName = UIConstants.UIExtensionsMenuPath + "Fullscreen BG (Image)";

    [MenuItem(MenuName, false, EditorConstants.MenuItemSortOrder.FullscreenBG)]
    public static void CreateFullscreenBgBlocking(MenuCommand cmd)
    {
        // 1) 対象 Canvas を必ず選ばせる
        var ctx = cmd.context as GameObject ?? Selection.activeGameObject;
        if (ctx == null)
        {
            EditorUtility.DisplayDialog("Fullscreen BG", "Canvas を選択してから実行してください。", "OK");
            return;
        }

        var canvas = ctx.GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Fullscreen BG", "選択オブジェクトの親階層に Canvas が見つかりません。Canvas を選択してから実行してください。", "OK");
            return;
        }

        // Prefab モード中なら、現在の Prefab ステージのルートが親かどうか軽くチェック
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            // 選択 Canvas が現在のステージ外だと生成が見えないため、注意喚起
            var inThisStage = canvas.gameObject.scene == prefabStage.scene;
            if (!inThisStage)
            {
                EditorUtility.DisplayDialog(
                    "Fullscreen BG",
                    "現在の Prefab ステージ外の Canvas が選択されています。\n" +
                    "Prefab モードで編集する Canvas を選択し直してください。",
                    "OK"
                );
                return;
            }
        }

        // 2) BG を生成して選択 Canvas の子に配置
        var go = new GameObject("BG", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create Fullscreen BG");
        GameObjectUtility.SetParentAndAlign(go, canvas.gameObject);

        // 3) Image 設定（常にブロッキング）
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 1f);
        img.raycastTarget = true;

        // 4) 全画面フィット
        UIUtil.FitFullScreen(img);
              

        Selection.activeObject = go;
        EditorGUIUtility.PingObject(go);
    }
}
#endif
