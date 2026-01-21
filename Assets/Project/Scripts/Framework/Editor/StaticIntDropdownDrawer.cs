#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(StaticIntDropdownAttribute))]
public sealed class StaticIntDropdownDrawer : PropertyDrawer
{
    // 型ごとに反射結果をキャッシュ
    private static readonly Dictionary<(Type, bool), (GUIContent[] display, int[] values)> s_cache = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Integer)
        {
            EditorGUI.LabelField(position, label.text, "Use on int fields");
            return;
        }

        var attr = (StaticIntDropdownAttribute)attribute;
        var key = (attr.SourceType, attr.IncludeNonPublic);

        if (!s_cache.TryGetValue(key, out var cached))
        {
            cached = BuildItems(attr);
            s_cache[key] = cached;
        }

        var display = cached.display;  
        var values = cached.values;    
        var current = property.intValue;

        // 現在値が定義にあるか
        var idx = Array.IndexOf(values, current);

        EditorGUI.BeginProperty(position, label, property);
        if (idx >= 0)
        {
            // ① 定義に一致 → そのままリストのみ表示（Customは出さない）
            var newIdx = EditorGUI.Popup(position, label, idx, display);
            if (newIdx != idx) property.intValue = values[newIdx];
        }
        else
        {
            // ② 未定義 → 先頭に Custom を一時的に挿入して表示
            var shown = new GUIContent[display.Length + 1];
            shown[0] = new GUIContent($"Custom ({current})");
            Array.Copy(display, 0, shown, 1, display.Length);

            // 選択0は未定義値を維持、>0を選べば定義値に置換
            var newIdx = EditorGUI.Popup(position, label, 0, shown);
            if (newIdx > 0) property.intValue = values[newIdx - 1];
        }
        EditorGUI.EndProperty();
    }

    // 定数だけを配列化（Customは入れない）
    private static (GUIContent[] display, int[] values) BuildItems(StaticIntDropdownAttribute attr)
    {
        var items = new List<(string name, int value)>();
        var flags = BindingFlags.Static | BindingFlags.FlattenHierarchy |
                    (attr.IncludeNonPublic ? BindingFlags.Public | BindingFlags.NonPublic
                                           : BindingFlags.Public);

        foreach (var f in attr.SourceType.GetFields(flags))
        {
            if (!f.IsLiteral || f.IsInitOnly) continue; // const
            if (f.FieldType != typeof(int)) continue;   // int
            items.Add((f.Name, (int)f.GetRawConstantValue()));
        }

        // 値の若い順で出す（同値は名前順）
        items.Sort((a, b) =>
        {
            var cmp = a.value.CompareTo(b.value);
            return cmp != 0 ? cmp : string.Compare(a.name, b.name, StringComparison.Ordinal);
        });

        var display = new GUIContent[items.Count];
        var values = new int[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            display[i] = new GUIContent($"{it.name}  ({it.value})");
            values[i] = it.value;
        }
        return (display, values);
    }
}
#endif
