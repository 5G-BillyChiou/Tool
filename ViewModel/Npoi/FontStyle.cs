namespace Tool.ViewModel.Npoi;

/// <summary>
/// 字體
/// </summary>
public class FontStyle
{
    /// <summary>
    /// 字體
    /// </summary>
    public string FontName { get; set; }
    /// <summary>
    /// 字體大小
    /// </summary>
    public short? FontHeightInPoints { get; set; }
    /// <summary>
    /// 自己決定文字預設格式
    /// </summary>
    public FontStyle()
    {
        FontName = "Calibri";
        FontHeightInPoints = null;
    }
}