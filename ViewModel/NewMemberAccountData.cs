namespace Tool.ViewModel;

public class NewMemberAccountData
{
    /// <summary>
    /// 期間
    /// </summary>
    public DateTimeOffset PeriodStartAt { get; set; }

    /// <summary>
    /// 新增人數
    /// </summary>
    public long NewMemberCount { get; set; }
}
