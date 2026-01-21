using UnityEngine;


/// <summary>
/// UI系定数
/// </summary>
public static class UIConstants
{
    // 画面サイズ
    public const float ScreenWidth = 1920f;
    public const float ScreenHeight = 1080f;
    public const float MatchWidthOrHeight = 0.5f;

    // セーフエリア
    public const string SafeAreaName = "SafeArea";
    public static readonly Rect EditorSafeAreaPortrait = new Rect(0f, 0.03f, 1f, 0.94f);
    public static readonly Rect EditorSafeAreaLandscape = new Rect(0.05f, 0f, 0.90f, 1f);

    // 常駐アセットラベル
    public const string CommonAssetLabel = "Common";

    // テキスト
    public const string StringDefaultGroupName = "Fix";
    public const string StringDefaultGroupKey = "FIX_OK";

    /// <summary>
    /// キャンバスプリセットSortOrder
    /// </summary>
    public static class CanvasSortOrder
    {
        public const int UI              = 1000;
        public const int Fade            = 2000;
        public const int DebugPrint      = 9000;
        public const int DebugMenu       = 10000;
    }

    public const string UIExtensionsMenuPath = "GameObject/UI/Extensions/";
    
}
