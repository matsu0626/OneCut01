#if _DEBUG

using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DebugMenu : MonoBehaviour
{
    [SerializeField] private GameObject m_bgRoot; // CanvasのBG

    private readonly float BASE_X = 20f;
    private readonly int BASE_LINE = 1;
    private readonly int LINE_MAX = 10;   // 1ページの最大行数
    private readonly string PROC_CLASSNAME_PREFIX = "DebugMenu_";

    struct MenuItem
    {
        public string _itemName;
        public bool _hideBG;   // 3D系テスト時でBGを消す場合true
    }

    // 使いたいキーはここに追加
    private static readonly MenuItem[] _menuItems = new MenuItem[]
    {
        new MenuItem { _itemName = "EXIT",              _hideBG = false }, // 先頭はEXIT＝ゲームへ固定
        new MenuItem { _itemName = "Addressables",      _hideBG = false },
        new MenuItem { _itemName = "Fade",              _hideBG = false },
        new MenuItem { _itemName = "TextureAtlas",      _hideBG = false },
        new MenuItem { _itemName = "Sound",             _hideBG = false },
        new MenuItem { _itemName = "UIButton",          _hideBG = false },
        new MenuItem { _itemName = "UIDialog",          _hideBG = false },
        new MenuItem { _itemName = "UIModalStack",      _hideBG = false },
        new MenuItem { _itemName = "UIScrollView",      _hideBG = false },
        new MenuItem { _itemName = "3DModel",           _hideBG = true  },
        new MenuItem { _itemName = "3DCharacterMove",   _hideBG = true  },
    };

    private enum State
    {
        Select,
        Exec,
    }
    private CancellationToken m_token;
    private int m_baseItemLine;
    private int m_menuItemNum;
    private float m_arrowX;
    private float m_itemX;
    private int m_select;
    private UniTaskStateMachine<State> m_state;
    private IDebugMenu m_debugProc;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_token = this.GetCancellationTokenOnDestroy();
        m_baseItemLine = BASE_LINE + 1;
        m_menuItemNum = _menuItems.Length;
        m_arrowX = BASE_X;
        m_itemX = m_arrowX + DebugPrint.Instance.GetFontSize();
        m_select = 0;
        m_debugProc = null;

        m_state = new(m_token);
        m_state.Add(State.Select, updateAct: OnStateSelect);
        m_state.Add(State.Exec, updateAct: OnStateExec);
        m_state.MoveToNextState(State.Select);
    }

    // Update is called once per frame
    private void Update()
    {
        m_state.Update();
    }

    private void OnStateSelect()
    {
        UpdateSelect();
        UpdateSelectDisp();
    }

    private void OnStateExec()
    {
        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Backspace))
        {
            // 3D系メニューだったらBGを戻す
            if (m_bgRoot && _menuItems[m_select]._hideBG)
            {
                m_bgRoot.SetActive(true);
            }

            m_state.MoveToNextState(State.Select);

            m_debugProc.OnExit();
            return;
        }

        m_debugProc.OnUpdate(m_token);

        if (m_select != 0)
        {
            DebugPrint.Instance.PrintLine(BASE_X, BASE_LINE, _menuItems[m_select]._itemName);
        }
        
    }


    // 選択位置の更新
    private void UpdateSelect()
    {
        int page = m_select / LINE_MAX;
        int smin = LINE_MAX * page;
        int smax = LINE_MAX * (page + 1) - 1;
        if (smax >= m_menuItemNum)
        {
            smax = m_menuItemNum - 1;
        }
        int lmax = LINE_MAX;
        if (lmax > m_menuItemNum)
        {
            lmax = m_menuItemNum;
        }

        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Left))
        {
            if (m_select - lmax < 0)
            {
                m_select = m_menuItemNum - 1;
            }
            else
            {
                m_select = (m_select + (m_menuItemNum - lmax)) % m_menuItemNum;
            }
        }
        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Right))
        {
            if (m_select == m_menuItemNum - 1)
            {
                m_select = 0;
            }
            else if (m_select + lmax >= m_menuItemNum)
            {
                m_select = m_menuItemNum - 1;
                
            }
            else
            {
                m_select = (m_select + lmax) % m_menuItemNum;

            }
        }

        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Up))
        {
            --m_select;
            if (m_select < smin)
            {
                m_select = smax;
            }
        }
        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Down))
        {
            ++m_select;
            if (m_select > smax)
            {
                m_select = smin;
            }
        }

        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Enter))
        {
            m_state.MoveToNextState(State.Exec);

            // 3D系メニューなど、BGを消したい場合
            if (m_bgRoot && _menuItems[m_select]._hideBG)
            {
                m_bgRoot.SetActive(false);
            }

            string className = PROC_CLASSNAME_PREFIX + _menuItems[m_select]._itemName;
            Type t = Type.GetType(className);
            AppDebug.Assert(t != null);
            object obj = Activator.CreateInstance(t);
            m_debugProc = obj as IDebugMenu;
            AppDebug.Assert(m_debugProc != null, className + "がIDebugMenuを継承していません");
            m_debugProc.OnEnter(transform, m_token);
        }

    }

    // 選択表示更新
    private void UpdateSelectDisp()
    {
        // ページ数を求める
        int pageNow = m_select / LINE_MAX;
        int pageMax = m_menuItemNum / LINE_MAX;
        if (pageMax <= 0) 
        { 
            pageMax++; 
        }
        if (m_menuItemNum > LINE_MAX)
        {
            if ((m_menuItemNum % LINE_MAX) != 0)
            { 
                pageMax++; 
            }
        }

        string pageStr = string.Format("Page {0}/{1}", pageNow + 1, pageMax);
        DebugPrint.Instance.PrintLine(BASE_X, BASE_LINE, pageStr);
                     
        
        int selectLine = m_baseItemLine + (m_select % LINE_MAX);
        DebugPrint.Instance.PrintLine(m_arrowX, selectLine, ">");

        int line = 0;
        for (int i = LINE_MAX * pageNow; (i < m_menuItemNum) && (line < LINE_MAX); ++i)
        {
            var item = _menuItems[i];
            Color color = (m_select == i) ? Color.yellow : Color.white;
            DebugPrint.Instance.PrintLine(m_itemX, m_baseItemLine + line, item._itemName, color);
            ++line;
        }

    }

}
#endif
