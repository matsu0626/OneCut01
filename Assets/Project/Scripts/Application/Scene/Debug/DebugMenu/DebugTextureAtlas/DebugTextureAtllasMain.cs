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

public class DebugTextureAtllasMain : MonoBehaviour
{
    private enum State
    {
        Select,
    }

    [SerializeField] private Image m_image;
    private AssetGroupLoader m_groupLoader = new();
    private UniTaskStateMachine<State> m_state;
    private CancellationToken m_token;
    private SpriteAtlas m_spriteAtlas;
    private List<string> m_spriteNames = new();
    private int m_select;
    private string m_spriteAtllasFileName = "common_01";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_token = this.GetCancellationTokenOnDestroy();
        m_state = new(m_token);
        m_state.Add(State.Select, OnStateEnter, OnStateUpdate);
        m_state.MoveToNextState(State.Select);
    }

    private async UniTask OnStateEnter(CancellationToken token)
    {
        // アトラスファイル読み込み/取得
        await m_groupLoader.LoadAssetsAsync(token, "CommonTexture").Wait();
        m_spriteAtlas = m_groupLoader.GetAsset<SpriteAtlas>(m_spriteAtllasFileName);
        m_image.gameObject.SetActive(true);
        
        var sprites = new Sprite[m_spriteAtlas.spriteCount];
        m_spriteAtlas.GetSprites(sprites);

        // (Clone)をぬいた名前にする
        for (int i = 0; i < sprites.Length; ++i)
        {
            var name = CommonUtil.WithoutCloneSuffix(sprites[i].name);

            m_spriteNames.Add(name);
        }


        SetupSpriteSelectCur();
    }

    
    private void OnStateUpdate()
    {
        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Right))
        {
            m_select++;
            if (m_select >= m_spriteAtlas.spriteCount)
            {
                m_select = 0;
            }
            SetupSpriteSelectCur();
        }
        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Left))
        {
            m_select--;
            if (m_select < 0)
            {
                m_select = m_spriteAtlas.spriteCount-1;
            }
            SetupSpriteSelectCur();
        }


        DebugDisp();
    }

    private void SetupSpriteSelectCur()
    {
        var name = m_spriteNames[m_select];
        m_image.sprite = m_spriteAtlas.GetSprite(name);
    }

    private void DebugDisp()
    {
        float x = 20f;
        int y = 2;
        DebugPrint.Instance.PrintLine(x, ++y, $"SpriteAtlas: {m_spriteAtllasFileName} [{m_select+1}/{m_spriteAtlas.spriteCount}]");
        DebugPrint.Instance.PrintLine(x, ++y, $"{m_spriteNames[m_select]}");
    }

    private void Update()
    {
        m_state.Update();
    }

    private void OnDestroy()
    {
        m_groupLoader.Release();
    }

}
#endif