using System.ComponentModel;

namespace Tool.Enum;

/// <summary>
/// 營運商遊戲的彩金分數縮寫方式。
/// </summary>
public enum OperatorJackpotDisplay
{
    /// <summary>
    /// 完整數字顯示。
    /// </summary>
    [Description("完整數字顯示")]
    OperatorJackpotDisplay_FullNumber = 0,

    /// <summary>
    /// 以 K 替代千 (1,000)。
    /// </summary>
    [Description("以 K 替代千 (1,000)")]
    OperatorJackpotDisplay_K = 1,

    /// <summary>
    /// 以 M 替代百萬 (1,000,000)。
    /// </summary>
    [Description("以 M 替代百萬 (1,000,000)")]
    OperatorJackpotDisplay_M = 2,

    /// <summary>
    /// 以 B 替代十億 (1,000,000,000)。
    /// </summary>
    [Description("以 B 替代十億 (1,000,000,000)")]
    OperatorJackpotDisplay_B = 3,

    /// <summary>
    /// 以 T 替代兆 (1,000,000,000,000)。
    /// </summary>
    [Description("以 T 替代兆 (1,000,000,000,000)")]
    OperatorJackpotDisplay_T = 4,

}