using System.ComponentModel;

namespace Tool.Enum;

/// <summary>
/// 會員轉帳紀錄交易類型
/// </summary>
public enum MemberTransferTypeEnum
{
    /// <summary>
    /// 存入
    /// </summary>
    [Description("存入")]
    MemberTransferType_Deposit = 1,

    /// <summary>
    /// 提出
    /// </summary>
    [Description("提出")]
    MemberTransferType_Withdraw = 2,
}
