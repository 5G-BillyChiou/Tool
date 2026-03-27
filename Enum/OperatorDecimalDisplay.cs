using System.ComponentModel;

namespace Tool.Enum;

/// <summary>
/// 營運商遊戲分數的小數、貨幣顯示。
/// <para>這Description有用在Excel匯入時比對取得enum</para>
/// </summary>
public enum OperatorDecimalDisplay
{
    /// <summary>
    /// 顯示小數。
    /// </summary>
    [Description("顯示小數")]
    OperatorDecimalDisplay_Enabled = 0,

    /// <summary>
    /// 不顯示小數。
    /// </summary>
    [Description("不顯示小數")]
    OperatorDecimalDisplay_Disabled = 1,

    ///// <summary>
    ///// 顯示小數與貨幣。
    ///// </summary>
    //[Description("顯示小數與貨幣")]
    //OperatorDecimalDisplay_Currency = 2, 

}
