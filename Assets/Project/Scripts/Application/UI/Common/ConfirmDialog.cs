using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public sealed class ConfirmDialog : MonoBehaviour
{
    // ボタンモード
    public enum ButtonMode
    {
        YesNo = 0,  // [YES][NO]両方
        Yes,        // [YES]のみ（水平センタリング）
    }

    public enum Result 
    { 
        Yes, 
        No,
    }

    [SerializeField] private TextMeshProUGUI m_messageText;
    [SerializeField] private UIButton m_yesBtn;
    [SerializeField] private UIButton m_noBtn;
    [SerializeField] private RectTransform m_baseRt;        // Base
    [SerializeField] private RectTransform m_textBaseRt;    // TextBase
    [SerializeField] private ConfirmDialogLayoutSettings m_settings;

    private AssetLoadHelper.Instantiated m_handle;
    private UniTaskCompletionSource<Result> m_tcs;
    private ButtonMode m_buttonMode = ButtonMode.YesNo;
    private bool m_decided;

    // Overlay 
    private ModalOverlay m_overlay;
    private ModalOverlay.OverlayHandle m_overlayHandle;
    private bool m_overlayActive;


    /// <summary>
    /// Addressables から ConfirmDialog を生成して返す
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="collectionName"></param>
    /// <param name="entryKey"></param>
    /// <param name="mode"></param>
    /// <param name="token"></param>
    /// <param name="smartArgs"></param>
    /// <returns></returns>
    public static async UniTask<ConfirmDialog> Create(    
        string collectionName,
        string entryKey,
        ButtonMode mode,
        CancellationToken token = default,
        params object[] smartArgs)
    {
        // ローカライズ文字列取得
        string localized;
        try
        {
            localized = await LocalizedStringUtil.GetStringAsync(collectionName, entryKey, smartArgs, token);
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception e) { AppDebug.LogException(e); return null; }

        return await Create(localized, mode, token);
    }

    /// <summary>
    /// Addressables から ConfirmDialog を生成して返す・Transform, string直接渡し
    /// </summary>
    /// <param name="text"></param>
    /// <param name="mode"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async UniTask<ConfirmDialog> Create(string text, ButtonMode mode, CancellationToken token = default)
    {

        AssetLoadHelper.Instantiated inst;

        try
        {
            inst = await AssetLoadHelper.InstantiateAsync("ConfirmDialog", token: token);
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
        ConfirmDialog dlg = go.GetComponent<ConfirmDialog>();
        if (dlg == null)
        {
            dlg = go.AddComponent<ConfirmDialog>();
        }

        dlg.Init(inst, text, mode);

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
        // 背景クリックで Closed
        m_overlayHandle = m_overlay.Push(transform, onCloseRequested: () => TrySetResult(Result.No));
        m_overlayActive = true;

        gameObject.SetActive(true);

        Result result;
        try
        {
            result = await m_tcs.Task;
        }
        finally
        {
            // Overlay は必ず解放
            if (m_overlayActive)
            {
                m_overlayHandle.Release();
                m_overlayActive = false;
            }
        }

        return result;
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

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="inst"></param>
    /// <param name="text"></param>
    /// <param name="mode"></param>
    private void Init(AssetLoadHelper.Instantiated inst, string text, ButtonMode mode)
    {
        m_handle = inst;
        m_buttonMode = mode;

        m_messageText?.SetText(text ?? string.Empty);
        ApplyLayout();

        // 二重登録ガード：一旦全部外してから付ける
        m_yesBtn.onClick.RemoveAllListeners();
        m_noBtn.onClick.RemoveAllListeners();

        m_yesBtn.onClick.AddListener(() => TrySetResult(Result.Yes));
        if (m_buttonMode == ButtonMode.YesNo)
        {
            m_noBtn.onClick.AddListener(() => TrySetResult(Result.No));
        }
        
    }


    private void TrySetResult(Result result)
    {
        if (m_decided) return;
        m_decided = true;
        m_tcs?.TrySetResult(result);

        // 閉じる（破棄）
        Destroy(gameObject);
    }


    private void ApplyLayout()
    {
        // 1) 本文の推奨高さ
        float availableWidth = GetAvailableTextWidth();
        m_messageText.textWrappingMode = TextWrappingModes.Normal;
        m_messageText.enableAutoSizing = false;
        m_messageText.ForceMeshUpdate();

        Vector2 pref = m_messageText.GetPreferredValues(m_messageText.text, availableWidth, Mathf.Infinity);
        float textHeight = Mathf.Ceil(pref.y);

        // 2) Text / TextBase の高さ確定
        var textRt = (RectTransform)m_messageText.transform;
        SetHeight(textRt, textHeight);

        float textBaseHeight = Mathf.Max(textHeight + m_settings.textBaseAdjustHeight,
                                         m_settings.textBaseMinHeight);
        SetHeight(m_textBaseRt, textBaseHeight);

        // 3) Base の必要高さを式で算出
        var yesRt = (RectTransform)m_yesBtn.transform;
        var noRt = (RectTransform)m_noBtn.transform;
        float yesH = yesRt.rect.height; // ボタンの高さ（Prefabで固定想定）

        float requiredBase =
            textBaseHeight
            + m_settings.spacingTextToButton   // TextBase→ボタンの隙間
            + yesH
            + m_settings.buttonBottomPadding   // 下余白
            + m_settings.baseAdjustHeight;     // 上余白ぶん

        float baseHeight = Mathf.Max(requiredBase, m_settings.baseMinHeight);
        SetHeight(m_baseRt, baseHeight);

        // 4) YES/NOボタンを“下余白に沿って”配置（縦アンカーは下固定に）
        EnsureBottomAnchored(yesRt);
        SetBottomPadding(yesRt, m_settings.buttonBottomPadding);

        if (m_buttonMode == ButtonMode.YesNo)
        {
            EnsureBottomAnchored(noRt);
            SetBottomPadding(noRt, m_settings.buttonBottomPadding);
        } else
        {
            // YESのみの場合は、NOボタン非表示＆YESボタンを中央寄せ
            m_noBtn.gameObject.SetActive(false);
            UIUtil.CenterHorizontally(yesRt);
        }
        
    }

    // --- helpers ---
    private static void SetHeight(RectTransform rt, float h)
        => rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);

    private static void EnsureBottomAnchored(RectTransform rt)
    {
        // 縦アンカーを下固定(anchorMin.y=anchorMax.y=0)にし、現在の高さを維持
        if (!Mathf.Approximately(rt.anchorMin.y, 0f) || !Mathf.Approximately(rt.anchorMax.y, 0f))
        {
            Vector2 aMin = rt.anchorMin, aMax = rt.anchorMax;
            aMin.y = 0f; aMax.y = 0f;
            rt.anchorMin = aMin; rt.anchorMax = aMax;

            float h = rt.rect.height;
            rt.offsetMin = new Vector2(rt.offsetMin.x, 0f);
            rt.offsetMax = new Vector2(rt.offsetMax.x, h);
        }
    }

    private static void SetBottomPadding(RectTransform rt, float bottom)
    {
        // 下固定アンカー時、offsetMin.y が「下からの余白」
        Vector2 offMin = rt.offsetMin;
        offMin.y = bottom;
        rt.offsetMin = offMin;
    }

    private float GetAvailableTextWidth()
    {
        // TextBase の現在の見た目の幅
        var w = m_textBaseRt.rect.width;
        if (w <= 0f) w = m_baseRt.rect.width;                                       // 保険
        if (w <= 0f) w = ((RectTransform)m_messageText.transform).rect.width;       // 最後の保険
        return w;
    }

    /// <summary>自分を最前面へ</summary>
    private void BringToFront()
    {
        transform.SetAsLastSibling();
    }
}
