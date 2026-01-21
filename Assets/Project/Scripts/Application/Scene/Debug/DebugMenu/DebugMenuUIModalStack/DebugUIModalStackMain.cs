#if _DEBUG
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.DedicatedServer;
using UnityEngine.U2D;
using UnityEngine.UI;
using static SceneTransitionManager;

public class DebugUIModalStackMain : MonoBehaviour
{
    private enum State
    {
        Wait,
        Open01,
        Open02,
        Open03,
    }

    [SerializeField] private UIButton m_btn;

    private UniTaskStateMachine<State> m_state;
    private DebugModalDialog m_dlg01 = null;
    private DebugModalDialog m_dlg02 = null;
    private DebugModalDialog m_dlg03 = null;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_state = new(this.GetCancellationTokenOnDestroy());
        m_state.Add(State.Wait);
        m_state.Add(State.Open01, OnStateOpen01Enter);
        m_state.Add(State.Open02, OnStateOpen02Enter);
        m_state.Add(State.Open03, OnStateOpen03Enter);
        m_state.MoveToNextState(State.Wait);

        m_btn.OnClickProc += OnClickButton;
    }


    void OnDestroy()
    {
        m_btn.OnClickProc -= OnClickButton;
    }

    void Update()
    {
        m_state.Update();
    }


    async UniTask OnStateOpen01Enter(CancellationToken token)
    {
        if (m_dlg01 == null)
        {
            await OpenDialog01(token);
        }
        
        var result = await ShowDialog01(token);
        switch (result)
        {
            case DebugModalDialog.Result.Yes:
                m_state.MoveToNextState(State.Open02);
                break;
            case DebugModalDialog.Result.No:
                CloseDialog01();
                m_state.MoveToNextState(State.Wait);
                break;
        }

       
    }
    async UniTask OnStateOpen02Enter(CancellationToken token)
    {
        if (m_dlg02 == null)
        {
            await OpenDialog02(token);
        }

        var result = await ShowDialog02(token);
        switch (result)
        {
            case DebugModalDialog.Result.Yes:
                m_state.MoveToNextState(State.Open03);
                break;
            case DebugModalDialog.Result.No:
                CloseDialog02();
                m_state.MoveToNextState(State.Open01);      // １つ前のダイアログが開いてるステートに戻す
                break;
        }


    }

    async UniTask OnStateOpen03Enter(CancellationToken token)
    {
        if (m_dlg03 == null)
        {
            await OpenDialog03(token);
        }

        var result = await ShowDialog03(token);
        switch (result)
        {
            case DebugModalDialog.Result.Yes:
                AppDebug.Log(">>> 最上位のダイアログ.");
                break;
            case DebugModalDialog.Result.No:
                CloseDialog03();
                m_state.MoveToNextState(State.Open02);
                break;
        }


    }

    void OnClickButton()
    {
        m_state.MoveToNextState(State.Open01);
    }

    private async UniTask OpenDialog01(CancellationToken token)
    {
        m_dlg01 = await DebugModalDialog.Create(0, false, token);
    }
    private async UniTask<DebugModalDialog.Result> ShowDialog01(CancellationToken token)
    {
        return await m_dlg01.ShowAsync(token);
    }
    private void CloseDialog01()
    {
        m_dlg01.Close();
        m_dlg01 = null;
    }


    private async UniTask OpenDialog02(CancellationToken token)
    {
        m_dlg02 = await DebugModalDialog.Create(1, false, token);
    }
    private async UniTask<DebugModalDialog.Result> ShowDialog02(CancellationToken token)
    {
        return await m_dlg02.ShowAsync(token);
    }
    private void CloseDialog02()
    {
        m_dlg02.Close();
        m_dlg02 = null;
    }

    private async UniTask OpenDialog03(CancellationToken token)
    {
        m_dlg03 = await DebugModalDialog.Create(2, true, token);
    }
    private async UniTask<DebugModalDialog.Result> ShowDialog03(CancellationToken token)
    {
        return await m_dlg03.ShowAsync(token);
    }
    private void CloseDialog03()
    {
        m_dlg03.Close();
        m_dlg03 = null;
    }

}

#endif
