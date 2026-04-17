namespace Tool;

/// <summary>
/// 連線設定
/// </summary>
public class ConnectionStrings
{
    /// <summary>
    /// 5G Main MySQL資料庫連線資訊
    /// </summary>
    public string FiveGameConnection { get; set; } = string.Empty;

    /// <summary>
    /// 遊戲 HotDB 資料庫連線資訊
    /// </summary>
    public string FiveGameTransConnection { get; set; } = string.Empty;

    /// <summary>
    /// Main Mongo 資料庫連線資訊
    /// </summary>
    public string AdminMongoConnection { get; set; } = string.Empty;

    /// <summary>
    /// 代理地區Warm資料庫連線資訊
    /// </summary>
    public string AgentWarmMongoConnection { get; set; } = string.Empty;
}
