namespace Tool;

/// <summary>
/// 開發相關設定
/// </summary>
public class DevelopmentSetting
{
    /// <summary>
    /// SSH Tunnel Setting
    /// </summary>
    public TunnelSetting TunnelSetting { get; set; }

    /// <summary>
    /// 是否開啟IP白名單檢查
    /// </summary>
    public bool EnableIpWhileListCheck { get; set; } = true;
}
