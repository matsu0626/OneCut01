#if _DEBUG
using System;
using UnityEngine;

/// <summary>
/// デバッグ用カメラ制御（グローバル・シングルトン）
/// ・Enable 時に対象カメラの状態を退避して操作
/// ・Disable 時に元の状態を戻す
/// ・右ドラッグ：オービット回転
/// ・中ドラッグ：パン
/// ・ホイール：ズーム
/// ・1（GameKeyboard.DebugCameraToggle）：オン／オフトグル
/// 表示情報は DebugPrint 経由で画面左上に出す
/// </summary>
public sealed class DebugCamera : SingletonMonoBehaviour<DebugCamera>
{
    [Header("Target Camera")]
    [SerializeField]
    private Camera m_cameraOverride;     // 指定があればこのカメラを使用

    [Header("Settings (optional SO)")]
    [SerializeField]
    private DebugCameraSettings m_settings; // あればこちらを優先

    // フォールバック用（SO が無いときに使う値）
    [Header("Fallback Settings (used when Settings is null)")]
    [SerializeField]
    private float m_fallbackOrbitRotateSpeed = 0.25f;   // deg / pixel
    [SerializeField]
    private float m_fallbackOrbitZoomSpeed = 0.5f;    // units / scroll
    [SerializeField]
    private float m_fallbackPanSpeed = 0.003f;  // units / (pixel * dist)
    [SerializeField]
    private float m_fallbackMinDistance = 0.5f;
    [SerializeField]
    private float m_fallbackMaxDistance = 30f;

    [Header("Misc")]
    [SerializeField]
    private bool m_startActive = false;

    // 操作対象カメラ
    private Camera m_targetCamera;
    private bool m_isActive;
    private bool m_hasOriginalState;

    // 退避用
    private Vector3 m_originalPosition;
    private Quaternion m_originalRotation;
    private float m_originalFov;
    private bool m_originalOrthographic;
    private float m_originalOrthoSize;

    // オービット状態
    private Vector3 m_targetPoint;   // 注視点
    private float m_distance;      // カメラ〜注視点距離
    private float m_yaw;           // Y軸まわり角度
    private float m_pitch;         // X軸まわり角度

    //========================================================
    // プロパティ（SO があればそちらを優先）
    //========================================================

    private float OrbitRotateSpeed =>
        m_settings ? m_settings.orbitRotateSpeed : m_fallbackOrbitRotateSpeed;

    private float OrbitZoomSpeed =>
        m_settings ? m_settings.orbitZoomSpeed : m_fallbackOrbitZoomSpeed;

    private float PanSpeed =>
        m_settings ? m_settings.panSpeed : m_fallbackPanSpeed;

    private float MinDistance =>
        m_settings ? m_settings.minDistance : m_fallbackMinDistance;

    private float MaxDistance =>
        m_settings ? m_settings.maxDistance : m_fallbackMaxDistance;

    //========================================================
    // ライフサイクル
    //========================================================

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        DontDestroyOnLoad(gameObject);

        if (m_startActive)
        {
            Enable();
        }
    }

    private void Update()
    {
        var keyboard = GameKeyboard.Instance;
        var mouse = DebugMouse.Instance;

        // 入力ラッパーがまだいなければ何もしない
        if (!keyboard || !mouse || !mouse.HasMouse)
        {
            return;
        }

        // トグルキー（GameKeyboard 側で F1 にバインド）
        if (keyboard.isTrg(GameKeyboard.Code.DebugCameraToggle))
        {
            Toggle();
        }

        if (m_isActive && m_targetCamera)
        {
            TickInput(mouse);

            if (DebugPrint.Instance && m_targetCamera)
            {
                OutputOverlay();
            }
        }
    }

    //========================================================
    // 入力処理
    //========================================================

    private void TickInput(DebugMouse mouse)
    {
        Vector2 delta = mouse.Delta;
        float scroll = mouse.ScrollY;

        // 右ボタン：オービット回転
        if (mouse.RightPressed)
        {
            // 1px あたり OrbitRotateSpeed[deg] 回す
            m_yaw += delta.x * OrbitRotateSpeed;
            m_pitch -= delta.y * OrbitRotateSpeed;
            m_pitch = Mathf.Clamp(m_pitch, -80f, 80f);
        }

        // 中ボタン：パン
        if (mouse.MiddlePressed)
        {
            float dx = delta.x;
            float dy = delta.y;

            var cam = m_targetCamera.transform;
            var right = cam.right;
            var up = cam.up;

            // 距離に比例して移動量を増やす（距離が遠いほど大きく動く）
            float scale = PanSpeed * Mathf.Max(m_distance, 0.01f);

            m_targetPoint -= right * (dx * scale);
            m_targetPoint -= up * (dy * scale);
        }

        // ホイール：ズーム
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            m_distance -= scroll * OrbitZoomSpeed;
            m_distance = Mathf.Clamp(m_distance, MinDistance, MaxDistance);
        }

        UpdateCameraTransform();
    }

    private void UpdateCameraTransform()
    {
        if (!m_targetCamera)
        {
            return;
        }

        Quaternion rot = Quaternion.Euler(m_pitch, m_yaw, 0f);
        Vector3 offset = rot * new Vector3(0f, 0f, m_distance);

        Vector3 camPos = m_targetPoint + offset;
        Transform camTr = m_targetCamera.transform;

        camTr.position = camPos;
        camTr.rotation = Quaternion.LookRotation(m_targetPoint - camPos, Vector3.up);
    }

    //========================================================
    // 公開 API
    //========================================================

    public static void Toggle()
    {
        if (!Instance)
        {
            return;
        }

        if (Instance.m_isActive)
        {
            Instance.Disable();
        }
        else
        {
            Instance.Enable();
        }
    }

    public static void SetActive(bool active)
    {
        if (!Instance)
        {
            return;
        }

        if (active)
        {
            Instance.Enable();
        }
        else
        {
            Instance.Disable();
        }
    }

    public void Enable()
    {
        if (m_isActive)
        {
            return;
        }

        m_targetCamera = ResolveTargetCamera();
        if (!m_targetCamera)
        {
            AppDebug.LogWarning("[DebugCamera] 有効なカメラが見つかりません");
            return;
        }

        Transform camTr = m_targetCamera.transform;

        // 元状態退避
        m_originalPosition = camTr.position;
        m_originalRotation = camTr.rotation;
        m_originalFov = m_targetCamera.fieldOfView;
        m_originalOrthographic = m_targetCamera.orthographic;
        m_originalOrthoSize = m_targetCamera.orthographicSize;
        m_hasOriginalState = true;

        // デバッグ中はとりあえず透視投影にそろえる
        m_targetCamera.orthographic = false;

        InitOrbitFromCurrentCamera();

        m_isActive = true;
        AppDebug.Log($"[DebugCamera] Enabled (camera={m_targetCamera.name})");
    }

    public void Disable()
    {
        if (!m_isActive)
        {
            return;
        }

        if (m_targetCamera && m_hasOriginalState)
        {
            Transform camTr = m_targetCamera.transform;

            camTr.position = m_originalPosition;
            camTr.rotation = m_originalRotation;
            m_targetCamera.fieldOfView = m_originalFov;
            m_targetCamera.orthographic = m_originalOrthographic;
            m_targetCamera.orthographicSize = m_originalOrthoSize;
        }

        m_isActive = false;
        m_targetCamera = null;
        m_hasOriginalState = false;

        AppDebug.Log("[DebugCamera] Disabled");
    }

    //========================================================
    // カメラ決定・オービット初期化
    //========================================================

    private Camera ResolveTargetCamera()
    {
        if (m_cameraOverride)
        {
            return m_cameraOverride;
        }

        Camera[] cams = Camera.allCameras;
        Camera best = null;
        float bestDepth = float.NegativeInfinity;

        foreach (Camera cam in cams)
        {
            if (!cam) continue;
            if (!cam.enabled) continue;
            if (!cam.gameObject.activeInHierarchy) continue;
            if (cam.targetTexture != null) continue; // RT 用カメラは除外

            if (cam.depth > bestDepth)
            {
                bestDepth = cam.depth;
                best = cam;
            }
        }

        if (!best)
        {
            best = Camera.main;
        }

        return best;
    }

    private void InitOrbitFromCurrentCamera()
    {
        Transform camTr = m_targetCamera.transform;

        // 仮の注視点：カメラ前方 5m
        m_targetPoint = camTr.position + camTr.forward * 5f;

        Vector3 dir = (camTr.position - m_targetPoint).normalized;
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = new Vector3(0f, 0f, 1f);
        }

        m_distance = Vector3.Distance(camTr.position, m_targetPoint);
        m_distance = Mathf.Clamp(m_distance, MinDistance, MaxDistance);

        m_yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float horizLen = new Vector2(dir.x, dir.z).magnitude;
        // dir: target -> camera。UnityのEuler(pitch,yaw,0) は「上向きピッチが正」なので dir から求めた値は符号反転する
        m_pitch = -Mathf.Atan2(dir.y, horizLen) * Mathf.Rad2Deg;
        m_pitch = Mathf.Clamp(m_pitch, -80f, 80f);
        

        UpdateCameraTransform();
    }

    //========================================================
    // HUD 出力（DebugPrint）
    //========================================================

    private void OutputOverlay()
    {
        DebugPrint dp = DebugPrint.Instance;
        if (!dp || !m_targetCamera)
        {
            return;
        }

        Transform camTr = m_targetCamera.transform;
        Vector3 pos = camTr.position;
        Vector3 rot = camTr.rotation.eulerAngles;

        float x = 40f;
        int line = 3;

        dp.PrintLine(x, line++, "[DebugCamera] " + (m_isActive ? "ON" : "OFF") + " (Toggle: 1)");
        dp.PrintLine(x, line++, $"Camera: {m_targetCamera.name}  depth:{m_targetCamera.depth:F1}");
        dp.PrintLine(x, line++, $"Pos:    {pos.x:F2}, {pos.y:F2}, {pos.z:F2}");
        dp.PrintLine(x, line++, $"Rot:    {rot.x:F1}, {rot.y:F1}, {rot.z:F1}");
        dp.PrintLine(x, line++, $"Target: {m_targetPoint.x:F2}, {m_targetPoint.y:F2}, {m_targetPoint.z:F2}");
        dp.PrintLine(x, line++, $"Dist:   {m_distance:F2}");
    }
}
#endif
