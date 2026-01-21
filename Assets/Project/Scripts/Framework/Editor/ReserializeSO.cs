#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ReserializeSO
{
    [MenuItem("Tools/Serialization/Force Reserialize Selected ScriptableObjects")]
    private static void ForceReserializeSelected()
    {
        var paths = Selection.objects
            .OfType<ScriptableObject>()
            .Select(AssetDatabase.GetAssetPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToArray();

        if (paths.Length == 0)
        {
            EditorUtility.DisplayDialog("Reserialize", "ScriptableObject を選択してください。", "OK");
            return;
        }

        // テキストシリアライズ推奨（一時的でも可）
        var prevMode = EditorSettings.serializationMode;
        if (prevMode != SerializationMode.ForceText)
            EditorSettings.serializationMode = SerializationMode.ForceText;

        AssetDatabase.ForceReserializeAssets(paths);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 元に戻したい場合はコメント解除
        // EditorSettings.serializationMode = prevMode;

        EditorUtility.DisplayDialog("Reserialize", $"Reserialized: {paths.Length} assets", "OK");
    }
}
#endif
