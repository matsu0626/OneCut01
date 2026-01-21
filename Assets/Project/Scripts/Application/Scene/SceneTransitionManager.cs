using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Rendering.STP;
using Color = UnityEngine.Color;


public class SceneTransitionManager : SingletonMonoBehaviour<SceneTransitionManager>
{
    private const string BGM_CONTINUE_TAG = "Continue";

    /// <summary>
    /// シーンID
    ///NOTE: シーン追加手順
    /// 1. Build Profile > Scene Listにシーン追加
    /// 2. enum追加
    /// 3. _sceneItemsテーブルに追加
    /// </summary>
    public enum SceneId
    {
        None,
        OneCutMain,
        // デバッグ用
#if _DEBUG
        DebugMenu,
#endif
        Max,
    }

    readonly struct SceneItem
    {
        public string Name { get; init; }
        public string BGM { get; init; }
        public bool HistoryRecord { get; init; }

    }

    private static readonly SceneItem[] _sceneItems =
    {
        new() { Name = "None",              BGM = "",               HistoryRecord = false },
        new() { Name = "OneCutMainScene",   BGM = "",               HistoryRecord = true  },
#if _DEBUG
        new() { Name = "DebugMenuScene",    BGM = "",               HistoryRecord = false  },
#endif
    };


    public enum FadeType
    {
        None,
        Black,
        White,
        Max,
    }

    [SerializeField] private SceneId m_firstScene = SceneId.None;       // 起動時に最初に遷移するシーン
    [SerializeField] private SceneId m_homeScene = SceneId.None;        // ヒストリーバック時に戻るデフォルトシーン
    public SceneId FirstScene => m_firstScene;
    public SceneId HomeScene => m_homeScene;

    SceneId m_currentSceneId = SceneId.None;
    string m_currentBGM = "";
    private AsyncOperation m_loadeSceneHandle;
    private readonly Stack<SceneId> m_history = new();


    /// <summary>
    /// シーン切り替え
    /// </summary>
    /// <param name="sceneId"></param>
    /// <param name="fadeType"></param>
    /// <param name="progress"></param>
    /*
     NOTE: 

    ・進捗率を取得したい場合のサンプル
     IProgress<float> progress = new Progress<float>(p =>
     {
        AppDebug.Log($"Loading: {(int)(p * 100)}%");
     });
     SceneTransitionManager.Instance.Change(next, FadeType.Black, progress).Forget();
    */
    public async UniTask Change(SceneId sceneId, FadeType fadeType = FadeType.Black, IProgress<float> progress = null, bool clearHistory = false)
    {
        if (clearHistory)
        {
            m_history.Clear();
        }
        if (m_currentSceneId == sceneId)
        {
            AppDebug.LogWarning("現在のシーンと同じシーンに遷移しようとしました");
            return;
        }

        // 現在のシーンがある場合はフェードアウト後にシーン切り替え
        if (m_currentSceneId != SceneId.None)
        {
            switch (fadeType)
            {
                case FadeType.Black:
                    FadeManager.Instance.StartBlackOut();
                    break;
                case FadeType.White:
                    FadeManager.Instance.StartWhiteOut();
                    break;
            }
            await FadeManager.Instance.Wait();
        }
        
        m_currentSceneId = sceneId;
        PushHistory(m_currentSceneId);

        string next = _sceneItems[(int)m_currentSceneId].Name;
        m_loadeSceneHandle = SceneManager.LoadSceneAsync(next, LoadSceneMode.Single);

        string bgm = _sceneItems[(int)m_currentSceneId].BGM;
        if (!string.IsNullOrEmpty(bgm) && bgm != BGM_CONTINUE_TAG && m_currentBGM != bgm)
        {
            m_currentBGM = bgm;
            SoundManager.Instance.PlayBGM(bgm).Forget();
        }


        progress?.Report(0f);
        while (!m_loadeSceneHandle.isDone)
        {
            // Unityのprogressは 0.0〜0.9 までしか上がらないので 0.0〜0.9 に線形マッピング
            float mapped = Mathf.Clamp01(m_loadeSceneHandle.progress / 0.9f) * 0.9f;
            progress?.Report(mapped);

            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        
        progress?.Report(1f);

        switch (fadeType)
        {
            case FadeType.Black:
                FadeManager.Instance.StartBlackIn();
                break;
            case FadeType.White:
                FadeManager.Instance.StartWhiteIn();
                break;
        }
    }

    /// <summary>
    /// シーン戻る
    /// </summary>
    /// <param name="fadeType"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public async UniTask ChangeBack(FadeType fadeType = FadeType.Black, IProgress<float> progress = null)
    {
        SceneId id = PopHistory();
        await Change(id, fadeType, progress);
    }

    private void PushHistory(SceneId id)
    {
        var param = _sceneItems[(int)id];

        if (param.HistoryRecord == false) return;
        if (m_history.Count > 0 && m_history.Peek().Equals(id)) return;   // 直前と同じなら二重化を避ける

        m_history.Push(id);
    }
    private SceneId PopHistory()
    {
        // 現在と同じシーンなら更に前のヒストリーから取り出し
        while (m_history.Count != 0)
        {
            SceneId id = m_history.Pop();
            if (id != m_currentSceneId)
            {
                return id;
            }
        }

        return HomeScene;    // デフォルトはホーム（起点）となるシーンに戻るようにしておく
    }



    private void Start()
    {
    }

#if _DEBUG
    public string GetDebugSceneInfo()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("[Scene]\n");
        sb.Append(" Current : ").Append(m_currentSceneId.ToString()).Append('\n');
        sb.Append(" History :");

        if (m_history != null && m_history.Count > 0)
        {
            int cnt = 0;
            foreach (SceneId id in m_history)
            {
                sb.Append('\n')
                  .Append("  ")
                  .Append(cnt)
                  .Append(": ")
                  .Append(id.ToString());
                cnt++;
            }
        }
        else
        {
            sb.Append(" (none)");
        }

        return sb.ToString();
    }
#endif



}
