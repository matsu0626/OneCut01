using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using FadeType = SceneTransitionManager.FadeType;
using SceneId = SceneTransitionManager.SceneId;


/// <summary>
/// 起動シーンメイン
/// </summary>
public class BootMain : MonoBehaviour
{
    private void Start()
    {
        SceneId next =
#if _DEBUG
           SceneId.DebugMenu;                               // 開発時はデバッグメニューへ

#else
           SceneTransitionManager.Instance.FirstScene;      // 本番ビルドは最初のシーンへ
#endif
        // ローディングの進捗をこちらからとりたい場合
        SceneTransitionManager.Instance.Change(next).Forget();
        
    }
}
