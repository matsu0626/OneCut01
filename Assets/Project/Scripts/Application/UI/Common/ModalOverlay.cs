using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ModalOverlay : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string m_seFileName = "se_02";
    [SerializeField] private Color m_overlayColor = new Color(0f, 0f, 0f, 0.8f);

    // ルート SafeArea ごとに1枚
    private static readonly Dictionary<Transform, ModalOverlay> s_pool = new();

    // このインスタンスのキー（= ルート SafeArea）
    [SerializeField] private Transform m_keyParent;

    // 実体
    [SerializeField] private RectTransform m_rect;
    [SerializeField] private Image m_fullscreenImg;

    // ダイアログのスタック（最上位 = 末尾）
    private readonly List<Transform> m_dialogs = new();
    private readonly List<Action> m_onCloseStack = new();

    private int m_stack;
    private Action m_onCloseRequested;

    /// <summary>
    /// ダイアログ(もしくはその子)から、同一ルートCanvas配下の SafeArea を特定し、そこをキーに取得/生成。
    /// </summary>
    public static ModalOverlay GetOrCreate(Transform dialogOrChild)
    {
        AppDebug.Assert(dialogOrChild != null, "[ModalOverlay] dialogOrChild is null.");

        var safeArea = ResolveRootSafeArea(dialogOrChild);
        AppDebug.Assert(safeArea != null, "[ModalOverlay] Root SafeArea not found.");

        if (!s_pool.TryGetValue(safeArea, out var overlay) || overlay == null)
        {
            overlay = CreateUnder(safeArea);
            s_pool[safeArea] = overlay;
        }
        return overlay;
    }

    /// <summary>
    /// 表示要求：ダイアログの最上位ノード直下に自分を差し込み、背面ブロック開始。
    /// </summary>
    public OverlayHandle Push(Transform dialog, Action onCloseRequested)
    {
        AppDebug.Assert(dialog != null);

        var parent = dialog.parent;
        AppDebug.Assert(parent != null, "[ModalOverlay] dialog に親がありません。");

        // 親が違っていたら付け替え（キーも更新）
        if (transform.parent != parent)
        {
            UnregisterFromPool();
            transform.SetParent(parent, false);
            RegisterToPool(parent);
        }

        // 既存スタックをクリーン（親が変わった/破棄されたダイアログを除去）
        CleanupDialogStack(parent);

        // dialog の直下になるよう配置
        PlaceDirectlyBelow(dialog);

        // スタックへ積む
        if (!m_dialogs.Contains(dialog))
        {
            m_dialogs.Add(dialog);
            m_onCloseStack.Add(onCloseRequested ?? (() => { }));
        }

        // 見た目&ブロック更新
        m_fullscreenImg.color = m_overlayColor;
        UIUtil.StretchToParentRectTransform(m_rect, parent);
        m_fullscreenImg.raycastTarget = true;

        m_stack = m_dialogs.Count; // 同期
        m_onCloseRequested = onCloseRequested;

        gameObject.SetActive(true);
        return new OverlayHandle(this, dialog);
    }

    private void PlaceDirectlyBelow(Transform dialog)
    {
        // ① overlay を dialog の現在 index に差し込む
        var di = dialog.GetSiblingIndex();
        transform.SetSiblingIndex(di);

        // ② dialog を overlay の直後へ（= overlay 直上に固定）
        var oi = transform.GetSiblingIndex();
        dialog.SetSiblingIndex(oi + 1);
    }

    // dialog を指定して Pop
    private void Pop(Transform dialog)
    {
        CleanupDialogStack(transform.parent);

        // 末尾前提だが、安全のため index を探す
        var idx = m_dialogs.LastIndexOf(dialog);
        if (idx >= 0)
        {
            m_dialogs.RemoveAt(idx);
            m_onCloseStack.RemoveAt(idx);
        }

        if (m_dialogs.Count > 0)
        {
            var top = m_dialogs[m_dialogs.Count - 1];
            if (top != null && top.parent == transform.parent)
            {
                PlaceDirectlyBelow(top);
            }
            // コールバックを最新に差し替え
            m_onCloseRequested = m_onCloseStack[m_onCloseStack.Count - 1];
            return;
        }

        // スタック空：完全に無効化
        m_onCloseRequested = null;
        m_fullscreenImg.raycastTarget = false; // ← これ重要（ブロック解除）
        gameObject.SetActive(false);
    }

    // 破棄済み/親違いを除去
    private void CleanupDialogStack(Transform expectedParent)
    {
        for (int i = m_dialogs.Count - 1; i >= 0; i--)
        {
            var t = m_dialogs[i];
            if (t == null || t.parent != expectedParent)
            {
                m_dialogs.RemoveAt(i);
                m_onCloseStack.RemoveAt(i); // コールバック側も同期除去
            }
        }
    }

    // ====== internals ======

    private static Transform ResolveRootSafeArea(Transform t)
    {
        var canvas = t.GetComponentInParent<Canvas>(true);
        if (canvas == null) return null;

        var root = canvas.rootCanvas != null ? canvas.rootCanvas.transform : canvas.transform;

        var sa = root.Find(UIConstants.SafeAreaName);
        if (sa != null) return sa;

        return FindDescendantByName(root, UIConstants.SafeAreaName);
    }

    private static Transform FindDescendantByName(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c.name == name) return c;
            var hit = FindDescendantByName(c, name);
            if (hit != null) return hit;
        }
        return null;
    }

    private static ModalOverlay CreateUnder(Transform safeArea)
    {
        var go = new GameObject("ModalOverlay", typeof(RectTransform));
        go.layer = safeArea.gameObject.layer;
        go.transform.SetParent(safeArea, false);

        var rt = (RectTransform)go.transform;
        UIUtil.StretchToParentRectTransform(rt, safeArea);

        var img = go.AddComponent<Image>();
        img.raycastTarget = true;

        var overlay = go.AddComponent<ModalOverlay>();
        overlay.m_rect = rt;
        overlay.m_fullscreenImg = img;
        overlay.m_keyParent = safeArea;
        overlay.m_stack = 0;

        overlay.m_fullscreenImg.color = overlay.m_overlayColor;
        go.SetActive(false);
        return overlay;
    }

    private void UnregisterFromPool()
    {
        if (m_keyParent == null) return;
        if (s_pool.TryGetValue(m_keyParent, out var me) && me == this)
            s_pool.Remove(m_keyParent);
    }

    private void RegisterToPool(Transform safeArea)
    {
        m_keyParent = safeArea;
        s_pool[m_keyParent] = this;
    }

    private void OnDestroy() => UnregisterFromPool();

    private void PlaySE()
    {
        SoundManager.Instance?.PlaySE(m_seFileName);
    }

    // ===== 背景クリック：閉じる要求。ダイアログ側が Close → Handle.Release を呼ぶのが基本。
    // 念のためのフォールバックとして、最前面を直接 Pop する処理も用意。
    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySE();

        // まず“今のトップ”向け onClose を呼ぶ
        m_onCloseRequested?.Invoke();

        // 念のためフォールバック：トップを直接 Pop
        if (m_dialogs.Count > 0)
        {
            var top = m_dialogs[m_dialogs.Count - 1];
            Pop(top);
        }
    }

    public readonly struct OverlayHandle : IDisposable
    {
        private readonly ModalOverlay m_owner;
        private readonly Transform m_dialog;

        public OverlayHandle(ModalOverlay owner, Transform dialog)
        {
            m_owner = owner;
            m_dialog = dialog;
        }

        public void Dispose() => Release();
        public void Release() { m_owner?.Pop(m_dialog); }
    }
}
