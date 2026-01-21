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

public class DebugUIDialogMain : MonoBehaviour
{
    private enum State
    {
        Wait,
        Open,
    }

    [SerializeField] private UIButton m_btn;

    private UniTaskStateMachine<State> m_state;
    private ConfirmDialog m_dlg;
    private int m_dlgCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_state = new(this.GetCancellationTokenOnDestroy());
        m_state.Add(State.Wait);
        m_state.Add(State.Open, OnStateOpenEnter);
        m_state.MoveToNextState(State.Wait);
        m_dlgCount = 0;

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


    async UniTask OnStateOpenEnter(CancellationToken token)
    {
        await OpenDialog(token);
        var result = await ShowDialog(token);
        switch (result)
        {
            case ConfirmDialog.Result.Yes:
                AppDebug.Log(">>> Yes.");
                break;
            case ConfirmDialog.Result.No:
                AppDebug.Log(">>> No.");
                break;
        }

        m_state.MoveToNextState(State.Wait);
    }

 

    void OnClickButton()
    {
        m_state.MoveToNextState(State.Open);
    }

    private async UniTask OpenDialog(CancellationToken token)
    {
        m_dlgCount++;
        ConfirmDialog.ButtonMode mode = m_dlgCount % 2 == 0 ? ConfirmDialog.ButtonMode.Yes : ConfirmDialog.ButtonMode.YesNo;
        m_dlg = await ConfirmDialog.Create("Fix", "FIX_TEST_OBJECTID", mode, token, new { id = m_dlgCount });
    }
    private async UniTask<ConfirmDialog.Result> ShowDialog(CancellationToken token)
    {
        return await m_dlg.ShowAsync(token);
    }
    
}

#endif
