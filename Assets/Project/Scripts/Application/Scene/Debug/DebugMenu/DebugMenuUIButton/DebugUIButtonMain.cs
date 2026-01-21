#if _DEBUG
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class DebugUIButtonMain : MonoBehaviour
{
    private enum State
    {
        Main,
    }

    [SerializeField] Button m_button;

    private UniTaskStateMachine<State> m_state;
    private CancellationToken m_token;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_token = this.GetCancellationTokenOnDestroy();
        m_state = new(m_token);
        m_state.Add(State.Main, OnStateMainEnter, OnStateMainUpdate);
        m_state.MoveToNextState(State.Main);
    }

    void OnDestroy()
    {
    }

    private async UniTask OnStateMainEnter(CancellationToken token)
    {
     
    }

    
    private void OnStateMainUpdate()
    {
      
    }

    private void Update()
    {
        m_state.Update();
    }

}
#endif