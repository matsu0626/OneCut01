#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ScrollViewCreator
{
    private const string MenuV = UIConstants.UIExtensionsMenuPath + "Scroll View (Vertical)";
    private const string MenuH = UIConstants.UIExtensionsMenuPath + "Scroll View (Horizontal)";
    private const string MenuV2 = UIConstants.UIExtensionsMenuPath + "Scroll View (Vertical + Scrollbar)";
    private const string MenuH2 = UIConstants.UIExtensionsMenuPath + "Scroll View (Horizontal + Scrollbar)";

    // --- 元のメニュー（スクロールバーなし） -----------------------------

    [MenuItem(MenuV, false, EditorConstants.MenuItemSortOrder.ScrollViewVertical)]
    public static void CreateVertical(MenuCommand cmd)
    {
        Create(cmd, vertical: true, withScrollbar: false);
    }

    [MenuItem(MenuH, false, EditorConstants.MenuItemSortOrder.ScrollViewHorizontal)]
    public static void CreateHorizontal(MenuCommand cmd)
    {
        Create(cmd, vertical: false, withScrollbar: false);
    }

    // --- 追加メニュー（スクロールバー付き） -----------------------------

    [MenuItem(MenuV2, false, EditorConstants.MenuItemSortOrder.ScrollViewVerticalScrollbar)]
    public static void CreateVerticalWithScrollbar(MenuCommand cmd)
    {
        Create(cmd, vertical: true, withScrollbar: true);
    }

    [MenuItem(MenuH2, false, EditorConstants.MenuItemSortOrder.ScrollViewHorizontalScrollbar)]
    public static void CreateHorizontalWithScrollbar(MenuCommand cmd)
    {
        Create(cmd, vertical: false, withScrollbar: true);
    }

    // ----------------------------------------------------------------------

    private static void Create(MenuCommand cmd, bool vertical, bool withScrollbar)
    {
        // 親決定（指定がなければ既存 Canvas、なければ作成）
        GameObject parent = cmd.context as GameObject;
        if (!parent)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (!canvas)
            {
                GameObject goCanvas = new GameObject(
                    "Canvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

                Canvas c = goCanvas.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = goCanvas.GetComponent<CanvasScaler>();
                // 既存プロジェクトの基準に合わせてください
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                parent = goCanvas;
            }
            else
            {
                parent = canvas.gameObject;
            }
        }

        // ScrollView 本体の作成（Viewport / Content まで）
        (GameObject root,
         ScrollRect scroll,
         RectTransform viewport,
         RectTransform content) = CreateScrollViewRoot(parent.transform, vertical);

        // ランタイム制御
        if (!root.GetComponent<UIScrollView>())
        {
            root.AddComponent<UIScrollView>();
        }

        // スクロールバー付き指定ならここで追加
        if (withScrollbar)
        {
            if (vertical)
            {
                CreateVerticalScrollbar(root.transform, scroll);
            }
            else
            {
                CreateHorizontalScrollbar(root.transform, scroll);
            }
        }

        Undo.RegisterCreatedObjectUndo(root, "Create Scroll View");
        Selection.activeGameObject = root;
    }

    /// <summary>
    /// ScrollView ルート（ScrollRect / Viewport / Content）を作成して返す。
    /// </summary>
    private static (GameObject root,
                    ScrollRect scroll,
                    RectTransform viewport,
                    RectTransform content)
        CreateScrollViewRoot(Transform parent, bool vertical)
    {
        // Root
        GameObject root = CommonUtil.CreateGO(
            vertical ? "ScrollView (Vertical)" : "ScrollView (Horizontal)",
            parent);
        RectTransform rootRT = (RectTransform)root.transform;
        rootRT.sizeDelta = new Vector2(600f, 600f);

        ScrollRect scroll = root.AddComponent<ScrollRect>();
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.horizontal = !vertical;
        scroll.vertical = vertical;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject vp = CommonUtil.CreateGO("Viewport", root.transform);
        RectTransform vpRT = (RectTransform)vp.transform;
        UIUtil.StretchToParentRectTransform(vpRT, root.transform);
        vp.AddComponent<RectMask2D>();
        scroll.viewport = vpRT;

        // Content
        GameObject contentGO = CommonUtil.CreateGO("Content", vp.transform);
        RectTransform ctRT = (RectTransform)contentGO.transform;
        ctRT.anchorMin = new Vector2(0f, 1f);
        ctRT.anchorMax = new Vector2(1f, 1f);
        ctRT.pivot = new Vector2(0.5f, 1f);
        ctRT.anchoredPosition = Vector2.zero;
        ctRT.sizeDelta = Vector2.zero;

        if (vertical)
        {
            VerticalLayoutGroup layout = contentGO.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 8f;
        }
        else
        {
            HorizontalLayoutGroup layout = contentGO.AddComponent<HorizontalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            layout.spacing = 8f;
        }

        ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = vertical
            ? ContentSizeFitter.FitMode.PreferredSize
            : ContentSizeFitter.FitMode.Unconstrained;
        fitter.horizontalFit = vertical
            ? ContentSizeFitter.FitMode.Unconstrained
            : ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = ctRT;

        return (root, scroll, vpRT, ctRT);
    }

    /// <summary>
    /// 縦用スクロールバーを作成して ScrollRect に紐づける。
    /// </summary>
    private static void CreateVerticalScrollbar(Transform root, ScrollRect scroll)
    {
        GameObject sbGO = CommonUtil.CreateGO("Scrollbar Vertical", root);
        RectTransform sbRT = (RectTransform)sbGO.transform;

        // 右端いっぱいに配置
        sbRT.anchorMin = new Vector2(1f, 0f);
        sbRT.anchorMax = new Vector2(1f, 1f);
        sbRT.pivot = new Vector2(1f, 1f);
        sbRT.sizeDelta = new Vector2(20f, 0f);
        sbRT.anchoredPosition = Vector2.zero;

        Image background = sbGO.AddComponent<Image>();
        background.raycastTarget = true;

        GameObject handleGO = CommonUtil.CreateGO("Handle", sbGO.transform);
        RectTransform handleRT = (RectTransform)handleGO.transform;
        handleRT.anchorMin = new Vector2(0f, 0f);
        handleRT.anchorMax = new Vector2(1f, 1f);
        handleRT.sizeDelta = Vector2.zero;

        Image handleImg = handleGO.AddComponent<Image>();

        Scrollbar scrollbar = sbGO.AddComponent<Scrollbar>();
        scrollbar.targetGraphic = handleImg;
        scrollbar.handleRect = handleRT;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }

    /// <summary>
    /// 横用スクロールバーを作成して ScrollRect に紐づける。
    /// </summary>
    private static void CreateHorizontalScrollbar(Transform root, ScrollRect scroll)
    {
        GameObject sbGO = CommonUtil.CreateGO("Scrollbar Horizontal", root);
        RectTransform sbRT = (RectTransform)sbGO.transform;

        // 下端いっぱいに配置
        sbRT.anchorMin = new Vector2(0f, 0f);
        sbRT.anchorMax = new Vector2(1f, 0f);
        sbRT.pivot = new Vector2(0.5f, 0f);
        sbRT.sizeDelta = new Vector2(0f, 20f);
        sbRT.anchoredPosition = Vector2.zero;

        Image background = sbGO.AddComponent<Image>();
        background.raycastTarget = true;

        GameObject handleGO = CommonUtil.CreateGO("Handle", sbGO.transform);
        RectTransform handleRT = (RectTransform)handleGO.transform;
        handleRT.anchorMin = new Vector2(0f, 0f);
        handleRT.anchorMax = new Vector2(1f, 1f);
        handleRT.sizeDelta = Vector2.zero;

        Image handleImg = handleGO.AddComponent<Image>();

        Scrollbar scrollbar = sbGO.AddComponent<Scrollbar>();
        scrollbar.targetGraphic = handleImg;
        scrollbar.handleRect = handleRT;
        scrollbar.direction = Scrollbar.Direction.LeftToRight;

        scroll.horizontalScrollbar = scrollbar;
        scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }
}
#endif
