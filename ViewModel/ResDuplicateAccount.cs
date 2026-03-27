namespace Tool.ViewModel;

/// <summary>
/// 重複帳號查詢結果
/// </summary>
public class ResDuplicateAccount
{
    /// <summary>
    /// 營運商 ID
    /// </summary>
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>
    /// 帳號
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 重複數量
    /// </summary>
    public int Count { get; set; }
}
