namespace Tool.Enum;

/// <summary>
/// 注單下注種類。
/// <para>使用位元為 0 ~ 7，需與 BetTypeEnum 配合計算。</para>
/// </summary>
[Flags]
public enum AccountingBetCategoryEnum
{
    None = 0,

    NormalMode = 1 << 0,
    BlitzMode = 1 << 1,

    All = NormalMode | BlitzMode,
}