using Unity.VisualScripting;
using UnityEngine.Localization.SmartFormat.Core.Parsing;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

/// <summary>
/// Editor系定数
/// </summary>
public static class EditorConstants
{
    /// <summary>
    /// MenuItemSortOrder
    /// </summary>
    public static class MenuItemSortOrder
    {
        // Canvas
        public const int CanvasWithPreset           = 3000;
        public const int CanvasWithPresetES         = 3001;
        public const int AddSortOrderToCanvas       = 3002;

        // Image
        public const int FullscreenBG               = 3100;

        // Text
        public const int TMPLocalized               = 3200;
        public const int AddLseToTMP                = 3201;

        // Button/CheckBox
        public const int UIButtonTMP                = 3300;
        public const int UIButtonNoLabel            = 3301;
        public const int UICheckBoxTMP              = 3302;
        public const int UICheckBoxNoLabel          = 3303;

        // ScrollView / ScrollTextView 
        public const int ScrollViewVertical             = 3400;
        public const int ScrollViewHorizontal           = 3401;
        public const int ScrollViewVerticalScrollbar    = 3402;
        public const int ScrollViewHorizontalScrollbar  = 3403;
        public const int ScrollTextView                 = 3410;
        


        // その他
        public const int MoveToSafeArea             = 3900;

    }

}
