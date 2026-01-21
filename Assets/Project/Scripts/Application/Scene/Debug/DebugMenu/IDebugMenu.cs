#if _DEBUG
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// デバッグメニュー用インターフェース
/// </summary>
public interface IDebugMenu
{
    public UniTask OnEnter(Transform parent, CancellationToken token);
    public UniTask OnUpdate(CancellationToken token);
    public void OnExit();

}


#endif
