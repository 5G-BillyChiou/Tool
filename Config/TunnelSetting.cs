namespace Tool;

/// <summary>
/// SSH Tunnel Setting
/// </summary>
public class TunnelSetting
{
    /// <summary>
    /// 是否開啟MongoDB SSH Tunnel
    /// </summary>
    public bool EnableMongoForwarding { get; set; }

    /// <summary>
    /// 進行SSH Tunnel 的 Host
    /// </summary>
    public string ProxyHost { get; set; }

    /// <summary>
    /// 進行SSH Tunnel 的 登入帳號
    /// </summary>
    public string ProxyUserName { get; set; }

    /// <summary>
    /// 用以驗證 SSH Tunnel 的 Private Key
    /// </summary>
    public string PemKeyPath { get; set; }
}
