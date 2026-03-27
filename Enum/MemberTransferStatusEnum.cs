using System.ComponentModel;

namespace Tool.Enum;

/// <summary>
/// 會員轉帳紀錄交易狀態
/// </summary>
public enum MemberTransferStatusEnum
{
    /// <summary>
    /// 成功
    /// </summary>
    [Description("成功")]
    MemberTransferStatus_Success = 1,

    /// <summary>
    /// 失敗
    /// </summary>
    [Description("失敗")]
    MemberTransferStatus_Fail = 2,

    /// <summary>
    /// 交易中
    /// </summary>
    [Description("交易中")]
    MemberTransferStatus_Processing = 3

}