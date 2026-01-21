#if _DEBUG

using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using SceneId = SceneTransitionManager.SceneId;

/// <summary>
/// デバッグメニュー・EXIT（ゲームへ）
/// </summary>
public class DebugMenu_EXIT : IDebugMenu
{

    public async UniTask OnEnter(Transform parent, CancellationToken token)
    {
        await SceneTransitionManager.Instance.Change(SceneTransitionManager.Instance.FirstScene);
    }


    public UniTask OnUpdate(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public void OnExit()
    {
    }

}


#endif
