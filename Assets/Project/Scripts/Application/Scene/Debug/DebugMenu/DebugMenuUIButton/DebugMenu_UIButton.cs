#if _DEBUG

using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;


/// <summary>
/// デバッグメニュー・UIButton
/// </summary>
public class DebugMenu_UIButton : IDebugMenu
{
    private AssetLoadHelper.Instantiated m_view;
    

    public async UniTask OnEnter(Transform parent, CancellationToken token)
    {
        m_view = await AssetLoadHelper.InstantiateAsync("DebugUIButton");
        await m_view.Wait();
    }


    public UniTask OnUpdate(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public void OnExit()
    {
        m_view.Dispose();
    }

}


#endif
