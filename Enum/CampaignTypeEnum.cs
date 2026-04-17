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
    [Description("FreeSpinBonus")]
    CampaignTypeEnum_FreeSpinBonus = 1,

    /// <summary>
    /// Free Round Bonus
    /// </summary>
    [Description("FreeRoundBonus")]
    CampaignTypeEnum_FreeRoundBonus = 2,

    /// <summary>
    /// Free Round API
    /// </summary>
    [Description("FreeRoundAPI")]
    CampaignTypeEnum_FreeRoundAPI = 3,

    /// <summary>
    /// 錦標賽
    /// </summary>
    [Description("Tournament")]
    CampaignTypeEnum_Tournament = 4,

    /// <summary>
    /// 紅包雨
    /// </summary>
    [Description("CashDrop")]
    CampaignTypeEnum_CashDrop = 5,
}
