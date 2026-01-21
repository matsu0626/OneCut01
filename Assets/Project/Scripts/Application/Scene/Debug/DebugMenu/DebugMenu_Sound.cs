#if _DEBUG

using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D;


/// <summary>
/// デバッグメニュー・Sound
/// </summary>
public class DebugMenu_Sound : IDebugMenu
{
    enum Select {
        SE,
        BGM,
        VolumeSE,
        VolumeBGM,
        StopBGM,
        Max
    }

    struct BGMItem
    {
        public string _key;
    }
    private static readonly BGMItem[] _bgmItems = new BGMItem[]
    {
        new BGMItem { _key = "bgm_01" },
        new BGMItem { _key = "bgm_02" },
        new BGMItem { _key = "bgm_03" },

    };

    private List<string> m_seFileNames = new();
    private Select m_selectItem = 0;
    private int m_selectSE = 0;
    private int m_selectBGM = 0;
    private float m_volumeSE = 0f;
    private float m_volumeBGM = 0f;
    private float m_volumeChangeSp = 0f;


    public UniTask OnEnter(Transform parent, CancellationToken token)
    {
        m_seFileNames = SoundManager.Instance.GetFileNameListSE();
        m_selectItem = 0;
        m_selectSE = 0;
        m_selectBGM = 0;
        m_volumeSE = SoundManager.Instance.GetSEVolume();
        m_volumeBGM = SoundManager.Instance.GetBGMVolume();
        m_volumeChangeSp = 1f / 10;
        return UniTask.CompletedTask;
    }


    public async UniTask OnUpdate(CancellationToken token)
    {
        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Left))
        {
            switch (m_selectItem)
            {
                case Select.SE:
                    {
                        m_selectSE--;
                        if (m_selectSE < 0)
                        {
                            m_selectSE = m_seFileNames.Count - 1;
                        }
                    }
                    break;
                case Select.BGM:
                    {
                        m_selectBGM--;
                        if (m_selectBGM < 0)
                        {
                            m_selectBGM = _bgmItems.Length - 1;
                        }
                    }
                    break;
                case Select.VolumeSE:
                    {
                        m_volumeSE -= m_volumeChangeSp;
                        if (m_volumeSE < 0)
                        {
                            m_volumeSE = 0;
                        }
                        SoundManager.Instance.SetSEVolume(m_volumeSE);
                    }
                    break;
                case Select.VolumeBGM:
                    {
                        m_volumeBGM -= m_volumeChangeSp;
                        if (m_volumeBGM < 0)
                        {
                            m_volumeBGM = 0;
                        }
                        SoundManager.Instance.SetBGMVolume(m_volumeBGM);
                    }
                    break;


            }

        }
        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Right))
        {
            switch (m_selectItem)
            {
                case Select.SE:
                    {
                        m_selectSE++;
                        if (m_selectSE >= m_seFileNames.Count)
                        {
                            m_selectSE = 0;
                        }

                    }
                    break;
                case Select.BGM:
                    {
                        m_selectBGM++;
                        if (m_selectBGM >= _bgmItems.Length)
                        {
                            m_selectBGM = 0;
                        }
                    }
                    break;
                case Select.VolumeSE:
                    {
                        m_volumeSE += m_volumeChangeSp;
                        if (m_volumeSE > 1f)
                        {
                            m_volumeSE = 1f;
                        }
                        SoundManager.Instance.SetSEVolume(m_volumeSE);
                    }
                    break;
                case Select.VolumeBGM:
                    {
                        m_volumeBGM += m_volumeChangeSp;
                        if (m_volumeBGM > 1f)
                        {
                            m_volumeBGM = 1f;
                        }
                        SoundManager.Instance.SetBGMVolume(m_volumeBGM);
                    }
                    break;
            }
        }
        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Up))
        {
            m_selectItem--;
            if (m_selectItem < 0)
            {
                m_selectItem = Select.Max-1;
            }
        }
        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Down))
        {
            m_selectItem++;
            if (m_selectItem >= Select.Max)
            {
                m_selectItem = 0;
            }
        }
        if (GameKeyboard.Instance.isRepeat(GameKeyboard.Code.Enter))
        {
            switch (m_selectItem)
            {
                case Select.SE:
                    {
                        var selectSE = m_seFileNames[m_selectSE];
                        SoundManager.Instance.PlaySE(selectSE);
                    }
                    break;
                case Select.BGM:
                    {
                        var selectBGM = _bgmItems[m_selectBGM]._key;
                        await SoundManager.Instance.PlayBGM(selectBGM);
                    }
                    break;
                case Select.StopBGM:
                    SoundManager.Instance.StopBGM();
                    break;
            }
        }

        DebugDisp();
    }

    public void OnExit()
    {
    }

    
    private void DebugDisp()
    {
        float x = 20f;
        float w = 30f;
        int y = 2;
        var selectSE = m_seFileNames[m_selectSE];
        var selectBGM = _bgmItems[m_selectBGM]._key;

        DebugPrint.Instance.PrintLine(x, y+(int)m_selectItem, ">");

        DebugPrint.Instance.PrintLine(x+w, y++, $"SE: {selectSE} [{m_selectSE + 1}/{m_seFileNames.Count}]",
            m_selectItem == Select.SE ? Color.yellow : Color.white);
        DebugPrint.Instance.PrintLine(x+w, y++, $"BGM: {selectBGM} [{m_selectBGM + 1}/{_bgmItems.Length}]",
            m_selectItem == Select.BGM ? Color.yellow : Color.white);
        DebugPrint.Instance.PrintLine(x + w, y++, $"VolumeSE: {m_volumeSE:F1}",
            m_selectItem == Select.VolumeSE ? Color.yellow : Color.white);
        DebugPrint.Instance.PrintLine(x + w, y++, $"VolumeBGM: {m_volumeBGM:F1}",
                    m_selectItem == Select.VolumeBGM ? Color.yellow : Color.white);
        DebugPrint.Instance.PrintLine(x + w, y++, $"StopBGM",
                    m_selectItem == Select.StopBGM ? Color.yellow : Color.white);
    }
}


#endif
