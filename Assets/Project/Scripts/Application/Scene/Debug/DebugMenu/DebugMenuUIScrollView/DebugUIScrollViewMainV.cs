using TMPro;
using UnityEngine;
using UnityEngine.UI;


public sealed class DebugUIScrollViewMainV : MonoBehaviour, IScrollViewDelegate
{
    [SerializeField] private UIScrollView m_scrollView;
    [SerializeField] private GameObject m_cellPrefab;
    [SerializeField] private int m_itemCount = 30;
    [SerializeField] private bool m_horizontal = false;

    private int m_selectedIndex = 0;

    private void Start()
    {
        if (!m_scrollView || !m_cellPrefab)
        {
            Debug.LogError("[DebugUIScrollViewMainV] m_scrollView / m_cellPrefab 未設定");
            return;
        }

        // 縦横・件数・見た目更新/クリック受け取りのみ指定
        m_scrollView.Initialize(m_cellPrefab, m_itemCount, m_horizontal, this);
    }

    // 見た目更新だけやればOK（クリック配線は UIScrollView が自動でやる）
    public void SetupCell(UIScrollCell cell, int index)
    {
        var title = cell.GetByName<TextMeshProUGUI>("Label");
        title.text = $"{index}";

        var selectImg = cell.GetByName<Image>("Select");
        bool isSelected = (index == m_selectedIndex);
        selectImg.gameObject.SetActive(isSelected);

    }
    // クリック結果（複数ボタンは controlId で識別：GameObject名）
    public void OnCellClicked(UIScrollCell cell, int index, string controlId)
    {
        // controlId でボタン別の処理を分けることもできる
        Debug.Log($"Cell clicked: index={index}, id={controlId}");

        if (m_selectedIndex == index) return;

        // 選択変更した場合は、枠の更新のためセルを再セットアップ
        m_selectedIndex = index;
        m_scrollView.Refresh();
    }
}
