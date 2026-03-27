namespace Tool.Enum;

/// <summary>
/// 遊戲玩家狀態
/// </summary>
public enum MemberStatusEnum
{
    /// <summary>
    /// 未知
    /// </summary>
    MemberStatus_Unknown = -2,

    /// <summary>
    /// 訪客
    /// </summary>
    MemberStatus_Guest = -1,

    /// <summary>
    /// 鎖定
    /// </summary>
    MemberStatus_Locked = 0,

    /// <summary>
    /// 正常使用
    /// </summary>
    MemberStatus_Normal = 1,

    /// <summary>
    /// 測試
    /// </summary>
    MemberStatus_Testing = 2,

    /// <summary>
    /// 風險玩家
    /// </summary>
    MemberStatus_Risky = 3
}