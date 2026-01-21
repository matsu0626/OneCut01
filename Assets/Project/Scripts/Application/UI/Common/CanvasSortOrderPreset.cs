using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[ExecuteAlways]
public sealed class CanvasSortOrderPreset : MonoBehaviour
{
    [StaticIntDropdown(typeof(UIConstants.CanvasSortOrder))]
    [SerializeField] private int m_sortOrder = UIConstants.CanvasSortOrder.UI;

    [SerializeField, Tooltip("プリセットに加算する微調整")]
    private int m_offset = 0;

    [SerializeField, Tooltip("子階層のCanvasにも同じ順序を適用")]
    private bool m_applyToChildren = false;

#if UNITY_EDITOR
    [SerializeField, Tooltip("InspectorでCanvasを編集不能にする（SortOrder直接入力を防止")]
    private bool m_lockCanvasInspector = true;
#endif

    private Canvas _canvas;

    private void Reset()
    {
        _canvas = GetComponent<Canvas>();
        EnsureCanvasSetup();
        Apply();
#if UNITY_EDITOR
        ApplyCanvasLock();
#endif
    }

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        EnsureCanvasSetup();
        Apply();
#if UNITY_EDITOR
        ApplyCanvasLock();
#endif
    }

    private void OnEnable()
    {
        Apply();
#if UNITY_EDITOR
        ApplyCanvasLock();
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_canvas == null) _canvas = GetComponent<Canvas>();
        EnsureCanvasSetup();
        Apply();
        ApplyCanvasLock();
    }
#endif

    private void EnsureCanvasSetup()
    {
        if (_canvas == null) _canvas = GetComponent<Canvas>();
        _canvas.overrideSorting = true;
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    public void Apply()
    {
        var order = m_sortOrder + m_offset;

        _canvas.overrideSorting = true;
        _canvas.sortingOrder = order;

        if (m_applyToChildren)
        {
            var childCanvases = GetComponentsInChildren<Canvas>(true);
            for (var i = 0; i < childCanvases.Length; i++)
            {
                var c = childCanvases[i];
                if (c == _canvas) continue;
                c.overrideSorting = true;
                c.sortingOrder = order;
            }
        }
    }

#if UNITY_EDITOR
    private void ApplyCanvasLock()
    {
        SetCanvasLocked(m_lockCanvasInspector);
    }

    private void SetCanvasLocked(bool locked)
    {
        if (_canvas == null) return;

        var flags = _canvas.hideFlags;

        if (locked)
            _canvas.hideFlags = flags | HideFlags.NotEditable;
        else
            _canvas.hideFlags = flags & ~HideFlags.NotEditable;

        UnityEditor.EditorUtility.SetDirty(_canvas);
    }
#endif

    public void SetSortOrderByConst(int baseValue, int offset = 0)
    {
        m_sortOrder = baseValue;
        m_offset = offset;
        Apply();
    }
}
