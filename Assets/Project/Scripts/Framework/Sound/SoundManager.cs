using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

public class SoundManager : SingletonMonoBehaviour<SoundManager>
{
    private readonly string ASSET_GROUP_LABEL = "CommonSound";

    [Header("AudioMixer Routing")]
    [SerializeField] AudioMixer m_mixer;
    [SerializeField] string m_bgmMixerParam = "BGM_Volume";
    [SerializeField] string m_seMixerParam = "SE_Volume";

    [Header("AudioSources (assign)")]
    [SerializeField] AudioSource m_bgm1;           // loop=ON, playOnAwake=OFF, spatialBlend=0, Output=BGM
    [SerializeField] AudioSource m_bgm2;           // 同上
    [SerializeField] AudioSource m_seTemplate;     // loop=OFF, playOnAwake=OFF, spatialBlend=0, Output=SE

    [Header("Behavior")]
    [SerializeField, Range(0f, 5f)] float m_defaultBgmFade = 0.6f;
    [SerializeField, Range(1, 64)] int m_sePoolSize = 12;

    private AssetGroupLoader m_seLoader = new();
    private CancellationToken m_token;
    private readonly Queue<AudioSource> m_sePool = new();

    private AudioSource m_bgmPlaying; 
    private AudioSource m_bgmFading;

    private AssetLoadHelper.LoadedAsset<AudioClip> m_bgmLoader;


    // Mixer の Exposed Param を 0..1 で制御
    public void SetBGMVolume(float v) => m_mixer?.SetFloat(m_bgmMixerParam, ToDb(v));
    public void SetSEVolume(float v) => m_mixer?.SetFloat(m_seMixerParam, ToDb(v));

    // 0..1 で現在のBGMボリュームを取得
    public float GetBGMVolume()
    {
        if (m_mixer != null && m_mixer.GetFloat(m_bgmMixerParam, out var db))
            return FromDb(db);
        return 1f; // 取得できなければ 1 を既定に
    }

    // 0..1 で現在のSEボリュームを取得
    public float GetSEVolume()
    {
        if (m_mixer != null && m_mixer.GetFloat(m_seMixerParam, out var db))
            return FromDb(db);
        return 1f;
    }


    /// <summary>
    /// SE再生
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="volume"></param>
    public void PlaySE(string fileName, float volume = 1f)
    {
        var clip = GetAssetSE(fileName);
        var a = RentSE();
        a.transform.position = Vector3.zero;
        a.spatialBlend = 0f;
        a.PlayOneShot(clip, Mathf.Clamp01(volume));
        StartCoroutine(ReturnSEWhenDone(a, clip.length));
    }

    /// <summary>
    /// SE 3D 再生 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="pos"></param>
    /// <param name="volume"></param>
    /// <param name="spatialBlend"></param>
    public void PlaySE3D(string fileName, Vector3 pos, float volume = 1f, float spatialBlend = 1f)
    {
        var clip = GetAssetSE(fileName);
        var a = RentSE();
        a.transform.position = pos;
        a.spatialBlend = Mathf.Clamp01(spatialBlend);
        a.PlayOneShot(clip, Mathf.Clamp01(volume));
        StartCoroutine(ReturnSEWhenDone(a, clip.length));
    }

    /// <summary>
    /// BGM読み込み
    /// </summary>
    /// <param name="key">Addresablesキー名</param>
    /// <returns></returns>
    public async UniTask LoadBGM(string key)
    {
        UnloadBGM();
        m_bgmLoader = await AssetLoadHelper.LoadAsync<AudioClip>(key, m_token);
    }

    /// <summary>
    /// BGM解放
    /// </summary>
    public void UnloadBGM()
    {
        m_bgmLoader.Dispose();
    }

    // BGM 再生（クロスフェード）
    public async UniTask PlayBGM(string key, float fade = -1f)
    {
        AudioClip next = null;

        UnloadBGM();

        if (fade < 0f) fade = m_defaultBgmFade;

        if (!string.IsNullOrEmpty(key))
        {
            if (m_bgmLoader.Asset == null)
            {
                await LoadBGM(key);
                next = m_bgmLoader.Asset;
            }
        }

        var from = m_bgmPlaying;
        var to = m_bgmFading;

        to.clip = next; to.volume = 0f;
        if (next) to.Play();

        float t = 0f, v0 = from ? from.volume : 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float p = fade > 0f ? Mathf.Clamp01(t / fade) : 1f;
            if (from) from.volume = Mathf.Lerp(v0, 0f, p);
            if (to && next) to.volume = Mathf.Lerp(0f, 1f, p);
            await UniTask.Yield(PlayerLoopTiming.Update, m_token);
        }
        if (from) { from.Stop(); from.volume = 1f; }
        m_bgmPlaying = to; m_bgmFading = from;
    }

    // BGM 停止（フェードアウト）
    public void StopBGM(float fade = -1f)
    {
        if (!IsBGMPlaying()) return;
        if (fade < 0f) fade = m_defaultBgmFade;
        PlayBGM(null, fade).Forget();
    }

    // 現在BGMが再生中か？（クロスフェード中も true）
    public bool IsBGMPlaying()
    {
        return (m_bgmPlaying && m_bgmPlaying.isPlaying)
            || (m_bgmFading && m_bgmFading.isPlaying);
    }

    /// <summary>
    /// 全アセット解放
    /// </summary>
    public void ReleaseAssetAll()
    {
        m_seLoader.Release();
        m_bgmLoader.Dispose();
    }

    /// <summary>
    /// SEファイル名リスト取得
    /// </summary>
    /// <returns></returns>
    public List<string> GetFileNameListSE() => m_seLoader.GetFileNameList();

    private void Start()
    {
        m_token = this.GetCancellationTokenOnDestroy();
        LoadAssetSE().Forget();

        // SEプール作成
        m_seTemplate.playOnAwake = false; m_seTemplate.loop = false; m_seTemplate.spatialBlend = 0f;
        for (int i = 0; i < m_sePoolSize; i++)
        {
            m_sePool.Enqueue(Instantiate(m_seTemplate, transform));
        }

        // BGM 2枚体制
        foreach (var a in new[] { m_bgm1, m_bgm2 })
        {
            a.playOnAwake = false; a.loop = true; a.spatialBlend = 0f;
        }
        m_bgmPlaying = m_bgm1;
        m_bgmFading = m_bgm2;
    }
    private async UniTask LoadAssetSE()
    {
        await m_seLoader.LoadAssetsAsync(m_token, ASSET_GROUP_LABEL).Wait();
    }
    private AudioClip GetAssetSE(string fileName)
    {
        return m_seLoader.GetAsset<AudioClip>(fileName);
    }

    private AudioSource RentSE() => m_sePool.Count > 0 ? m_sePool.Dequeue() : Instantiate(m_seTemplate, transform);

    private IEnumerator ReturnSEWhenDone(AudioSource a, float length)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, length + 0.02f));
        a.Stop(); a.clip = null; m_sePool.Enqueue(a);
    }
    private static float ToDb(float v) => Mathf.Approximately(v, 0f) ? -80f : Mathf.Log10(Mathf.Clamp01(v)) * 20f;
    private static float FromDb(float db) => db <= -80f ? 0f : Mathf.Pow(10f, db / 20f);

    
    
    

}
