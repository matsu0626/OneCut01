using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DebugMenu_3DModel : IDebugMenu
{
    private const string UiPrefabKey = "Debug3DModelUI";
    private const string WorldPrefabKey = "Debug3DModelWorld";

    private Debug3DModelMain m_uiMain;

    private AssetLoadHelper.Instantiated m_uiHandle;
    private AssetLoadHelper.Instantiated m_worldHandle;
    private bool m_hasUI;
    private bool m_hasWorld;

    public async UniTask OnEnter(Transform parent, CancellationToken token)
    {
        m_hasUI = false;
        m_hasWorld = false;
        m_uiMain = null;

        // ) UIプレハブ生成（親はデバッグ用CanvasのSafeAreaとか）
        // 親transformはnullで渡す。内部でAttachToRootSafeAreaを使っているのでそちらで設定される。
        var uiInst = await AssetLoadHelper.InstantiateAsync(UiPrefabKey, null, token);
        var uiGO = uiInst.Instance;
        if (!uiGO)
        {
            uiInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DModel)}] UI Instantiate失敗 key={UiPrefabKey}");
            return;
        }

        m_uiMain = uiGO.GetComponent<Debug3DModelMain>();
        if (!m_uiMain)
        {
            uiInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DModel)}] {UiPrefabKey} に Debug3DModelMain がない");
            return;
        }

        m_uiHandle = uiInst;
        m_hasUI = true;

        // --- 2) 3D側プレハブ生成（親は null＝シーン直下） ---
        var worldInst = await AssetLoadHelper.InstantiateAsync(WorldPrefabKey, null, token);
        var worldGO = worldInst.Instance;
        if (!worldGO)
        {
            worldInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DModel)}] World Instantiate失敗 key={WorldPrefabKey}");
            return;
        }

        var worldRoot = worldGO.GetComponent<Debug3DModelWorldRoot>();
        if (!worldRoot)
        {
            worldInst.Dispose();
            AppDebug.LogError($"[{nameof(DebugMenu_3DModel)}] {WorldPrefabKey} に Debug3DModelWorldRoot がない");
            return;
        }

        m_worldHandle = worldInst;
        m_hasWorld = true;

        // --- 3) UI側に世界情報をセット ---
        m_uiMain.SetupWorld(worldRoot.SpawnRoot, worldRoot.PreviewCamera, worldRoot.KeyLight);

        // --- 4) UIの初期化 ---
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
