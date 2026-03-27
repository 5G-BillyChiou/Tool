using System.ComponentModel;

namespace Tool.Enum;

public enum AccountingGameEndEnum
{
    /// <summary>
    /// 未結束
    /// </summary>
    [Description("未結束")]
    AccountingGameEnd_NotEnd = 0,

    /// <summary>
    /// 已結束
    /// </summary>
    [Description("已結束")]
    AccountingGameEnd_End = 1,
}
