using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// デバッグ用キーボード
/// </summary>
public class GameKeyboard : SingletonMonoBehaviour<GameKeyboard>
{
    [SerializeField] private InputSetting setting;

    public enum Code
    {
        // 汎用キー
        Up,
        Down,
        Left,
        Right,
        Enter,
        Backspace,

        // デバッグ機能用キー
        DebugCameraToggle,   // デバッグカメラ用トグルキー

        // -------------
        Max,
    }
    
    struct KeyItem
    {
        public Key _id;
    }

    // 使いたいキーはここに追加
    private static readonly KeyItem[] _checkKeyItems = new KeyItem[(int)Code.Max]
    {
        // 汎用キー
        new KeyItem { _id = Key.UpArrow },
        new KeyItem { _id = Key.DownArrow },
        new KeyItem { _id = Key.LeftArrow },
        new KeyItem { _id = Key.RightArrow },
        new KeyItem { _id = Key.Enter },
        new KeyItem { _id = Key.Backspace },

        // デバッグ機能用キー
        new KeyItem { _id = Key.Digit1 },       // デバッグカメラ用トグルキー: 1キー
    };


    private bool[] m_press = new bool[(int)Code.Max];
    private bool[] m_trg = new bool[(int)Code.Max];
    private bool[] m_rls = new bool[(int)Code.Max];
    private bool[] m_repeat = new bool[(int)Code.Max];
    private float[] m_cntRepeat = new float[(int)Code.Max];

    private void Start()
    {
    }

    private void Update()
    {
        if (Keyboard.current == null) return;   // キーボードが接続されていない   

        for (int key = 0; key < (int)Code.Max; ++key) 
        {
            var obj = _checkKeyItems[key];
                        
            m_press[key] = Keyboard.current[obj._id].isPressed;
            m_trg[key] = Keyboard.current[obj._id].wasPressedThisFrame;
            m_rls[key] = Keyboard.current[obj._id].wasReleasedThisFrame;

            m_repeat[key] = m_trg[key];
            if (m_repeat[key])
            {
                m_cntRepeat[key] = setting.firstDelay;
            }
            if (m_press[key])
            {
                if (m_cntRepeat[key] > 0f)
                {
                    m_cntRepeat[key] -= Time.deltaTime;
                    if (m_cntRepeat[key] <= 0f)
                    {
                        m_cntRepeat[key] = 0f;
                    }
                }else
                {
                    m_repeat[key] = true;
                    m_cntRepeat[key] = setting.repeatInterval;
                }
            }
        }
    }

    public bool isPress(Code code)
    {
        return m_press[(int)code];
    }
    public bool isTrg(Code code) 
    {
	    return m_trg[(int)code];
    }
    public bool isRls(Code code)
    {
        return m_rls[(int)code];
    }
    public bool isRepeat(Code code)
    {
        return m_repeat[(int)code];
    }

}



