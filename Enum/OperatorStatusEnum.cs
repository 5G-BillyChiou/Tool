using System.ComponentModel;

namespace Tool.Enum;

/// <summary>
/// 營運商狀態
/// </summary>
public enum OperatorStatusEnum
{
    /// <summary>
    /// 停用
    /// </summary>
    [Description("停用")]
    OperatorStatus_Disabled = 0,

    /// <summary>
    /// 啟用
    /// </summary>
    [Description("啟用")]
    OperatorStatus_Enabled = 1
}