using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 共通ユーティリティ
/// </summary>
public static class CommonUtil
{
    /// <summary>
    /// RectTransform を持つ空の GameObject を生成して parent の子にする（ローカル基準）
    /// </summary>
    public static GameObject CreateGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>子孫から名前一致の Transform を検索（非アクティブも含む）。</summary>
    public static Transform FindDescendantByName(Transform root, string name)
    {
        if (!root || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c.name == name) return c;
            var hit = FindDescendantByName(c, name);
            if (hit) return hit;
        }
        return null;
    }

    /// <summary>子孫から名前一致でコンポーネント取得（無ければ null）。</summary>
    public static T FindByName<T>(Transform root, string name) where T : Component
    {
        var t = FindDescendantByName(root, name);
        return t ? t.GetComponent<T>() : null;
    }

    static readonly Regex s_regexClone = new(@"\s*\(Clone\)$", RegexOptions.Compiled);

    /// <summary>
    /// 接尾辞(Clone)を除外した文字列を取得
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string WithoutCloneSuffix(string str) => s_regexClone.Replace(str, string.Empty);
}

