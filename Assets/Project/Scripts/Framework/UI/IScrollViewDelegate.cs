public interface IScrollViewDelegate
{
    /// <summary>セル生成/再利用時の初期化（見た目更新＆クリック登録はここで）。</summary>
    void SetupCell(UIScrollCell cell, int index);

    /// <summary>
    /// セル内クリック。複数ボタン対応：controlId で識別。
    /// UIScrollCell 自体も渡すので、呼び出し側でセル内オブジェクトの表示 ON/OFF などができる。
    /// </summary>
    void OnCellClicked(UIScrollCell cell, int index, string controlId);
}
