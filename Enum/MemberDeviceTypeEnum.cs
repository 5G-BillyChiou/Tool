using System.ComponentModel;

namespace Tool.Enum;

/// <summary>
/// 會員登入的裝置類型。
/// </summary>
public enum MemberDeviceTypeEnum
{
    /// <summary> 未知 </summary>
    [Description("未知")]
    MemberDeviceType_Unknown = 0,

    /// <summary> 桌機或筆電 </summary>
    [Description("桌機或筆電")]
    MemberDeviceType_PC = 1,

    /// <summary> 移動裝置 </summary>
    [Description("移動裝置")]
    MemberDeviceType_Mobile = 2,
}