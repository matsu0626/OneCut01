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

/// <summary>
/// デバッグメニュー・Addressables
/// </summary>
public class DebugMenu_Fade : IDebugMenu
{
    private int m_step = 0;

    public UniTask OnEnter(Transform parent, CancellationToken token)
    {
        m_step = 0;
        return UniTask.CompletedTask;
    }


    public async UniTask OnUpdate(CancellationToken token)
    {
        float duration = 2f;
        
        FadeManager.Instance._DebugDisp();

        switch (m_step)
        {
            case 0:
                FadeManager.Instance.StartFadeOut(duration, Color.yellow);
                await FadeManager.Instance.Wait();
                m_step++;
                break;
            case 1:
                FadeManager.Instance.StartFadeIn(duration, Color.yellow);
                await FadeManager.Instance.Wait();
                m_step++;
                break;
            case 2:
                FadeManager.Instance.StartWhiteOut(duration);
                await FadeManager.Instance.Wait();
                m_step++;
                break;
            case 3:
                FadeManager.Instance.StartWhiteIn(duration);
                await FadeManager.Instance.Wait();
                m_step = 0;
                break;
        }

        
    }

    public void OnExit()
    {
        FadeManager.Instance.Clear();
    }

}


#endif
