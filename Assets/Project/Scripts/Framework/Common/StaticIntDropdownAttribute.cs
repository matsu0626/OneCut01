using System;
using UnityEngine;

/// <summary>
/// 属性・どの定数クラスを読むか指定
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class StaticIntDropdownAttribute : PropertyAttribute
{
    public Type SourceType { get; }
    public bool IncludeNonPublic { get; }

    public StaticIntDropdownAttribute(Type sourceType, bool includeNonPublic = false)
    {
        SourceType = sourceType;
        IncludeNonPublic = includeNonPublic;
    }
}
