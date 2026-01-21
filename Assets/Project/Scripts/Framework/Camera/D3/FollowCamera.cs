using UnityEngine;

/// <summary>
/// ターゲット追従カメラ（3D）
/// ・サイドビュー / ビハインドビューをプルダウンで切り替え可能
/// ・位置のみ追従、回転は LookAt でターゲットを見るシンプル仕様
/// </summary>
public sealed class FollowCamera3D : MonoBehaviour
{
    // どのモードでオフセットを解釈するか
    public enum ViewMode
    {
        /// <summary>
        /// ワールド座標固定オフセット（サイド固定ビュー向け）
        /// </summary>
        Side,

        /// <summary>
        /// ターゲットのローカル座標でオフセット（ビハインドビュー向け）
        /// </summary>
        Behind,
    }

    [Header("Target")]
    [SerializeField] private Transform m_target;          // 追従対象

    [Header("View")]
    [SerializeField] private ViewMode m_viewMode = ViewMode.Behind;

    /// <summary>
    /// カメラオフセット
    /// Side  : ワールド空間でのオフセット
    /// Behind: (0,高さ, -距離) をターゲットの回転で回して使用
    /// </summary>
    [SerializeField] private Vector3 m_offset = new Vector3(0f, 3f, -6f);

    /// <summary>
    /// ターゲットを見るかどうか
    /// </summary>
    [SerializeField] private bool m_lookAtTarget = true;

    /// <summary>
    /// LookAt する位置へのオフセット（頭の少し上を見たいとき等）
    /// </summary>
    [SerializeField] private Vector3 m_lookAtOffset = Vector3.zero;

    [Header("Follow")]
    [SerializeField] private float m_followLerp = 10f;    // 追従の補間速度
    [SerializeField] private bool m_useUnscaledDeltaTime = true;

    private void Reset()
    {
        // とりあえず mainCamera に付けたとき用の簡易初期化
        if (!m_target && Camera.main != null && Camera.main != this)
        {
            // 何もしない（手動で設定してもらう想定）
        }
    }

    private void LateUpdate()
    {
        if (!m_target)
        {
            return;
        }

        float dt = m_useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }

        // ビューの種類に応じてオフセットを決定
        Vector3 offset;
        switch (m_viewMode)
        {
            case ViewMode.Side:
                // ワールド空間のオフセットそのまま
                offset = m_offset;
                break;

            case ViewMode.Behind:
            default:
                // ターゲットの回転に合わせてローカルオフセットを回す
                offset = m_target.rotation * m_offset;
                break;
        }

        // 目標位置を計算
        Vector3 desiredPos = m_target.position + offset;

        // なめらかに追従
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            m_followLerp * dt
        );

        // ターゲットを見る
        if (m_lookAtTarget)
        {
            Vector3 lookPos = m_target.position + m_lookAtOffset;
            transform.LookAt(lookPos);
        }
    }

    /// <summary>
    /// Runtime からターゲットを差し替えたいとき用
    /// </summary>
    public void SetTarget(Transform target)
    {
        m_target = target;
    }

    /// <summary>
    /// Runtime からビューを切り替えたいとき用
    /// </summary>
    public void SetViewMode(ViewMode mode)
    {
        m_viewMode = mode;
    }
}
