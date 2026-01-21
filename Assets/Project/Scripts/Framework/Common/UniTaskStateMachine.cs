using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface UnitaskState
{
    UniTask Enter();
    void Update();
    void Exit();
}

public class UniTaskStateMachine<T>
{
    /// <summary>
    /// ステート
    /// </summary>
    public class UniTaskState : UnitaskState
    {
        private readonly UniTaskAction m_enterAct; // 開始時に呼び出されるデリゲート
        private readonly Action m_updateAct; // 更新時に呼び出されるデリゲート
        private readonly Action m_exitAct; // 終了時に呼び出されるデリゲート

        private bool m_isUniTaskExec;
        private CancellationToken m_parentToken; //親オブジェクトから伝播してきたCancellationToken
        private CancellationTokenSource m_localTokenSource = null; //ステート内から関数を停止する際に用いる
        private CancellationTokenSource m_linkedTokenSource = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public UniTaskState(CancellationToken token, UniTaskAction enterAct = null, Action updateAct = null, Action exitAct = null)
        {
            m_parentToken = token;
            m_enterAct = enterAct;
            m_updateAct = updateAct ?? delegate { };
            m_exitAct = exitAct ?? delegate { };
            if (m_localTokenSource != null)
            {
                m_localTokenSource.Cancel();
                m_localTokenSource.Dispose();
            }
            m_localTokenSource = new CancellationTokenSource();
        }
        /// <summary>
        /// 開始時UniTask呼び出し.
        /// </summary>
        /// <returns></returns>
        private async UniTask CallEnterUniTask(CancellationToken token)
        {
            m_isUniTaskExec = true;
            await m_enterAct(token);
            await UniTask.Yield(token);
            m_isUniTaskExec = false;
        }
        /// <summary>
        /// 開始します
        /// </summary>
        public async UniTask Enter()
        {
            if (m_enterAct == null) { return; }

            if (m_localTokenSource != null)
            {
                m_localTokenSource.Cancel();
                m_localTokenSource.Dispose();
            }
            m_localTokenSource = new CancellationTokenSource();

            if (m_linkedTokenSource != null)
            {
                m_linkedTokenSource.Cancel();
                m_linkedTokenSource.Dispose();
                m_linkedTokenSource = null;
            }
            m_linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(m_parentToken, m_localTokenSource.Token);
            await CallEnterUniTask(m_linkedTokenSource.Token);
        }
        /// <summary>
        /// 更新時updateAct処理
        /// </summary>
        public void Update()
        {
            if (m_isUniTaskExec) { return; }

            m_updateAct();
        }
        /// <summary>
        /// 終了時exitAct処理
        /// </summary>
        public void Exit()
        {
            if (m_isUniTaskExec)
            {
                if (m_localTokenSource != null && m_localTokenSource.Token.CanBeCanceled)
                {
                    m_localTokenSource.Cancel();
                    m_localTokenSource.Dispose();
                    m_localTokenSource = null;
                }
                if (m_linkedTokenSource != null && m_linkedTokenSource.Token.CanBeCanceled)
                {
                    m_linkedTokenSource.Cancel();
                    m_linkedTokenSource.Dispose();
                    m_linkedTokenSource = null;
                }
            }
            m_exitAct();
        }
    }

    public delegate UniTask UniTaskAction(CancellationToken token);
    private CancellationToken m_token;
    private Dictionary<T, UnitaskState> m_stateTable = new Dictionary<T, UnitaskState>(); // ステートのテーブル
    private UnitaskState m_currentState; // 現在のステート
    private bool m_enableTransiteToSameState; // 同じステートへの遷移を有効にするフラグを設定
    private bool m_isExitActExec; // Exitでステート遷移の呼び出しを防ぐフラグ

    public T CurrentState // 現在のステート
    {
        get
        {
            // ValueからKeyを検索
            foreach (KeyValuePair<T, UnitaskState> pair in m_stateTable)
            {
                if (pair.Value == m_currentState)
                {
                    return pair.Key;
                }
            }
            return m_stateTable.First().Key;
        }
    }
    /// <summary>
    /// コンストラクタ.
    /// </summary>
    /// <param name="obj">操作対象オブジェクト</param>
    public UniTaskStateMachine(CancellationToken token, bool isEnableTransiteToSameState = false)
    {
        m_token = token;
        m_enableTransiteToSameState = isEnableTransiteToSameState;
    }
    /// <summary>
    /// ステートを追加します
    /// </summary>
    public void Add(T key, UniTaskAction enterAct = null, Action updateAct = null, Action exitAct = null)
    {
        if (!m_stateTable.TryAdd(key, new UniTaskState(m_token, enterAct, updateAct, exitAct)))
        {
            AppDebug.Log(key + "が既にStateMachine追加されているためスキップします");
        }
    }
    /// <summary>
    /// 次のステートを移行して実行する
    /// </summary>
    public void MoveToNextState(T key)
    {
        if (!m_stateTable.TryGetValue(key, out var state))
        {
            AppDebug.Assert(false, "遷移先State： " + Enum.GetName(typeof(T), key) + " がUnitaskStateMachineに追加されていません");
            return;
        }
        if (!m_enableTransiteToSameState && m_currentState == state) { return; }

        if (m_isExitActExec)
        {
            AppDebug.LogError("UniTaskStateMachine：exitActでMoveToNextStateを呼び出さないでください。スキップします。");
            return;
        }

        m_isExitActExec = true;
        m_currentState?.Exit();
        m_isExitActExec = false;

        m_currentState = state;
        m_currentState.Enter();
    }
    /// <summary>
    /// UpdateAct更新
    /// </summary>
    public void Update()
    {
        m_currentState?.Update();
    }
}
