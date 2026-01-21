using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ボタン拡張
/// </summary>
[AddComponentMenu("UI/UIButton")]
public class UIButton : Button
{
    [Header("SEファイル名")]
    [SerializeField] private string seFileName = "se_01";

    public event Action OnClickProc;

    private void PlaySE()
    {
        SoundManager.Instance?.PlaySE(seFileName);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (!IsActive() || !IsInteractable()) return;

        PlaySE();
        OnClickProc?.Invoke();
        base.OnPointerClick(eventData);
    }

    // クリック時コールバッククリア

    public void ClearOnClick()
    {
        OnClickProc = null;
    }

    protected override void OnDestroy()
    {
        ClearOnClick();
        base.OnDestroy();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UIButton))]
class UIButtonEditor : UnityEditor.UI.ButtonEditor
{
    SerializedProperty seFileName;

    protected override void OnEnable()
    {
        base.OnEnable();
        seFileName = serializedObject.FindProperty("seFileName");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("SE Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(seFileName);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
