#if _DEBUG
using UnityEngine;

/// <summary>
/// DebugMenu_3DCharacterMove 用のワールド側 Root。
/// テスト用プレイヤーやカメラなど、ワールドにある参照をまとめる。
/// </summary>
public sealed class Debug3DCharacterMoveWorldRoot : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform m_playerRoot;

    [Header("Camera")]
    [SerializeField] private Camera m_followCamera;

    public Transform PlayerRoot => m_playerRoot;
    public Camera FollowCamera => m_followCamera;

    private void Reset()
    {
        if (!m_playerRoot)
        {
            m_playerRoot = transform;
        }

        if (!m_followCamera && Camera.main != null)
        {
            m_followCamera = Camera.main;
        }
    }
}
#endif
