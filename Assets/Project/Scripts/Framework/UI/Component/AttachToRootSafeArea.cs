using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AttachToRootSafeArea : MonoBehaviour
{
    // セーフエリアへのアタッチ完了フラグ
    private bool m_attached;
    private UniTaskCompletionSource m_tcs;

    /// <summary>
    /// セーフエリアへの親付けが完了するまで待つ。
    /// 既に完了していれば即時完了。
    /// </summary>
    public UniTask WaitUntilAttachedAsync(CancellationToken token = default)
    {
        if (m_attached) return UniTask.CompletedTask;

        m_tcs ??= new UniTaskCompletionSource();
        return m_tcs.Task.AttachExternalCancellation(token);
    }

    private void OnEnable()
    {
        // 自動でアタッチを開始
        if (!m_attached)
        {
            var token = this.GetCancellationTokenOnDestroy();
            AttachAsync(token).Forget();
        }
    }

    private async UniTaskVoid AttachAsync(CancellationToken token)
    {
        if (m_attached) return;

        var rt = transform as RectTransform;
        if (!rt)
        {
            CompleteAttach();
            return;
        }

        // Canvas / SafeArea が揃うまで 1フレーム待つ
        await UniTask.Yield();                // 単に次フレームまで待つ
        token.ThrowIfCancellationRequested(); // ここでキャンセルを反映

        var canvas = ResolveRootCanvas();
        if (!canvas)
        {
            CompleteAttach();
            return;
        }

        var safe = ResolveSafeArea(canvas.transform);
        if (!safe)
        {
            // SafeArea が無い場合は Canvas の RectTransform を使う
            safe = canvas.transform as RectTransform;
        }

        if (!safe)
        {
            CompleteAttach();
            return;
        }

        AppDebug.Log($"[AttachToRootSafeArea] before: anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}, pos={rt.anchoredPosition}");


        // ---- ここがポイント ----
        // Anchor / Pivot / sizeDelta / anchoredPosition は一切いじらず、
        // 単に SafeArea の子に付け替えるだけにする。
        // プレハブ側で 0.5,0.5 中央アンカー＋Pos を決めておけば、
        // そのまま SafeArea 中央基準で配置される。
        rt.SetParent(safe, worldPositionStays: false);


        AppDebug.Log($"[AttachToRootSafeArea] after: anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}, pos={rt.anchoredPosition}");



        // 回転・スケールもプレハブの値をそのまま使いたいので、ここでは触らない。
        // 必要になったらここで rt.localScale / localRotation を揃える。

        CompleteAttach();
    }

    private void CompleteAttach()
    {
        m_attached = true;
        m_tcs?.TrySetResult();
    }

    // ルート Canvas を探す（UI 用ソートオーダーのもの）
    private static Canvas ResolveRootCanvas()
    {
        const int order = UIConstants.CanvasSortOrder.UI;
        var all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c && c.isRootCanvas && c.sortingOrder == order)
                return c.rootCanvas;
        }
        return null;
    }

    // ルート配下の SafeArea を探す
    private static RectTransform ResolveSafeArea(Transform root)
    {
        var t = root.Find(UIConstants.SafeAreaName);
        return t ? (RectTransform)t : null;
    }
}
