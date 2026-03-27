using System.ComponentModel;

namespace Tool.Enum;

/// <summary>
/// 營運商遊戲分數的千分位符號。
/// <para>這Description有用在Excel匯入時比對取得enum</para>
/// </summary>
public enum OperatorThousandsSeparator
{
    /// <summary>
    /// 逗號符號。
    /// <para>當整數部分變成逗號符號符號表示時，那小數點會變成小數點符號，及 互換符號 </para>
    /// </summary>
    [Description("逗號符號")]
    OperatorThousandsSeparator_Comma = 0,

    /// <summary>
    /// 小數點符號。
    /// <para>當整數部分變成小數點符號表示時，那小數點會變成千分號，及 互換符號 </para>
    /// </summary>
    [Description("小數點符號")]
    OperatorThousandsSeparator_Period = 1
}