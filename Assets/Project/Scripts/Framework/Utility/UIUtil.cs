using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public static class UIUtil
{
    /// <summary>
    /// Imageを全画面にフィットさせる
    /// </summary>
    /// <param name="img"></param>
    public static void FitFullScreen(Image img)
    {
        if (!img) return;

        // 1) this
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        // 2) parents（Canvas 直下まで全員ストレッチ）
        var p = rt.parent as RectTransform;
        while (p != null && p.GetComponent<Canvas>() == null)
        {
            if (p.anchorMin != Vector2.zero || p.anchorMax != Vector2.one ||
                p.offsetMin != Vector2.zero || p.offsetMax != Vector2.zero)
            {
                p.anchorMin = Vector2.zero;
                p.anchorMax = Vector2.one;
                p.offsetMin = Vector2.zero;
                p.offsetMax = Vector2.zero;
                p.pivot = new Vector2(0.5f, 0.5f);
            }
            p = p.parent as RectTransform;
        }

        // 3) Image 設定
        img.preserveAspect = false; // ここが ON だと左右や上下に隙間が出る
        img.raycastTarget = true;  // 背面ブロックしたいなら true
        img.type = Image.Type.Simple; // スプライト貼ってるなら Simple 推奨
    }

    /// <summary>
    /// 親のtransformに全面フィットさせる
    /// 全画面ダイアログ、モーダルのフェード背景、親領域いっぱいに広げたいパネルやスクリーンUI などの時に使用
    /// ※rt=UIオブジェクトのtransformであること
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parent"></param>
    public static void StretchToParentRectTransform(RectTransform rt, Transform parent)
    {
        if (rt == null || !(parent is RectTransform)) return;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    /// <summary>
    /// X方向のみ中央寄せ。Y方向（アンカー/ピボット/座標）は変更しない。
    /// </summary>
    /// <param name="rt">対象 RectTransform</param>
    /// <param name="setPivotX">pivot.x も 0.5 に揃えるか</param>
    public static void CenterHorizontally(RectTransform rt, bool setPivotX = true)
    {
        if (!rt) return;

        // 横アンカー中央
        var aMin = rt.anchorMin;
        var aMax = rt.anchorMax;
        aMin.x = 0.5f; aMax.x = 0.5f;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;

        // 必要なら横ピボット中央
        if (setPivotX)
        {
            var pv = rt.pivot;
            pv.x = 0.5f;
            rt.pivot = pv;
        }

        // Xを0に（Yは触らない）
        var pos = rt.anchoredPosition;
        pos.x = 0f;
        rt.anchoredPosition = pos;
    }


}
