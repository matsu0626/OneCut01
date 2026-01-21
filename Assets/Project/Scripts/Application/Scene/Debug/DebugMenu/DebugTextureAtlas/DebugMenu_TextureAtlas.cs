#if _DEBUG

using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;


/// <summary>
/// デバッグメニュー・TextureAtlas
/// </summary>
public class DebugMenu_TextureAtlas : IDebugMenu
{
    private AssetLoadHelper.Instantiated m_view;

    public async UniTask OnEnter(Transform parent, CancellationToken token)
    {
        m_view = await AssetLoadHelper.InstantiateAsync("DebugTextureAtlas");
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
