using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

public class FadeManager : SingletonMonoBehaviour<FadeManager>
{
    public readonly float DEFAULT_FADE_DURATION = 0.5f;

    enum State
    {
        FADE_IN,
        FADE_OUT,
        NONE,
    }

    [SerializeField] CanvasGroup m_canvasGroup;
    [SerializeField] Image m_image;

    UniTaskStateMachine<State> m_state;
    CancellationToken m_token;
    private float m_duration;
    private float m_progress;


    /// <summary>
    /// フェードIN開始
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="color"></param>
    public void StartFadeIn(float duration, Color color)
    {
        m_duration = duration;
        m_image.color = color;
        // 入力を遮断
        m_canvasGroup.blocksRaycasts = true;
        m_canvasGroup.interactable = true;
        m_state.MoveToNextState(State.FADE_IN);
    }
    /// <summary>
    /// ブラックイン開始
    /// </summary>
    /// <param name="duration"></param>
    public void StartBlackIn(float duration)
    {
        StartFadeIn(duration, Color.black);
    }
    /// <summary>
    /// ブラックイン開始
    /// </summary>
    public void StartBlackIn()
    {
        StartFadeIn(DEFAULT_FADE_DURATION, Color.black);
    }
    /// <summary>
    /// ホワイトイン開始
    /// </summary>
    /// <param name="duration"></param>
    public void StartWhiteIn(float duration)
    {
        StartFadeIn(duration, Color.white);
    }
    /// <summary>
    /// ホワイトイン開始
    /// </summary>
    public void StartWhiteIn()
    {
        StartFadeIn(DEFAULT_FADE_DURATION, Color.white);
    }

    /// <summary>
    /// フェードアウト開始
    /// </summary>
    /// <param name="duration"></param>
    public void StartFadeOut(float duration, Color color)
    {
        m_duration = duration;
        m_image.color = color;
        m_canvasGroup.blocksRaycasts = true;
        m_canvasGroup.interactable = true;
        m_state.MoveToNextState(State.FADE_OUT);
    }
    /// <summary>
    /// ブラックアウト開始
    /// </summary>
    /// <param name="duration"></param>
    public void StartBlackOut(float duration)
    {
        StartFadeOut(duration, Color.black);
    }
    /// <summary>
    /// ブラックアウト開始
    /// </summary>
    public void StartBlackOut()
    {
        StartFadeOut(DEFAULT_FADE_DURATION, Color.black);
    }
    /// <summary>
    /// ホワイトアウト開始
    /// </summary>
    /// <param name="duration"></param>
    public void StartWhiteOut(float duration)
    {
        StartFadeOut(duration, Color.white);
    }
    /// <summary>
    /// ホワイトアウト開始
    /// </summary>
    public void StartWhiteOut()
    {
        StartFadeOut(DEFAULT_FADE_DURATION, Color.white);
    }

    /// <summary>
    /// クリア
    /// </summary>
    public void Clear()
    {
        m_canvasGroup.alpha = 0f;
        m_canvasGroup.blocksRaycasts = false;
        m_canvasGroup.interactable = false;
        m_state.MoveToNextState(State.NONE);
        m_progress = 0f;
    }

    /// <summary>
    /// フェード待ち
    /// </summary>
    public async UniTask Wait()
    {
        if (m_state.CurrentState == State.NONE) return;
        await UniTask.WaitUntil(() => m_state.CurrentState == State.NONE, PlayerLoopTiming.Update, m_token);
    }



    private void Start()
    {
        m_canvasGroup.alpha = 0f;
        m_canvasGroup.blocksRaycasts = false;
        m_canvasGroup.interactable = false;
        m_token = this.GetCancellationTokenOnDestroy();
        m_state = new(m_token);
        m_state.Add(State.FADE_IN, OnEnter_FadeIn);
        m_state.Add(State.FADE_OUT, OnEnter_FadeOut);
        m_state.Add(State.NONE);
        m_state.MoveToNextState(State.NONE);
        m_duration = DEFAULT_FADE_DURATION;
        m_progress = 0f;
    }

    private void Update()
    {
        m_state.Update();
    }

    private async UniTask OnEnter_FadeIn(CancellationToken token)
    {
        m_canvasGroup.alpha = 1f;
        m_progress = 0f;
        while (m_canvasGroup.alpha > 0f)
        {
            m_progress += Time.deltaTime;

            m_canvasGroup.alpha = 1f - (m_progress / m_duration);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
        m_canvasGroup.alpha = 0f;
        // 入力遮断終了
        m_canvasGroup.blocksRaycasts = false;
        m_canvasGroup.interactable = false;
        m_state.MoveToNextState(State.NONE);
    }

    private async UniTask OnEnter_FadeOut(CancellationToken token)
    {
        m_canvasGroup.alpha = 0f;
        m_progress = 0f;
        while (m_canvasGroup.alpha < 1f)
        {
            m_progress += Time.deltaTime;
            m_canvasGroup.alpha = m_progress / m_duration;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
        m_canvasGroup.alpha = 1f;
        m_state.MoveToNextState(State.NONE);

    }

    
    //デバッグ表示
#if _DEBUG
    [Conditional("_DEBUG")]
    public void _DebugDisp()
    {
        int y = 1;
        DebugPrint.Instance.PrintLine(20, ++y, $"state: {m_state.CurrentState}");
        DebugPrint.Instance.PrintLine(20, ++y, $"duration: {m_duration}");
        DebugPrint.Instance.PrintLine(20, ++y, $"alpha: {m_canvasGroup.alpha}");
        DebugPrint.Instance.PrintLine(20, ++y, $"color: {m_image.color}");
        DebugPrint.Instance.PrintLine(20, ++y, $"blocksRaycasts: {m_canvasGroup.blocksRaycasts}");
        DebugPrint.Instance.PrintLine(20, ++y, $"interactable: {m_canvasGroup.interactable}");
    }

#endif
}
