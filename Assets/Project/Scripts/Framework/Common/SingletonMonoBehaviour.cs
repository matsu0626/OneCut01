using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

#if UNITY_EDITOR
    /// <summary>
    /// Warningを出さないヌルチェック
    /// エディタ中以外はWarning出た方が安全そうなのでエディタに限定中
    /// </summary>
    /// <returns></returns>
    public static bool IsNull()
    {
        if (instance != null)
        {
            return false;
        }

        Type t = typeof(T);
        var findObj = (T)FindFirstObjectByType(t);

        return (findObj == null);
    }
#endif

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                Type t = typeof(T);

                instance = (T)FindFirstObjectByType(t);
                if (instance == null)
                {
                    AppDebug.LogWarning(t + " をアタッチしているGameObjectはありません");
                }
            }

            return instance;
        }
    }

    virtual protected void Awake()
    {
        // 他のGameObjectにアタッチされているか調べる.
        // アタッチされている場合は破棄する.
        if (this != Instance)
        {
            Destroy(this);
            AppDebug.LogWarning(
                typeof(T) +
                " は既に他のGameObjectにアタッチされているため、コンポーネントを破棄しました." +
                " アタッチされているGameObjectは " + Instance.gameObject.name + " です.");
            return;
        }
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }
  
}
