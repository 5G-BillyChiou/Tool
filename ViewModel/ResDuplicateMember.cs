namespace Tool.ViewModel;

public class ResDuplicateMember
{
    /// <summary>
    /// 會員 ID
    /// </summary>
    public string MemberId { get; set; } = string.Empty;

    /// <summary>
    /// 營運商 ID
    /// </summary>
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>
    /// 會員帳號
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 重複數量
    /// </summary>
    public int Count { get; set; }
}
