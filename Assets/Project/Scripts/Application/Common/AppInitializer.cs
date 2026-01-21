using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// アプリケーション初期化処理
/// </summary>
public static class AppInitializer
{
    private static bool m_initialized = false;
    private static bool m_initializedAfter = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (!m_initialized)
        {
            m_initialized = true;

            SetupSystem();

            CreateGlobalObjects();
        }
    }


    private static void SetupSystem()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        Addressables.ClearResourceLocators();
        Addressables.InitializeAsync();
    }
    
      
    private static void CreateGlobalObjects()
    {
        CreateObject("AssetManager");
        CreateObject("FadeManager");
        CreateObject("SceneTransitionManager");
        CreateObject("SoundManager");
        CreateObject("GameInput");
#if _DEBUG
        CreateObject("DebugPrint");
        CreateObject("DebugInput");
        CreateObject("DebugSystem");
        CreateObject("DebugDialogLauncher");
        CreateObject("DebugCamera");
#endif
    }

    private static void CreateObject(string prefabName)
    {
        var prefab = Resources.Load<GameObject>(prefabName);
        GameObject.Instantiate(prefab);
    }

    /// <summary>
    /// 初期化・AfterSceneLoad
    /// BeforeSceneLoadではタイミングが早すぎて上手くいかないものがある場合はこちらに。
    /// 後々きちんと作ったらもしかして要らないかも・・？
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeAfter()
    {
        if (!m_initializedAfter)
        {
            m_initializedAfter = true;

            // 言語ロケール設定
            SetUpLocaleAsync().Forget();
        }
    }
    /// <summary>
    ///  言語ロケールセットアップ
    /// </summary>
    /// <returns></returns>
    private static async UniTask SetUpLocaleAsync()
    {
        try
        {
            await LocalizationSettings.InitializationOperation.ToUniTask();

            var sys = Application.systemLanguage;
            var locale = LocalizationSettings.AvailableLocales.GetLocale(sys);

            // ない場合は2文字コードで前方一致
            if (locale == null)
            {
                var two = sys.ToString().Substring(0, 2).ToLowerInvariant();
                locale = LocalizationSettings.AvailableLocales.Locales.Find(l => l.Identifier.Code.StartsWith(two, StringComparison.OrdinalIgnoreCase));
            }

            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
                await UniTask.NextFrame();
            }
        }
        catch (Exception ex)
        {
            AppDebug.LogException(ex);
        }
    }


}
