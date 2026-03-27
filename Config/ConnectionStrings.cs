namespace Tool;

/// <summary>
/// 連線設定
/// </summary>
public class ConnectionStrings
{
    /// <summary>
    /// 5G Main MySQL資料庫連線資訊
    /// </summary>
    public string FiveGameConnection { get; set; }

    /// <summary>
    /// Main Mongo資料庫連線資訊
    /// </summary>
    public string AdminMongoConnection { get; set; }

    /// <summary>
    /// 各地區Mongo資料庫連線資訊
    /// </summary>
    public string AgentMongoConnection { get; set; }

    /// <summary>
    /// 代理地區Warm資料庫連線資訊
    /// </summary>
    public string AgentWarmMongoConnection { get; set; }
}
