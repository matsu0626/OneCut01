using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// アプリケーションデバッグ機能.
/// _DEBUG時のみ有効となるログ表示など.
/// </summary>
public static class AppDebug
{
    public static Action<string, string, Action> DispErrorDialogAction { get; set; }
    private static bool m_isCalledErrorDialog = false;

    public static bool IsAssertedOnce;

    public static bool EnableMasterCheck = true;

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("_DEBUG")]
    public static void Log(object message) => UnityEngine.Debug.Log($"[Debug]{message}");

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("_DEBUG")]
    public static void Log(string tag, object message) => UnityEngine.Debug.Log($"[{tag}]{message}");

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("_DEBUG")]
    public static void LogFormat(object message, params object[] args) => UnityEngine.Debug.LogFormat($"[Debug]{message}", args);

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("_DEBUG")]
    public static void LogWarning(object message) => UnityEngine.Debug.LogWarning($"[Warning]{message}");

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("_DEBUG")]
    public static void LogWarning(string tag, object message) => UnityEngine.Debug.LogWarning($"[{tag}]{message}");

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("_DEBUG")]
    public static void LogWarningFormat(object message, params object[] args) => UnityEngine.Debug.LogWarningFormat($"[Warning]{message}", args);

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("_DEBUG")]
    public static void LogError(object message) => UnityEngine.Debug.LogError($"[Error]{message}");

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("_DEBUG")]
    public static void LogError(string tag, object message) => UnityEngine.Debug.LogError($"[{tag}]{message}");

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("_DEBUG")]
    public static void LogErrorFormat(string message, params object[] args) => UnityEngine.Debug.LogErrorFormat($"[Error]{message}", args);

    // NOTE: LogException に限り Conditional 属性を使用しない
    // ReSharper disable Unity.PerformanceAnalysis
    public static void LogException(Exception exception)
    {
        UnityEngine.Debug.LogException(exception);
    }

    // ReSharper disable Unity.PerformanceAnalysis
#if !(_DEBUG && UNITY_ASSERTIONS)
    [Conditional("_DEBUG_AND_UNITY_ASSERTIONS")]
#endif
    public static void LogAssertion(object message) => UnityEngine.Debug.LogAssertion(message);

    // ReSharper disable Unity.PerformanceAnalysis
#if !(_DEBUG && UNITY_ASSERTIONS)
    [Conditional("_DEBUG_AND_UNITY_ASSERTIONS")]
#endif
    public static void LogAssertion(object message, UnityEngine.Object context) => UnityEngine.Debug.LogAssertion(message, context);

    // ReSharper disable Unity.PerformanceAnalysis
#if !(_DEBUG && UNITY_ASSERTIONS)
    [Conditional("_DEBUG_AND_UNITY_ASSERTIONS")]
#endif
    public static void Assert(bool condition, bool dummy = false, [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        UnityEngine.Debug.Assert(condition);
        CheckAndDispErrorDialog(condition, null, sourceFilePath, sourceLineNumber);
    }

    // ReSharper disable Unity.PerformanceAnalysis
#if !(_DEBUG && UNITY_ASSERTIONS)
    [Conditional("_DEBUG_AND_UNITY_ASSERTIONS")]
#endif
    public static void Assert(bool condition, string message, [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        UnityEngine.Debug.Assert(condition, message);
        CheckAndDispErrorDialog(condition, message, sourceFilePath, sourceLineNumber);
    }

    // ReSharper disable Unity.PerformanceAnalysis
#if !(_DEBUG && UNITY_ASSERTIONS)
    [Conditional("_DEBUG_AND_UNITY_ASSERTIONS")]
#endif
    public static void Assert(bool condition, object message, [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        UnityEngine.Debug.Assert(condition, message);
        CheckAndDispErrorDialog(condition, null, sourceFilePath, sourceLineNumber);
    }

#if _DEBUG
    static System.Collections.Generic.List<Tuple<string, int>> s_shownAssertList = new System.Collections.Generic.List<Tuple<string, int>>();
#endif

    private static void CheckAndDispErrorDialog(bool condition, string title, string sourceFilePath, int sourceLineNumber)
    {
#if _DEBUG
        if (IsAssertedOnce)
        {
            foreach (var assertInfo in s_shownAssertList)
            {
                if (assertInfo.Item1 == sourceFilePath && assertInfo.Item2 == sourceLineNumber)
                {
                    return;
                }
            }
            s_shownAssertList.Add(new Tuple<string, int>(sourceFilePath, sourceLineNumber));
        }
#endif
        if (!condition && !m_isCalledErrorDialog)
        {
            m_isCalledErrorDialog = true;
            string callerInfoText = title + Environment.NewLine + sourceFilePath + Environment.NewLine + "Line: " + sourceLineNumber.ToString();
            DispErrorDialogAction?.Invoke(callerInfoText, null, () => m_isCalledErrorDialog = false);
        }
        else
        {
            m_isCalledErrorDialog = false;
        }
    }
}
