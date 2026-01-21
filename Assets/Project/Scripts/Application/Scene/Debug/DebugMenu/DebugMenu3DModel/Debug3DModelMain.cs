using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug 用 3D モデルプレビュー
/// ・Addressables ラベルで一括ロード（AssetGroupLoader）
/// ・モデル一覧を TMP_Dropdown に表示して選択
/// ・選んだモデルを SpawnRoot に Instantiate して表示
/// ・モデルに付いている Animator から AnimationClip 一覧を拾い、
///   もう一つのドロップダウンで再生アニメを切り替え
/// </summary>
public class Debug3DModelMain : MonoBehaviour
{
    // ================================
    // 3Dプレビュー（ワールド側から渡してもらう）
    // ================================
    [Header("3D Preview (set from world prefab)")]
    [SerializeField] private Transform m_spawnRoot;
    [SerializeField] private Camera m_previewCamera;
    [SerializeField] private Light m_keyLight;

    // ================================
    // パネルの最小化
    // ================================
    [Header("UI - Panel")]
    [SerializeField] private GameObject m_panelContentRoot;      // 中身全部をぶら下げるルート
    [SerializeField] private Button m_panelToggleButton;         // 最小化トグルボタン
    [SerializeField] private TMP_Text m_panelToggleLabel;        // ボタンの表示（▲ / ▼）
    [SerializeField] private RectTransform m_panelRect;          // パネル本体（この Rect を動かす）

    [SerializeField] private float m_expandedHeight = 220f;      // 展開時の高さ（起動時に上書き）
    [SerializeField] private float m_collapsedHeight = 40f;      // 最小化したときの高さ
    [SerializeField] private float m_minimizedBottomMargin = 180f;// 画面下からのオフセット(px)

    private bool m_isMinimized;
    private Vector2 m_expandedAnchoredPos;                       // 展開時の anchoredPosition を記憶

    // ================================
    // UI - Model
    // ================================
    [Header("UI - Model")]
    [SerializeField] private TMP_Dropdown m_modelDropdown;      // モデル一覧
    [SerializeField] private Button m_loadButton;
    [SerializeField] private Button m_unloadButton;
    [SerializeField] private Toggle m_autoRotateToggle;
    [SerializeField] private Slider m_rotateSpeedSlider;
    [SerializeField] private Slider m_scaleSlider;
    [SerializeField] private Toggle m_lightToggle;
    [SerializeField] private TMP_Text m_statusLabel;

    // ================================
    // UI - Animation
    // ================================
    [Header("UI - Animation")]
    [SerializeField] private TMP_Dropdown m_animDropdown;       // アニメーション一覧

    // ================================
    // Addressables
    // ================================
    [Header("Addressables")]
    [SerializeField] private string m_modelLabel = "ModelView"; // ラベル名

    [Header("Camera / Fitting")]
    [SerializeField] private float m_cameraDistanceFactor = 3.0f;

    private CancellationToken m_token;

    // AssetGroupLoader 本体
    private AssetGroupLoader m_groupLoader;

    // モデル名一覧（ドロップダウン表示用）
    private readonly List<string> m_modelNameList = new();

    // 表示中のインスタンス
    private GameObject m_modelInstance;

    // 表示中モデルの Animator
    private Animator m_animator;

    // アニメーション名一覧（AnimationClip 名）
    private readonly List<string> m_animNameList = new();

    private bool m_autoRotate = true;
    private float m_rotateSpeed = 30f;
    private float m_scale = 1f;

    // ================================
    // 外から 3D 側の情報を注入する
    // ================================
    public void SetupWorld(Transform spawnRoot, Camera previewCamera, Light keyLight)
    {
        m_spawnRoot = spawnRoot;
        m_previewCamera = previewCamera;
        m_keyLight = keyLight;
    }

    // ================================
    // ライフサイクル
    // ================================
    public async UniTask InitializeAsync(CancellationToken token)
    {
        m_token = token;

        if (!m_spawnRoot)
        {
            AppDebug.LogError($"[{nameof(Debug3DModelMain)}] m_spawnRoot 未設定");
        }
        if (!m_previewCamera)
        {
            AppDebug.LogError($"[{nameof(Debug3DModelMain)}] m_previewCamera 未設定");
        }

        // パネルの初期位置＆高さを記録
        if (m_panelRect)
        {
            m_expandedAnchoredPos = m_panelRect.anchoredPosition;
            // インスペクタ値より Scene 上の実際の高さを優先
            m_expandedHeight = m_panelRect.sizeDelta.y;
        }

        // UI 初期値
        if (m_autoRotateToggle) m_autoRotateToggle.isOn = true;
        if (m_rotateSpeedSlider) m_rotateSpeedSlider.value = 0.5f; // 0..1
        if (m_scaleSlider) m_scaleSlider.value = 0.5f;
        if (m_lightToggle && m_keyLight)
        {
            m_lightToggle.isOn = m_keyLight.enabled;
        }

        ApplyUIToParams();
        WireUIEvents();

        // 起動時は展開状態スタート
        m_isMinimized = false;
        ApplyPanelMinimizeState();

        // === AssetGroupLoader を使ってラベル一括読み込み ===
        await LoadModelGroupAsync(m_token);

        if (m_modelNameList.Count > 0)
        {
            SetStatus($"Ready. ラベル '{m_modelLabel}' のモデル数: {m_modelNameList.Count}");
        }
        else
        {
            SetStatus($"Ready. ラベル '{m_modelLabel}' のモデルが見つかりません");
        }
    }

    public void Tick(float deltaTime)
    {
        if (!m_modelInstance) return;

        if (m_autoRotate)
        {
            m_modelInstance.transform.Rotate(Vector3.up, m_rotateSpeed * deltaTime, Space.World);
        }
    }

    public void Dispose()
    {
        UnloadModel();

        if (m_loadButton) m_loadButton.onClick.RemoveListener(OnClickLoad);
        if (m_unloadButton) m_unloadButton.onClick.RemoveListener(OnClickUnload);
        if (m_autoRotateToggle) m_autoRotateToggle.onValueChanged.RemoveListener(OnAutoRotateChanged);
        if (m_rotateSpeedSlider) m_rotateSpeedSlider.onValueChanged.RemoveListener(OnRotateSpeedChanged);
        if (m_scaleSlider) m_scaleSlider.onValueChanged.RemoveListener(OnScaleChanged);
        if (m_lightToggle) m_lightToggle.onValueChanged.RemoveListener(OnLightChanged);
        if (m_modelDropdown) m_modelDropdown.onValueChanged.RemoveListener(OnModelDropdownChanged);
        if (m_animDropdown) m_animDropdown.onValueChanged.RemoveListener(OnAnimDropdownChanged);
        if (m_panelToggleButton) m_panelToggleButton.onClick.RemoveListener(OnClickTogglePanel);

        // AssetGroupLoader の解放
        m_groupLoader?.Release();
        m_groupLoader = null;
    }

    // ================================
    // UI wiring
    // ================================
    private void WireUIEvents()
    {
        if (m_loadButton) m_loadButton.onClick.AddListener(OnClickLoad);
        if (m_unloadButton) m_unloadButton.onClick.AddListener(OnClickUnload);

        if (m_autoRotateToggle) m_autoRotateToggle.onValueChanged.AddListener(OnAutoRotateChanged);
        if (m_rotateSpeedSlider) m_rotateSpeedSlider.onValueChanged.AddListener(OnRotateSpeedChanged);
        if (m_scaleSlider) m_scaleSlider.onValueChanged.AddListener(OnScaleChanged);
        if (m_lightToggle) m_lightToggle.onValueChanged.AddListener(OnLightChanged);

        if (m_modelDropdown) m_modelDropdown.onValueChanged.AddListener(OnModelDropdownChanged);
        if (m_animDropdown) m_animDropdown.onValueChanged.AddListener(OnAnimDropdownChanged);

        if (m_panelToggleButton) m_panelToggleButton.onClick.AddListener(OnClickTogglePanel);
    }

    private void OnClickLoad()
    {
        var name = GetSelectedModelName();
        if (string.IsNullOrEmpty(name))
        {
            SetStatus("モデルが選択されていません");
            return;
        }

        LoadModelAsync(name).Forget();
    }

    private void OnClickUnload()
    {
        UnloadModel();
    }

    private void OnAutoRotateChanged(bool _)
    {
        ApplyUIToParams();
    }

    private void OnRotateSpeedChanged(float _)
    {
        ApplyUIToParams();
    }

    private void OnScaleChanged(float _)
    {
        ApplyUIToParams();
    }

    private void OnLightChanged(bool _)
    {
        ApplyUIToParams();
    }

    private void OnModelDropdownChanged(int index)
    {
        // ここではまだ何もしない（Load ボタンでロード）
    }

    private void OnAnimDropdownChanged(int index)
    {
        PlaySelectedAnimation();
    }

    private void OnClickTogglePanel()
    {
        m_isMinimized = !m_isMinimized;
        ApplyPanelMinimizeState();
    }

    private void ApplyUIToParams()
    {
        if (m_autoRotateToggle) m_autoRotate = m_autoRotateToggle.isOn;
        if (m_rotateSpeedSlider) m_rotateSpeed = Mathf.Lerp(0f, 180f, m_rotateSpeedSlider.value);
        if (m_scaleSlider) m_scale = Mathf.Lerp(0.1f, 3.0f, m_scaleSlider.value);

        if (m_lightToggle && m_keyLight)
        {
            m_keyLight.enabled = m_lightToggle.isOn;
        }

        if (m_modelInstance)
        {
            m_modelInstance.transform.localScale = Vector3.one * m_scale;
        }
    }

    /// <summary>
    /// パネルの最小化／展開の見た目を反映
    /// </summary>
    private void ApplyPanelMinimizeState()
    {
        if (m_panelContentRoot)
        {
            m_panelContentRoot.SetActive(!m_isMinimized);
        }

        if (m_panelToggleLabel)
        {
            m_panelToggleLabel.text = m_isMinimized ? "▲" : "▼";
        }

        if (!m_panelRect) return;

        // 高さを切り替え
        var size = m_panelRect.sizeDelta;
        size.y = m_isMinimized ? m_collapsedHeight : m_expandedHeight;
        m_panelRect.sizeDelta = size;

        // 位置を計算（アンカーが中央の前提）
        var parentRect = m_panelRect.parent as RectTransform;
        if (parentRect == null) return;

        if (!m_isMinimized)
        {
            // 展開時は元の位置に戻す
            m_panelRect.anchoredPosition = m_expandedAnchoredPos;
        }
        else
        {
            float parentH = parentRect.rect.height;
            float selfH = m_panelRect.rect.height;
            float margin = m_minimizedBottomMargin;

            // 下から margin 分だけ上に、という y を計算（中央アンカー前提）
            float y = -parentH * 0.5f + selfH * 0.5f + margin;
            m_panelRect.anchoredPosition = new Vector2(m_expandedAnchoredPos.x, y);
        }
    }

    // ================================
    // AssetGroupLoader でラベル一括ロード
    // ================================
    private async UniTask LoadModelGroupAsync(CancellationToken token)
    {
        m_modelNameList.Clear();

        if (m_groupLoader != null)
        {
            m_groupLoader.Release();
            m_groupLoader = null;
        }

        if (string.IsNullOrEmpty(m_modelLabel))
        {
            SetStatus("モデルラベルが設定されていません");
            return;
        }

        try
        {
            m_groupLoader = new AssetGroupLoader();
            m_groupLoader.LoadAssetsAsync(token, m_modelLabel);

            SetStatus($"Loading group... label = {m_modelLabel}");
            await m_groupLoader.Wait();

            var count = m_groupLoader.GetAssetCount();
            if (count <= 0)
            {
                SetStatus($"ラベル '{m_modelLabel}' のモデルが 0 件です");
                return;
            }

            var fileNames = m_groupLoader.GetFileNameList();
            m_modelNameList.AddRange(fileNames);

            if (m_modelDropdown)
            {
                m_modelDropdown.ClearOptions();

                var options = new List<TMP_Dropdown.OptionData>();
                foreach (var name in m_modelNameList)
                {
                    options.Add(new TMP_Dropdown.OptionData(name));
                }

                m_modelDropdown.AddOptions(options);
                m_modelDropdown.value = 0;
                m_modelDropdown.RefreshShownValue();
            }

#if _DEBUG
            m_groupLoader.DumpAllAssets();
#endif

            SetStatus($"Loaded group. label = {m_modelLabel}, count = {count}");
        }
        catch (OperationCanceledException)
        {
            SetStatus("モデルグループの読み込みがキャンセルされました");
        }
        catch (Exception e)
        {
            AppDebug.LogException(e);
            SetStatus($"モデルグループ読み込みで例外発生: {e.GetType().Name}");
        }
    }

    // ドロップダウンの選択モデル名を返す
    private string GetSelectedModelName()
    {
        if (m_modelDropdown && m_modelNameList.Count > 0)
        {
            var index = Mathf.Clamp(m_modelDropdown.value, 0, m_modelNameList.Count - 1);
            return m_modelNameList[index];
        }

        return string.Empty;
    }

    // ================================
    // モデルのロード / アンロード
    // ================================
    private async UniTask LoadModelAsync(string fileName)
    {
        try
        {
            if (m_groupLoader == null)
            {
                SetStatus("グループがまだロードされていません");
                return;
            }

            UnloadModel();

            SetStatus($"Loading model: {fileName}");

            var prefab = m_groupLoader.GetAsset<GameObject>(fileName);
            if (!prefab)
            {
                SetStatus($"モデルが見つかりません: {fileName}");
                return;
            }

            GameObject instance;
            if (m_spawnRoot)
            {
                instance = GameObject.Instantiate(prefab, m_spawnRoot, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                instance = GameObject.Instantiate(prefab);
            }

            m_modelInstance = instance;
            m_modelInstance.transform.localScale = Vector3.one * m_scale;

            FitToCamera(m_modelInstance);

            // モデルに付いている Animator からアニメ一覧を構築
            SetupAnimator(m_modelInstance);

            SetStatus($"Loaded: {fileName}");

            await UniTask.Yield();
        }
        catch (OperationCanceledException)
        {
            SetStatus("モデル読み込みがキャンセルされました");
        }
        catch (Exception e)
        {
            AppDebug.LogException(e);
            SetStatus($"モデル読み込みで例外発生: {e.GetType().Name}");
        }
    }

    private void UnloadModel()
    {
        if (m_modelInstance)
        {
            GameObject.Destroy(m_modelInstance);
            m_modelInstance = null;
        }

        m_animator = null;
        m_animNameList.Clear();

        if (m_animDropdown)
        {
            m_animDropdown.ClearOptions();
            m_animDropdown.RefreshShownValue();
        }

        SetStatus("Unloaded");
    }

    // ================================
    // Animator / AnimationClip 周り
    // ================================
    private void SetupAnimator(GameObject root)
    {
        m_animator = null;
        m_animNameList.Clear();

        if (m_animDropdown)
        {
            m_animDropdown.ClearOptions();
        }

        if (!root)
        {
            return;
        }

        var animator = root.GetComponentInChildren<Animator>(true);
        if (!animator || animator.runtimeAnimatorController == null)
        {
            SetStatus("Animator または Controller が見つかりません");
            return;
        }

        m_animator = animator;

        var controller = m_animator.runtimeAnimatorController;
        var clips = controller.animationClips;

        if (clips == null || clips.Length == 0)
        {
            SetStatus("Animator に AnimationClip がありません");
            return;
        }

        foreach (var clip in clips)
        {
            var clipName = clip != null ? clip.name : null;
            if (string.IsNullOrEmpty(clipName))
            {
                continue;
            }

            if (!m_animNameList.Contains(clipName))
            {
                m_animNameList.Add(clipName);
            }
        }

        if (m_animNameList.Count == 0)
        {
            SetStatus("有効な AnimationClip が見つかりません");
            return;
        }

        if (m_animDropdown)
        {
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (var name in m_animNameList)
            {
                options.Add(new TMP_Dropdown.OptionData(name));
            }

            m_animDropdown.AddOptions(options);
            m_animDropdown.value = 0;
            m_animDropdown.RefreshShownValue();
        }

        // 先頭のアニメをとりあえず再生
        PlaySelectedAnimation();
    }

    private void PlaySelectedAnimation()
    {
        if (!m_animator) return;
        if (m_animNameList.Count == 0) return;
        if (!m_animDropdown) return;

        var index = Mathf.Clamp(m_animDropdown.value, 0, m_animNameList.Count - 1);
        var clipName = m_animNameList[index];

        m_animator.Play(clipName, 0, 0f);

        SetStatus($"Play Animation: {clipName}");
    }

    // ================================
    // 補助：カメラ調整・Bounds計算
    // ================================
    private void FitToCamera(GameObject root)
    {
        if (!root || !m_previewCamera) return;

        var bounds = CalculateBounds(root);
        if (bounds.size.sqrMagnitude <= 0.0001f) return;

        if (m_spawnRoot)
        {
            var offset = bounds.center;
            var localOffset = m_spawnRoot.InverseTransformPoint(offset);
            root.transform.localPosition -= localOffset;

            var b2 = CalculateBounds(root);
            var bottom = b2.min.y;
            root.transform.position += new Vector3(0f, -bottom, 0f);
        }

        var radius = bounds.extents.magnitude;
        var dist = Mathf.Max(0.5f, radius * m_cameraDistanceFactor);

        var center = bounds.center;
        m_previewCamera.transform.position = center - m_previewCamera.transform.forward * dist;
        m_previewCamera.transform.LookAt(center);
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.zero);
        }

        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    private void SetStatus(string msg)
    {
        if (m_statusLabel) m_statusLabel.text = msg;
        AppDebug.Log($"[{nameof(Debug3DModelMain)}] {msg}");
    }
}
