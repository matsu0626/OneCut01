using UnityEngine;

/// <summary>
/// 3Dキャラクター移動用モーター。
/// ・CharacterController 前提
/// ・カメラ基準の移動
/// ・重力 / ジャンプ / 接地判定
/// 入力（移動ベクトル / ジャンプ）は外側から渡す。
/// </summary>
[System.Serializable]
public sealed class CharacterMotor3D
{
    [Header("Move")]
    [SerializeField] private float m_moveSpeed = 4.0f;
    [SerializeField] private float m_rotationSpeed = 720.0f;

    [Header("Jump / Gravity")]
    [SerializeField] private float m_jumpPower = 5.0f;
    [SerializeField] private float m_gravity = -9.81f;
    [SerializeField] private float m_terminalVelocity = -50.0f;
    [SerializeField] private float m_groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask m_groundLayer = ~0;

    // 内部状態
    private Vector3 m_velocity;
    private bool m_isGrounded;

    // 外部から参照したい状態
    public Vector3 Velocity => m_velocity;
    public bool IsGrounded => m_isGrounded;

    // 参照
    private CharacterController m_controller;
    private Transform m_modelRoot;
    private Camera m_camera;

    // ==============================
    // 初期化
    // ==============================
    public void Setup(CharacterController controller, Transform modelRoot, Camera camera)
    {
        m_controller = controller;
        m_modelRoot = modelRoot != null ? modelRoot : controller.transform;
        m_camera = camera;

        m_velocity = Vector3.zero;
        m_isGrounded = false;
    }

    // ==============================
    // 1フレーム分の更新
    // ==============================
    public void Tick(Vector2 moveInput, bool jumpPressed, float deltaTime)
    {
        if (m_controller == null || deltaTime <= 0f)
        {
            return;
        }

        // Grounded 更新
        UpdateGrounded();

        // 水平移動（カメラ基準）
        Vector3 horizontalVelocity = CalculateHorizontalVelocity(moveInput);

        // 垂直方向（重力・ジャンプ）
        UpdateVerticalVelocity(jumpPressed, deltaTime);

        // 合成
        m_velocity.x = horizontalVelocity.x;
        m_velocity.z = horizontalVelocity.z;

        Vector3 deltaMove = m_velocity * deltaTime;
        m_controller.Move(deltaMove);

        // モデルの向き
        UpdateRotation(horizontalVelocity, deltaTime);
    }

    // ==============================
    // 内部処理
    // ==============================
    private void UpdateGrounded()
    {
        bool controllerGrounded = m_controller.isGrounded;

        bool rayHitGrounded = false;
        Vector3 origin = m_controller.transform.position + Vector3.up * 0.1f;
        float rayDistance = m_groundCheckDistance + 0.1f;

        if (Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                m_groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            rayHitGrounded = true;
        }

        m_isGrounded = controllerGrounded || rayHitGrounded;

        if (m_isGrounded && m_velocity.y < 0f)
        {
            // 地面に貼り付けるため、少しだけ下向きに固定
            m_velocity.y = -2f;
        }
    }

    private Vector3 CalculateHorizontalVelocity(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (m_camera != null)
        {
            forward = m_camera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            right = m_camera.transform.right;
            right.y = 0f;
            right.Normalize();
        }

        Vector3 moveDir = forward * moveInput.y + right * moveInput.x;
        if (moveDir.sqrMagnitude > 1f)
        {
            moveDir.Normalize();
        }

        return moveDir * m_moveSpeed;
    }

    private void UpdateVerticalVelocity(bool jumpPressed, float deltaTime)
    {
        if (m_isGrounded)
        {
            if (jumpPressed)
            {
                m_velocity.y = m_jumpPower;
            }
            else if (m_velocity.y < 0f)
            {
                m_velocity.y = -2f;
            }
        }
        else
        {
            float newY = m_velocity.y + m_gravity * deltaTime;
            if (newY < m_terminalVelocity)
            {
                newY = m_terminalVelocity;
            }
            m_velocity.y = newY;
        }
    }

    private void UpdateRotation(Vector3 horizontalVelocity, float deltaTime)
    {
        if (m_modelRoot == null)
        {
            return;
        }

        Vector3 moveDir = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
        if (moveDir.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
        m_modelRoot.rotation = Quaternion.RotateTowards(
            m_modelRoot.rotation,
            targetRot,
            m_rotationSpeed * deltaTime
        );
    }
}
