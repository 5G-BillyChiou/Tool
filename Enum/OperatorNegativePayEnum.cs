using System.ComponentModel;

namespace Tool.Enum;

public enum OperatorNegativePayEnum
{
    /// <summary>
    /// 承擔
    /// </summary>
    [Description("承擔")]
    OperatorNegativePay_Enabled = 1,

    /// <summary>
    /// 不承擔
    /// </summary>
    [Description("不承擔")]
    OperatorNegativePay_Disabled = 0,
}
