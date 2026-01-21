#if _DEBUG
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 3Dキャラクター移動テスト用デバッグメニュー
/// ・UIプレハブとワールドプレハブを生成
/// ・WorldRoot から PlayerRoot / Camera を受け取り、Main に渡す
/// ・OnUpdate で Main.Tick を呼ぶ
/// </summary>
public sealed class DebugMenu_3DCharacterMove : IDebugMenu
{
    private const string UiPrefabKey = "Debug3DCharacterMoveUI";
    private const string WorldPrefabKey = "Debug3DCharacterMoveWorld";

    private Debug3DCharacterMoveMain m_uiMain;

    private AssetLoadHelper.Instantiated m_uiHandle;
    private AssetLoadHelper.Instantiated m_worldHandle;
    private bool m_hasUI;
    private bool m_hasWorld;

    public async UniTask OnEnter(Transform parent, CancellationToken token)
    {
        m_hasUI = false;
        m_hasWorld = false;
        m_uiMain = null;

        // -------------------------
        // 1) UIプレハブ生成
        // -------------------------
        var uiInst = await AssetLoadHelper.InstantiateAsync(UiPrefabKey, null, token);
        var uiGO = uiInst.Instance;
        if (!uiGO)
        {
            uiInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DCharacterMove)}] UI Instantiate失敗 key={UiPrefabKey}");
            return;
        }

        m_uiMain = uiGO.GetComponent<Debug3DCharacterMoveMain>();
        if (!m_uiMain)
        {
            uiInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DCharacterMove)}] {UiPrefabKey} に {nameof(Debug3DCharacterMoveMain)} がありません");
            return;
        }

        m_uiHandle = uiInst;
        m_hasUI = true;

        // -------------------------
        // 2) ワールド側プレハブ生成
        // -------------------------
        var worldInst = await AssetLoadHelper.InstantiateAsync(WorldPrefabKey, null, token);
        var worldGO = worldInst.Instance;
        if (!worldGO)
        {
            worldInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DCharacterMove)}] World Instantiate失敗 key={WorldPrefabKey}");
            return;
        }

        var worldRoot = worldGO.GetComponent<Debug3DCharacterMoveWorldRoot>();
        if (!worldRoot)
        {
            worldInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DCharacterMove)}] {WorldPrefabKey} に {nameof(Debug3DCharacterMoveWorldRoot)} がありません");
            return;
        }

        m_worldHandle = worldInst;
        m_hasWorld = true;

        // -------------------------
        // 3) UI 側に世界情報をセット
        // -------------------------
        m_uiMain.SetupWorld(worldRoot.PlayerRoot, worldRoot.FollowCamera);

        // -------------------------
        // 4) UI 初期化
        // -------------------------
        await m_uiMain.InitializeAsync(token);
    }

    public UniTask OnUpdate(CancellationToken token)
    {
        if (m_uiMain != null)
        {
            m_uiMain.Tick(Time.unscaledDeltaTime);
        }
        return UniTask.CompletedTask;
    }

    public void OnExit()
    {
        if (m_uiMain != null)
        {
            m_uiMain.Dispose();
            m_uiMain = null;
        }

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
