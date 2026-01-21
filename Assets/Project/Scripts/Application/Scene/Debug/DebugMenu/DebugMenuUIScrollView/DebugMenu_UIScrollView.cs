#if _DEBUG

using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;


/// <summary>
/// デバッグメニュー・UIScrollView
/// </summary>
public class DebugMenu_UIScrollView : IDebugMenu
{
    private AssetLoadHelper.Instantiated m_viewV;
    private AssetLoadHelper.Instantiated m_viewH;


    public async UniTask OnEnter(Transform parent, CancellationToken token)
    {
        m_viewV = await AssetLoadHelper.InstantiateAsync("DebugUIScrollViewV");
        m_viewH = await AssetLoadHelper.InstantiateAsync("DebugUIScrollViewH");
        await m_viewV.Wait();
        await m_viewH.Wait();
    }


    public UniTask OnUpdate(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public void OnExit()
    {
        m_viewV.Dispose();
        m_viewH.Dispose();
    }

}


#endif
