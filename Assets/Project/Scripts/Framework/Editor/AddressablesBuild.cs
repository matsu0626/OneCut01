#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using static AddressablesBuild;

/// <summary>
/// エディタメニュー: Addressables ビルド
/// </summary>
public static class AddressablesBuild
{

    public enum Lang
    {
        JA,
        EN,
        Max,
    }

    private readonly static string DEBUG_ASSET_GROUP_NAME = "Debug";

    private static string GetLangGroupName(Lang lang)
    => lang switch
    {
        Lang.JA => "Localization-String-Tables-Japanese (Japan) (ja-JP)",
        Lang.EN => "Localization-String-Tables-English (en)",
        _ => throw new System.ArgumentOutOfRangeException(nameof(lang), lang, null),
    };

    private static void SetIncludeInBuild(AddressableAssetSettings settings, string groupName, bool include)
    {
        var group = settings.groups.FirstOrDefault(g => g != null && g.Name == groupName);
        if (group == null)
        {
            AppDebug.LogWarning($"[Build] Group not found: {groupName}");
            return;
        }
        var schema = group.GetSchema<BundledAssetGroupSchema>();
        if (schema == null)
        {
            schema = group.AddSchema<BundledAssetGroupSchema>();
        }

        if (schema.IncludeInBuild != include)
        {
            schema.IncludeInBuild = include;
            EditorUtility.SetDirty(schema);
            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssets();
        }
    }

    private static void SetLangIncludeInBuild(AddressableAssetSettings settings, Lang lang, bool include)
    {
        var groupName = GetLangGroupName(lang);
        SetIncludeInBuild(settings, groupName, include);
    }

    public static void ResetIncludeInBuild(AddressableAssetSettings settings)
    {
        SetIncludeInBuild(settings, DEBUG_ASSET_GROUP_NAME, true);

        for (int i = 0; i < (int)Lang.Max; i++)
        {
            bool include = (Lang.JA == (Lang)i);
            SetLangIncludeInBuild(settings, (Lang)i, include);
        }
    }

    /// <summary>
    /// Addressablesビルド
    /// </summary>
    /// /// <param name="debug"></param>
    /// <param name="lang"></param>
    public static void Build(bool debug = true, Lang lang = Lang.JA)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            AppDebug.LogError("AddressableAssetSettings が見つかりません。Project Settings > Addressables の Default Settings を確認してください。");
            return;
        }

        // リリースビルド時はデバッググループのデータは除外
        SetIncludeInBuild(settings, DEBUG_ASSET_GROUP_NAME, debug);

        // 言語に併せて必要なデータだけ含めるように
        for (int i = 0; i < (int)Lang.Max; i++)
        {
            bool include = (lang == (Lang)i);
            SetLangIncludeInBuild(settings, (Lang)i, include);
        }

        AddressableAssetSettings.BuildPlayerContent();
        AppDebug.Log($"[Addressables][lang:{lang}] Build Player Content 完了");

        // インクルード設定を元に戻しておく
        ResetIncludeInBuild(settings);
    }

    /// <summary>
    /// Addressablesクリーン
    /// </summary>
    public static void Clean()
    {
        AddressableAssetSettings.CleanPlayerContent();
    }
}
#endif
