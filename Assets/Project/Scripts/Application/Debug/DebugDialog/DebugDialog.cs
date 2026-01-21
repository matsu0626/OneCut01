using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DebugDialog : MonoBehaviour, IScrollViewDelegate
{
    [Serializable]
    private sealed class CategoryInfo
    {
        [Header("表示名（カテゴリ名）")]
        public string title;

        [Header("右側に表示するパネルのプレハブ")]
        public GameObject panelPrefab;
    }

    [Header("Root Panel (表示/非表示を切り替える親)")]
    [SerializeField] private GameObject m_rootPanel;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI m_titleText;
    [SerializeField] private UIButton m_closeButton;

    [Header("Left: Category List")]
    [SerializeField] private UIScrollView m_categoryScrollView;
    [SerializeField] private GameObject m_categoryCellPrefab;

    [Header("Right: Content Area Root")]
    [SerializeField] private RectTransform m_contentRoot;

    [Header("Categories")]
    [SerializeField] private List<CategoryInfo> m_categories = new List<CategoryInfo>();

    [SerializeField] private int m_selectedIndex = 0;

    private GameObject m_currentPanelInstance;
    private bool m_initialized;

    public bool IsOpen
    {
        get
        {
            if (m_rootPanel == null)
            {
                return false;
            }

            return m_rootPanel.activeSelf;
        }
    }

    private void Awake()
    {
        if (m_closeButton != null)
        {
            m_closeButton.OnClickProc += OnClickClose;
        }

        // 起動時はパネルは閉じておく
        if (m_rootPanel != null)
        {
            m_rootPanel.SetActive(false);
        }
    }

    //================= 公開 API =================

    /// <summary>必要なら初期化する（複数回呼んでも1回だけ動く）。</summary>
    public void InitializeIfNeeded()
    {
        if (m_initialized)
        {
            return;
        }

        BuildCategoryList();
        ShowSelectedCategoryPanel();
        UpdateTitle();

        m_initialized = true;
    }

    public void Open()
    {
        InitializeIfNeeded();

        if (m_rootPanel == null)
        {
            Debug.LogWarning("[DebugDialog] m_rootPanel 未設定");
            return;
        }

        m_rootPanel.SetActive(true);
    }

    public void Close()
    {
        if (m_rootPanel == null)
        {
            return;
        }

        m_rootPanel.SetActive(false);
    }

    private void OnClickClose()
    {
        Close();
    }

    //================= 初期化系 =================

    private void BuildCategoryList()
    {
        if (m_categoryScrollView == null || m_categoryCellPrefab == null)
        {
            Debug.LogError("[DebugDialog] m_categoryScrollView / m_categoryCellPrefab 未設定");
            return;
        }

        int itemCount = (m_categories != null) ? m_categories.Count : 0;
        if (itemCount <= 0)
        {
            return;
        }

        if (m_selectedIndex < 0 || m_selectedIndex >= itemCount)
        {
            m_selectedIndex = 0;
        }

        // カテゴリ一覧は縦スクロール前提（horizontal: false）
        m_categoryScrollView.Initialize(m_categoryCellPrefab, itemCount, false, this);
    }

    //================= IScrollViewDelegate =================

    public void SetupCell(UIScrollCell cell, int index)
    {
        TextMeshProUGUI label = cell.GetByName<TextMeshProUGUI>("Label");
        if (label != null)
        {
            string title = (m_categories != null &&
                            index >= 0 &&
                            index < m_categories.Count)
                ? m_categories[index].title
                : string.Format("Category {0}", index);

            label.text = title;
        }

        Image selectImg = cell.GetByName<Image>("Select");
        if (selectImg != null)
        {
            bool isSelected = (index == m_selectedIndex);
            selectImg.gameObject.SetActive(isSelected);
        }
    }

    public void OnCellClicked(UIScrollCell cell, int index, string controlId)
    {
        if (index == m_selectedIndex)
        {
            return;
        }

        m_selectedIndex = index;

        // 左リストの見た目更新
        m_categoryScrollView.Refresh();

        // 右側のパネル差し替え
        ShowSelectedCategoryPanel();
        UpdateTitle();
    }

    //================= 右側パネル =================

    private void ShowSelectedCategoryPanel()
    {
        DestroyCurrentPanel();

        if (m_categories == null ||
            m_selectedIndex < 0 ||
            m_selectedIndex >= m_categories.Count)
        {
            return;
        }

        if (m_contentRoot == null)
        {
            Debug.LogWarning("[DebugDialog] m_contentRoot 未設定");
            return;
        }

        // 念のため：シーンオブジェクトを親にしているかチェック
        if (!m_contentRoot.gameObject.scene.IsValid())
        {
            Debug.LogError("[DebugDialog] m_contentRoot がシーンオブジェクトではなくアセットを指しています。Prefab ではなく、シーン上の ContentArea をアサインしてください。");
            return;
        }

        GameObject prefab = m_categories[m_selectedIndex].panelPrefab;
        if (prefab == null)
        {
            return;
        }

        GameObject instance = Instantiate(prefab, m_contentRoot, false);
        m_currentPanelInstance = instance;
    }

    private void DestroyCurrentPanel()
    {
        if (m_currentPanelInstance != null)
        {
            Destroy(m_currentPanelInstance);
            m_currentPanelInstance = null;
        }
    }

    private void UpdateTitle()
    {
        if (m_titleText == null)
        {
            return;
        }

        string title = string.Empty;

        if (m_categories != null &&
            m_selectedIndex >= 0 &&
            m_selectedIndex < m_categories.Count)
        {
            title = m_categories[m_selectedIndex].title;
        }

        m_titleText.text = title;
    }
}
