using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class OneCutPlayerController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float m_moveSpeed = 3.0f;
    [SerializeField] float m_gravity = -9.81f;

    [Header("Visual")]
    [SerializeField] Transform m_modelRoot;         // Character01 を入れる
    [SerializeField] float m_turnSpeedDeg = 360f; // 1秒あたりの回転速度(度)

    CharacterController m_controller;
    float m_verticalVelocity;

    float m_currentYaw; // 現在のY角（見た目用）
    float m_targetYaw;  // 目標Y角（0 or 180）

    void Awake()
    {
        m_controller = GetComponent<CharacterController>();

        if (m_modelRoot == null)
        {
            var child = transform.Find("Character01");
            if (child != null)
            {
                m_modelRoot = child;
            }
        }

        if (m_modelRoot != null)
        {
            m_currentYaw = m_modelRoot.localEulerAngles.y;
            m_targetYaw = m_currentYaw;
        }
    }

    void Update()
    {
        float inputZ = 0f;

        if (GameKeyboard.Instance != null)
        {
            if (GameKeyboard.Instance.isPress(GameKeyboard.Code.Up))
            {
                inputZ += 1f;
            }
            if (GameKeyboard.Instance.isPress(GameKeyboard.Code.Down))
            {
                inputZ -= 1f;
            }
        }

        // ---- 向きの目標角度だけ決める ----
        if (inputZ > 0f)
        {
            m_targetYaw = 0f;     // 奥向き
        }
        else if (inputZ < 0f)
        {
            m_targetYaw = 180f;   // 手前向き
        }
        // inputZ == 0 のときは、直前の m_targetYaw を保つ

        // ---- 現在角度を目標角度へなめらかに近づける ----
        if (m_modelRoot != null)
        {
            m_currentYaw = Mathf.MoveTowardsAngle(
                m_currentYaw,
                m_targetYaw,
                m_turnSpeedDeg * Time.deltaTime
            );

            m_modelRoot.localRotation = Quaternion.Euler(0f, m_currentYaw, 0f);
        }

        // ---- 前後移動(Zのみ) ----
        Vector3 move = Vector3.forward * (inputZ * m_moveSpeed);

        // ---- 簡易重力 ----
        if (m_controller.isGrounded && m_verticalVelocity < 0f)
        {
            m_verticalVelocity = -1f;
        }

        m_verticalVelocity += m_gravity * Time.deltaTime;
        move.y = m_verticalVelocity;

        m_controller.Move(move * Time.deltaTime);
    }
}
