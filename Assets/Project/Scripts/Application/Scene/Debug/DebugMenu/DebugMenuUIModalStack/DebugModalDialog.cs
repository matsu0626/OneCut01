using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public sealed class DebugModalDialog : MonoBehaviour
{
    private const float SLIDE_OFFSET = 30f;
    public enum Result 
    {
        None,
        Yes, 
        No,
    }

    [SerializeField] private UIButton m_closeBtn;
    [SerializeField] private UIButton m_yesBtn;

    private AssetLoadHelper.Instantiated m_handle;
    private UniTaskCompletionSource<Result> m_tcs;
    private bool m_decided;
    private int m_index;
    private bool m_isTop;

    // Overlay 
    private ModalOverlay m_overlay;
    private ModalOverlay.OverlayHandle m_overlayHandle;
    private bool m_overlayActive;


    /// <summary>
    /// Addressables から DebugModalDialog を生成して返す
    /// </summary>
    /// <param name="index"></param>
    /// <param name="isTop">true == 最上位か？</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async UniTask<DebugModalDialog> Create(int index, bool isTop, CancellationToken token = default)
    {
        AssetLoadHelper.Instantiated inst;

        try
        {
            inst = await AssetLoadHelper.InstantiateAsync("DebugModalDialog", token: token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            AppDebug.LogException(e);
            return null;
        }
               
        GameObject go = inst.Instance;
        DebugModalDialog dlg = go.GetComponent<DebugModalDialog>();
        if (dlg == null)
        {
            dlg = go.AddComponent<DebugModalDialog>();
        }

        dlg.Init(inst, index, isTop);

        return dlg;
    }

    /// <summary>
    /// 表示＆結果取得
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async UniTask<Result> ShowAsync(CancellationToken token = default)
    {
        // すでに待機中なら再利用せず新規に（安全）
        m_tcs = new UniTaskCompletionSource<Result>();
        m_decided = false;

        // キャンセル伝播（例：シーン遷移や親破棄）
        if (token.CanBeCanceled)
        {
            token.Register(() =>
            {
                if (!m_decided) TrySetResult(Result.No);
            });
        }


        // 1) SafeArea 親付け完了まで待つ（これが肝）
        if (TryGetComponent<AttachToRootSafeArea>(out var attacher))
            await attacher.WaitUntilAttachedAsync(token);

        // 2) まず自分（Dialog）を最前面に（この順序が重要）
        BringToFront();

        // 3) 親 (= SafeArea) をキーに Overlay を用意
        var parent = transform.parent;
        AppDebug.Assert(parent != null, "Parent is null after attach.");
        m_overlay = ModalOverlay.GetOrCreate(parent);
        m_overlayHandle = m_overlay.Push(transform, onCloseRequested: () => TrySetResult(Result.No));
        m_overlayActive = true;

        gameObject.SetActive(true);
        return await m_tcs.Task;
    }

    /// <summary>
    /// 閉じる（破棄）
    /// </summary>
    public void Close()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (m_overlayActive)
        {
            m_overlayHandle.Release();
            m_overlayActive = false;
        }
        m_handle.Dispose();
    }
    
    
    private void Init(AssetLoadHelper.Instantiated inst, int index, bool isTop)
    {
        m_handle = inst;
        m_isTop = isTop;
        m_index = index;

        var rt = (RectTransform)transform;
        float ofs = SLIDE_OFFSET * m_index;
        rt.anchoredPosition += new Vector2(ofs, -ofs);


        // 二重登録ガード：一旦全部外してから付ける
        m_yesBtn.onClick.RemoveAllListeners();
        m_closeBtn.onClick.RemoveAllListeners();

        // 最上位なら Yes ボタン非表示
        if (m_isTop)
        {
            m_yesBtn.gameObject.SetActive(false);
        }
        else
        {
            m_yesBtn.gameObject.SetActive(true);
            m_yesBtn.onClick.AddListener(() => TrySetResult(Result.Yes));
        }
        
        m_closeBtn.onClick.AddListener(() => TrySetResult(Result.No));
    }


    private void TrySetResult(Result result)
    {
        if (m_decided) return;
        m_decided = true;
        m_tcs?.TrySetResult(result);
    }

    /// <summary>自分を最前面へ</summary>
    private void BringToFront()
    {
        transform.SetAsLastSibling();
    }
}
