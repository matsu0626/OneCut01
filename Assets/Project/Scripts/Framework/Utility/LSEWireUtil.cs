using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Components;
using System.Linq;

public static class LSEWireUtil
{
    /// <summary>
    /// LocalizationStringEventの設定
    /// </summary>
    /// <param name="lse"></param>
    /// <param name="tableCollectionName"></param>
    /// <param name="entryKey"></param>
    /// <returns></returns>
    public static bool SetLocalizeString(LocalizeStringEvent lse, string tableCollectionName, string entryKey)
    {
        var col = LocalizationEditorSettings.GetStringTableCollection(tableCollectionName);
        if (col == null)
        {
            AppDebug.LogWarning($"テーブルコレクションが見つかりません. {tableCollectionName}");
            return false;
        }

        var shared = col.SharedData;
        var entry = shared.GetEntry(entryKey);
        if (entry == null)
        {
            AppDebug.LogWarning($"entryKeyが見つかりません. key={entryKey}\n" +
                                $" [Shared] keys = {string.Join(", ", shared.Entries.Select(e => e.Key))}");
            return false;
        }

        var ls = lse.StringReference;
        ls.TableReference = shared.TableCollectionNameGuid;
        ls.TableEntryReference = entry.Id;
        lse.StringReference = ls;

        EditorUtility.SetDirty(lse);
        return true;
    }
}
