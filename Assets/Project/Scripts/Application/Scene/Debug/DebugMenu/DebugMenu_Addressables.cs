#if _DEBUG

using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

/// <summary>
/// デバッグメニュー・Addressables
/// </summary>
public class DebugMenu_Addressables : IDebugMenu    
{
    private AssetGroupLoader m_groupLoader = new();


    public async UniTask OnEnter(Transform parent, CancellationToken token)
    {
        // 一括読み込み
        string label = "Common";
        var progress = new Progress<float>(p => {
            AppDebug.Log($">>Loading - {p * 100f:0}%");
        });
        await m_groupLoader.LoadAssetsAsync(token, label, OnAssetGroupLoadCompleted).Wait(progress);
        
        var sprAtrlas = m_groupLoader.GetAsset<SpriteAtlas>("common_01");
        var spr = sprAtrlas.GetSprite("button_circle");

        // 単体読み込み
        using var tex = await AssetLoadHelper.LoadAsync<Texture2D>("Texture/BG/bg_01");
        

        


    }


    public UniTask OnUpdate(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public void OnExit()
    {
        m_groupLoader.Release();
    }


    private void OnAssetGroupLoadCompleted(AssetGroupLoader loader)
    {
        loader.DumpAllAssets();
    }

}


#endif
