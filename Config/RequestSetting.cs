namespace Tool.Config;

public class RequestSetting
{
    /// <summary>
    /// 要處理的錢包類型
    /// </summary>
    public string WalletType { get; set; } = string.Empty;

    /// <summary>
    /// 要處理的營運商ID
    /// </summary>
    public string OperatorId { get; set; } = string.Empty;
}
