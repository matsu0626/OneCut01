#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

/// <summary>
/// Localization の StringTableCollection を Addressables グループに
/// プロファイル切り替え（All Local / FIX Local & Others Remote）で振り分けるツール。
/// </summary>
public static class StringMoveProfiles
{
    private const string LocalGroupName = "StringLocal";
    private const string RemoteGroupName = "StringRemote";

    // 「FIX」コレクション名判定
    private static bool IsFixCollection(string collectionName)
    {
        // 完全一致
        if (collectionName == "Fix") return true;


        return false;
    }

    // リモート側に付けるラベル
    private const string RemoteLabel = ""; // 使わないなら null/空でOK
    // ===============================

    [MenuItem("Tools/String/Addressables/All Local")]
    public static void Move_AllLocal()
    {
        var (local, remote) = EnsureGroups();
        var collections = FindAllStringTableCollections();

        var moved = 0;
        foreach (var col in collections)
            moved += MoveCollection(col, local, applyRemoteLabel: false);

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Localization Move Profiles", $"All Local 完了：{moved} entries moved.", "OK");
    }

    [MenuItem("Tools/String/Addressables/FIX Local && Others Remote")]
    public static void Move_FixLocal_OthersRemote()
    {
        var (local, remote) = EnsureGroups();
        var collections = FindAllStringTableCollections();

        var moved = 0;
        foreach (var col in collections)
        {
            var isFix = IsFixCollection(col.TableCollectionName);
            var target = isFix ? local : remote;
            var addRemoteLabel = !isFix && !string.IsNullOrEmpty(RemoteLabel);
            moved += MoveCollection(col, target, addRemoteLabel);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Localization Move Profiles", $"FIX Local / Others Remote 完了：{moved} entries moved.", "OK");
    }

    // ========== 中身（共通ユーティリティ） ==========

    private static (AddressableAssetGroup local, AddressableAssetGroup remote) EnsureGroups()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorUtility.DisplayDialog("String Move Profiles", "AddressableAssetSettings が見つかりません。Window > Asset Management > Addressables で初期化してください。", "OK");
            throw new System.InvalidOperationException("AddressableAssetSettings not found.");
        }

        var local = settings.FindGroup(LocalGroupName);
        if (local == null)
            local = settings.CreateGroup(LocalGroupName, false, false, true, null, typeof(BundledAssetGroupSchema));

        var remote = settings.FindGroup(RemoteGroupName);
        if (remote == null)
            remote = settings.CreateGroup(RemoteGroupName, false, false, true, null, typeof(BundledAssetGroupSchema));

        // スキーマの最低限設定（必要ならプロジェクト方針に合わせて）
        EnsureBundledSchema(local, isRemote: false);
        EnsureBundledSchema(remote, isRemote: true);

        return (local, remote);
    }

    private static void EnsureBundledSchema(AddressableAssetGroup group, bool isRemote)
    {
        var schema = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
        // Build/Load Path は Addressables の Profile で管理するのが本筋。
        // ここではリモート側の "Include In Build" をONにしておく程度（必要なら調整）。
        schema.IncludeInBuild = true;
        // Custom Build/Load Path を使う場合は Profile 変数に合わせて設定してください。
        // 例:
        // schema.BuildPath.SetVariableByName(AddressableAssetSettingsDefaultObject.Settings, isRemote ? "RemoteBuildPath" : "LocalBuildPath");
        // schema.LoadPath .SetVariableByName(AddressableAssetSettingsDefaultObject.Settings, isRemote ? "RemoteLoadPath"  : "LocalLoadPath" );
    }

    private static StringTableCollection[] FindAllStringTableCollections()
    {
        var guids = AssetDatabase.FindAssets("t:StringTableCollection");
        var list = new System.Collections.Generic.List<StringTableCollection>(guids.Length);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var col = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);
            if (col != null) list.Add(col);
        }
        return list.ToArray();
    }

    /// <summary>
    /// 指定コレクションの「全言語の StringTable」と「SharedData」を targetGroup に移動。
    /// 戻り値は移動した Addressables エントリ数。
    /// </summary>
    private static int MoveCollection(StringTableCollection collection, AddressableAssetGroup targetGroup, bool applyRemoteLabel)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var moved = 0;

        // 各言語の StringTable
        foreach (var table in collection.StringTables)
        {
            if (table == null) continue;
            var path = AssetDatabase.GetAssetPath(table);
            if (string.IsNullOrEmpty(path)) continue;

            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, targetGroup);
            if (entry.parentGroup != targetGroup)
            {
                settings.MoveEntry(entry, targetGroup);
                moved++;
            }
            if (applyRemoteLabel && !string.IsNullOrEmpty(RemoteLabel) && !entry.labels.Contains(RemoteLabel))
            {
                entry.SetLabel(RemoteLabel, true, true);
            }
        }

        // SharedData（ID解決に必要）
        var shared = collection.SharedData;
        if (shared != null)
        {
            var path = AssetDatabase.GetAssetPath(shared);
            if (!string.IsNullOrEmpty(path))
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, targetGroup);
                if (entry.parentGroup != targetGroup)
                {
                    settings.MoveEntry(entry, targetGroup);
                    moved++;
                }
            }
        }

        return moved;
    }
}
#endif
