#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// LocalizedStringの新規エントリに自動で Smart を付ける“監視&強制”エディタ拡張
/// ・機能有効/無効切り替え
/// ・手動で全コレクションSmart有効化
/// </summary>
public sealed class StringSmartEnforcer : AssetModificationProcessor
{
    private const string PrefKeyEnabled = "StringSmartEnforcer.Enabled";

    // ====== メニュー：有効/無効トグル ======
    [MenuItem("Tools/String/Smart Enforcer/Enabled", priority = 10)]
    private static void ToggleEnabled()
    {
        var enabled = !EditorPrefs.GetBool(PrefKeyEnabled, true);
        EditorPrefs.SetBool(PrefKeyEnabled, enabled);
        AppDebug.Log($"[StringSmartEnforcer] Enabled = {enabled}");
    }

    // チェックマーク表示
    [MenuItem("Tools/String/Smart Enforcer/Enabled", validate = true)]
    private static bool ToggleEnabledValidate()
    {
        Menu.SetChecked("Tools/String/Smart Enforcer/Enabled", EditorPrefs.GetBool(PrefKeyEnabled, true));
        return true;
    }

    // ====== 手動一括実行（全コレクション） ======
    [MenuItem("Tools/String/Smart Enforcer/Apply to All Tables", priority = 11)]
    private static void ApplyAll()
    {
        var guids = AssetDatabase.FindAssets("t:StringTable");
        var totalEdited = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(path);
            if (table == null) continue;
            totalEdited += EnforceSmart(table);
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Smart Enforcer", $"全StringTableに適用しました。\nSmartを付与：{totalEdited} entries", "OK");
    }

    // ====== セーブフック：保存対象の StringTable に自動適用 ======
    private static string[] OnWillSaveAssets(string[] paths)
    {
        if (!EditorPrefs.GetBool(PrefKeyEnabled, true))
            return paths;

        var anyEdited = 0;
        foreach (var path in paths)
        {
            // 変更されたアセットのうち、StringTableだけ処理
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(path);
            if (table == null) continue;

            anyEdited += EnforceSmart(table);
        }

        if (anyEdited > 0)
        {
            // ここでは SaveAssets しない（Unity の保存フローに任せる）
            // ログ控えめ
            // Debug.Log($"[SmartEnforcer] Smart付与: {anyEdited} entries");
        }

        return paths;
    }

    /// <summary>
    /// 指定 StringTable の全エントリに対して、IsSmart=false を true に補正。
    /// 変更数を返す。
    /// </summary>
    private static int EnforceSmart(StringTable table)
    {
        if (table == null) return 0;

        // Undoをテーブル単位で記録
        Undo.RecordObject(table, "Smart Enforce");

        var edited = 0;
        foreach (var entry in table.Values)
        {
            if (entry == null) continue;
            if (!entry.IsSmart)
            {
                entry.IsSmart = true;
                edited++;
            }
        }

        if (edited > 0)
            EditorUtility.SetDirty(table);

        return edited;
    }
}
#endif
