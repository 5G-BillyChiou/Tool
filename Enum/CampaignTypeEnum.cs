using System.ComponentModel;

namespace Tool.Enum;

/// <summary>
/// 活動類型
/// </summary>
public enum CampaignTypeEnum
{
    /// <summary>
    /// 預設
    /// </summary>
    CampaignType_Default = 0,

    /// <summary>
    /// 贈送免費旋轉 (FreeSpinBonus)
    /// </summary>
    [Description("FSB")]
    CampaignTypeEnum_FreeSpinBonus = 1,

    /// <summary>
    /// 紅包雨
    /// </summary>
    [Description("CashDrop")]
    CampaignTypeEnum_CashDrop = 2,

    /// <summary>
    /// 錦標賽
    /// </summary>
    [Description("Tournament")]
    CampaignTypeEnum_Tournament = 3,
}
