#if _DEBUG
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 3D ライト調整用デバッグメニュー
/// ・UI プレハブと World プレハブを生成
/// ・WorldRoot から Directional/Lamp/Bloom を受け取って UI に渡す
/// </summary>
public sealed class DebugMenu_3DLight : IDebugMenu
{
    // Addressables のキー名は実際の設定に合わせて調整してください
    private const string UiPrefabKey = "Debug3DLightUI";
    private const string WorldPrefabKey = "Debug3DLightWorldRoot";

    private Debug3DLightUI m_ui;

    private AssetLoadHelper.Instantiated m_uiHandle;
    private AssetLoadHelper.Instantiated m_worldHandle;
    private bool m_hasUI;
    private bool m_hasWorld;

    public async UniTask OnEnter(Transform parent, CancellationToken token)
    {
        m_hasUI = false;
        m_hasWorld = false;
        m_ui = null;

        // -------------------------
        // 1) UI プレハブ生成
        // -------------------------
        var uiInst = await AssetLoadHelper.InstantiateAsync(UiPrefabKey, null, token);
        var uiGO = uiInst.Instance;
        if (!uiGO)
        {
            uiInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DLight)}] UI Instantiate失敗 key={UiPrefabKey}");
            return;
        }

        m_ui = uiGO.GetComponent<Debug3DLightUI>();
        if (!m_ui)
        {
            uiInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DLight)}] {UiPrefabKey} に {nameof(Debug3DLightUI)} がありません");
            return;
        }

        m_uiHandle = uiInst;
        m_hasUI = true;

        // -------------------------
        // 2) World プレハブ生成
        // -------------------------
        var worldInst = await AssetLoadHelper.InstantiateAsync(WorldPrefabKey, null, token);
        var worldGO = worldInst.Instance;
        if (!worldGO)
        {
            worldInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DLight)}] World Instantiate失敗 key={WorldPrefabKey}");
            return;
        }

        var worldRoot = worldGO.GetComponent<Debug3DLightWorldRoot>();
        if (!worldRoot)
        {
            worldInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DLight)}] {WorldPrefabKey} に {nameof(Debug3DLightWorldRoot)} がありません");
            return;
        }

        m_worldHandle = worldInst;
        m_hasWorld = true;

        // -------------------------
        // 3) UI 側に World 情報をセット
        // -------------------------
        m_ui.Setup(worldRoot, token);
    }

    public UniTask OnUpdate(CancellationToken token)
    {
        // ライト調整 UI はイベント駆動のみで毎フレーム処理は特になし
        return UniTask.CompletedTask;
    }

    public void OnExit()
    {
        // UI 参照クリア
        m_ui = null;

        if (m_hasUI)
        {
            m_uiHandle.Dispose();
            m_hasUI = false;
        }

        if (m_hasWorld)
        {
            m_worldHandle.Dispose();
            m_hasWorld = false;
        }
    }
}
#endif
