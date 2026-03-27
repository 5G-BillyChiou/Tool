using System.ComponentModel;

namespace Tool.Enum;

/// <summary>
/// 錢包類型
/// <para>這Description有用在Excel匯入時比對取得enum</para>
/// </summary>
[Description("錢包類型")]
public enum WalletTypeEnum
{
    /// <summary>
    /// 共用錢包
    /// </summary>
    [Description("共用")]
    WalletType_Shared = 0,

    /// <summary>
    /// 獨立錢包
    /// </summary>
    [Description("獨立")]
    WalletType_Single = 1,
}
