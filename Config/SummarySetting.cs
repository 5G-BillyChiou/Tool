namespace Tool.Config;

/// <summary>
/// 排程匯總相關設定
/// </summary>
public class SummarySetting
{
    /// <summary>
    /// 測試的營運商ID
    /// </summary>
    public string[] TestOperatorIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 比對開始時間
    /// </summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>
    /// 比對結束時間
    /// </summary>
    public DateTimeOffset EndAt { get; set; }
}