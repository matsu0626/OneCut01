using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SafeAreaFitter : MonoBehaviour
{
    [Header("Editor Preview (normalized 0..1, x,y = left/bottom, w,h)")]
    [SerializeField] private Rect m_customNormalizedPortrait = new Rect(0f, 0.03f, 1f, 0.94f); // 上5%/下3% 目安
    [SerializeField] private Rect m_customNormalizedLandscape = new Rect(0.05f, 0f, 0.90f, 1f); // 左右5% 目安

    private RectTransform m_rt;
    private Rect m_lastSafeArea;
    private Vector2Int m_lastScreen;
    private ScreenOrientation m_lastOrientation;

    private void OnEnable()
    {
        m_rt = transform as RectTransform;
        Apply(force: true);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            ApplyEditorPreview();
            return;
        }

        // OnChange 相当：変化時のみ反映
        if (Screen.safeArea != m_lastSafeArea ||
            Screen.width != m_lastScreen.x ||
            Screen.height != m_lastScreen.y ||
            Screen.orientation != m_lastOrientation)
        {
            Apply();
        }
    }

    // --- Runtime apply ---
    public void Apply(bool force = false)
    {
        if (!EnsureRT()) return;

        var canvas = m_rt.GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.isRootCanvas) { SetFullStretch(m_rt); return; }

        var pixel = canvas.pixelRect;
        if (pixel.width <= 0 || pixel.height <= 0) { SetFullStretch(m_rt); return; }

        var sa = Screen.safeArea;
        SetAnchorsFromPixelRect(sa, pixel);

        m_lastSafeArea = sa;
        m_lastScreen = new Vector2Int(Screen.width, Screen.height);
        m_lastOrientation = Screen.orientation;
    }

    // --- Editor: always use CustomNormalized ---
    private void ApplyEditorPreview(bool force = false)
    {
        if (!EnsureRT()) return;

        var canvas = m_rt.GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.isRootCanvas) { SetFullStretch(m_rt); return; }

        var pixel = canvas.pixelRect;
        if (pixel.width <= 0 || pixel.height <= 0) { SetFullStretch(m_rt); return; }

        bool portrait = IsPortrait(Screen.width, Screen.height);
        var norm = portrait ? m_customNormalizedPortrait : m_customNormalizedLandscape;
        var target = PixelFromNormalized(pixel, norm);

        SetAnchorsFromPixelRect(target, pixel);
    }

    [ContextMenu("Apply Now")]
    private void ApplyNow_ContextMenu()
    {
        if (Application.isPlaying) Apply(force: true);
        else ApplyEditorPreview(force: true);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // --- helpers ---
    private bool EnsureRT()
    {
        if (m_rt == null) m_rt = transform as RectTransform;
        return m_rt != null;
    }

    private static Rect PixelFromNormalized(Rect canvasPixel, Rect norm)
    {
        return new Rect(
            canvasPixel.x + canvasPixel.width * norm.x,
            canvasPixel.y + canvasPixel.height * norm.y,
            canvasPixel.width * norm.width,
            canvasPixel.height * norm.height
        );
    }

    private void SetAnchorsFromPixelRect(Rect safe, Rect pixelRect)
    {
        var min = new Vector2(
            Mathf.InverseLerp(pixelRect.x, pixelRect.xMax, safe.xMin),
            Mathf.InverseLerp(pixelRect.y, pixelRect.yMax, safe.yMin)
        );
        var max = new Vector2(
            Mathf.InverseLerp(pixelRect.x, pixelRect.xMax, safe.xMax),
            Mathf.InverseLerp(pixelRect.y, pixelRect.yMax, safe.yMax)
        );

        m_rt.anchorMin = min;
        m_rt.anchorMax = max;
        m_rt.offsetMin = Vector2.zero;
        m_rt.offsetMax = Vector2.zero;
    }

    private static void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static bool IsPortrait(int w, int h) => h >= w;
}
