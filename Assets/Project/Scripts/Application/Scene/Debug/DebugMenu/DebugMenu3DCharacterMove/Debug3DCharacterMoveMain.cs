#if _DEBUG
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 3Dキャラクター移動テスト（DebugMenu 用）
/// ・World プレハブから PlayerRoot / カメラを受け取る
/// ・CharacterMotor3D に移動ロジックを委譲する
/// ・入力は GameKeyboard から取得
/// ・状態は DebugPrint で画面表示
/// </summary>
public sealed class Debug3DCharacterMoveMain : MonoBehaviour
{
    // ================================
    // World 側から渡してもらうもの
    // ================================
    [Header("World")]
    [SerializeField] private CharacterController m_controller;
    [SerializeField] private Transform m_characterModelRoot;  // 見た目の向きを変えたい場合のルート
    [SerializeField] private Camera m_camera;                 // 参照カメラ（なければ Camera.main）

    // ================================
    // キャラ移動モーター（フレームワーク側ロジック）
    // ================================
    [Header("Motor (Framework)")]
    [SerializeField] private CharacterMotor3D m_motor = new CharacterMotor3D();

    // ================================
    // Debug 表示
    // ================================
    [Header("Debug View")]
    [SerializeField] private bool m_useDebugPrint = true;

    private CancellationToken m_token;

    // ================================
    // World 設定（DebugMenu から呼ぶ想定）
    // ================================
    /// <summary>
    /// World プレハブ側から PlayerRoot / カメラを注入する。
    /// </summary>
    public void SetupWorld(Transform playerRoot, Camera followCamera)
    {
        if (playerRoot != null)
        {
            // CharacterController を探す
            m_controller = playerRoot.GetComponent<CharacterController>();
            if (!m_controller)
            {
                AppDebug.LogError($"[{nameof(Debug3DCharacterMoveMain)}] PlayerRoot に CharacterController が付いていません");
            }

            // モデルルート未設定なら、とりあえず PlayerRoot を使う
            if (!m_characterModelRoot)
            {
                m_characterModelRoot = playerRoot;
            }
        }

        if (followCamera != null)
        {
            m_camera = followCamera;
        }
        else if (!m_camera && Camera.main != null)
        {
            // カメラ未指定なら、最悪 Camera.main を使う
            m_camera = Camera.main;
        }
    }

    // ================================
    // 初期化（DebugMenu.OnEnter から呼ばれる想定）
    // ================================
    public UniTask InitializeAsync(CancellationToken token)
    {
        m_token = token;

        if (!m_controller)
        {
            AppDebug.LogError($"[{nameof(Debug3DCharacterMoveMain)}] m_controller 未設定");
        }
        if (!m_characterModelRoot)
        {
            AppDebug.LogError($"[{nameof(Debug3DCharacterMoveMain)}] m_characterModelRoot 未設定");
        }
        if (!m_camera)
        {
            AppDebug.LogError($"[{nameof(Debug3DCharacterMoveMain)}] m_camera 未設定");
        }

        // モーター初期化（以降の移動ロジックは CharacterMotor3D 側に委譲）
        m_motor.Setup(m_controller, m_characterModelRoot, m_camera);

        return UniTask.CompletedTask;
    }

    // ================================
    // 毎フレーム更新（DebugMenu.OnUpdate から呼ぶ想定）
    // ================================
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        if (!m_controller)
        {
            return;
        }

        // 入力取得（GameKeyboard から）
        Vector2 moveInput = GetMoveInputFromGameKeyboard();
        bool jumpPressed = GetJumpFromGameKeyboard();

        // モーター更新
        m_motor.Tick(moveInput, jumpPressed, deltaTime);

        // Debug 表示
        if (m_useDebugPrint && DebugPrint.Instance != null)
        {
            var vel = m_motor.Velocity;
            DebugPrint.Instance.PrintLine(
                10f,
                0,
                $"3DMove Grounded={m_motor.IsGrounded} " +
                $"Vel=({vel.x:F2},{vel.y:F2},{vel.z:F2}) " +
                $"Input=({moveInput.x:F2},{moveInput.y:F2})"
            );
        }
    }

    // ================================
    // 終了処理（DebugMenu.OnExit から呼ぶ想定）
    // ================================
    public void Dispose()
    {
        // いまのところ特に解放対象はないけど、
        // 将来イベント購読などを足したらここで解除する。
    }

    // ================================
    // 入力まわり（GameKeyboard をラップ）
    // ================================
    private Vector2 GetMoveInputFromGameKeyboard()
    {
        var kb = GameKeyboard.Instance;
        if (kb == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (kb.isPress(GameKeyboard.Code.Up))
        {
            input.y += 1f;
        }
        if (kb.isPress(GameKeyboard.Code.Down))
        {
            input.y -= 1f;
        }
        if (kb.isPress(GameKeyboard.Code.Left))
        {
            input.x -= 1f;
        }
        if (kb.isPress(GameKeyboard.Code.Right))
        {
            input.x += 1f;
        }

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        return input;
    }

    private bool GetJumpFromGameKeyboard()
    {
        var kb = GameKeyboard.Instance;
        if (kb == null)
        {
            return false;
        }

        // ひとまず Enter をジャンプトリガーにしておく
        return kb.isTrg(GameKeyboard.Code.Enter);
    }
}
#endif
