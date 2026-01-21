#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    // C# 9 'init' 用の互換ダミー
    /* 
    NOTE:
    ・↓こんな感じのテーブルを作りたい時に、初期化だけ許可するinitを使用
    readonly struct Item
    {
        public string Name { get; init; }

    }
    private static readonly Item[] _items =
    {
        new() { Name = "Name01" },
    };
   →これを入れないと何故かエラーが起きるみたい…
    */
    internal static class IsExternalInit { }
}
#endif
