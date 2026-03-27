using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Tool.Enum;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 下注彙總表
/// </summary>
public class SummaryBetBase : SummaryBase
{
    /// <summary>
    /// 下注的遊戲館編號
    /// </summary>
    [BsonElement("game_id")]
    [BsonRepresentation(BsonType.String)]
    public string GameId { get; set; }

    /// <summary>
    /// 下注的幣別代號
    /// <para>如果是MonthlyTop系列的彙總，都轉成USD</para>
    /// </summary>
    [BsonElement("currency_sn")]
    [BsonRepresentation(BsonType.Int32)]
    public uint CurrencySn { get; set; }

    /// <summary>
    /// 總下注次數
    /// </summary>
    [BsonElement("total_bet_count")]
    [BsonRepresentation(BsonType.Int32)]
    public int TotalBetCount { get; set; }

    /// <summary>
    /// 總下注額
    /// </summary>
    [BsonElement("total_bet_amount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalBetAmount { get; set; }

    /// <summary>
    /// 總獲得派彩金額
    /// <para>(該金額就是遊戲中所有會獲得的獎金總和)</para>
    /// </summary>
    [BsonElement("total_payout")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalPayout { get; set; }

    /// <summary>
    /// 總獲得紅利金額
    /// <para>(FreeGame + BonusGame + 幸運一擊)</para>
    /// </summary>
    [BsonElement("total_bonus")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalBonus { get; set; }

    /// <summary>
    /// 總獲得活動彩金金額
    /// <para>(額外的特殊活動)</para>
    /// </summary>
    [BsonElement("total_promotion_bonus")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalPromotionBonus { get; set; }

    /// <summary>
    /// 總獲得彩金金額
    /// <para>(僅限 Jackpot)</para>
    /// </summary>
    [BsonElement("total_jackpot")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalJackpot { get; set; }

    ///// <summary>
    ///// 會員登入次數(遊戲人次)
    ///// </summary>
    //[BsonElement("total_login_count")]
    //[BsonRepresentation(BsonType.Int32)]
    //public int TotalLoginCount { get; set; }

    //----------------------------BetCategory------------------------

    /// <summary>
    /// 注單下注種類
    /// </summary>
    [BsonElement("bet_category")]
    [BsonRepresentation(BsonType.Int32)]
    public AccountingBetCategoryEnum BetCategory { get; set; }

    //----------------------------BasicBet------------------------

    /// <summary>
    /// 總下注次數
    /// </summary>
    [BsonElement("total_basic_bet_count")]
    [BsonRepresentation(BsonType.Int32)]
    public int TotalBasicBetCount { get; set; }

    /// <summary>
    /// 總下注額
    /// </summary>
    [BsonElement("total_basic_bet_amount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalBasicBetAmount { get; set; }

    /// <summary>
    /// 總獲得派彩金額
    /// <para>(該金額就是遊戲中所有會獲得的獎金總和)</para>
    /// </summary>
    [BsonElement("total_basic_payout")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalBasicPayout { get; set; }

    //----------------------------ExtraBet-----------------------

    /// <summary>
    /// 總下注次數
    /// </summary>
    [BsonElement("total_extra_bet_count")]
    [BsonRepresentation(BsonType.Int32)]
    public int TotalExtraBetCount { get; set; }

    /// <summary>
    /// 總下注額
    /// </summary>
    [BsonElement("total_extra_bet_amount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalExtraBetAmount { get; set; }

    /// <summary>
    /// 總獲得派彩金額
    /// <para>(該金額就是遊戲中所有會獲得的獎金總和)</para>
    /// </summary>
    [BsonElement("total_extra_payout")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalExtraPayout { get; set; }

    //--------------------------FeatureBuy---------------------------

    /// <summary>
    /// 總下注次數
    /// </summary>
    [BsonElement("total_feature_buy_bet_count")]
    [BsonRepresentation(BsonType.Int32)]
    public int TotalFeatureBuyBetCount { get; set; }

    /// <summary>
    /// 總下注額
    /// </summary>
    [BsonElement("total_feature_buy_bet_amount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalFeatureBuyBetAmount { get; set; }

    /// <summary>
    /// 總獲得派彩金額
    /// <para>(該金額就是遊戲中所有會獲得的獎金總和)</para>
    /// </summary>
    [BsonElement("total_feature_buy_payout")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalFeatureBuyPayout { get; set; }

    //------------------------------------------------------------

    /// <summary>
    /// 遊戲贏分
    /// </summary>
    [BsonIgnore]
    public decimal GameProfit
    {
        get
        {
            // 下注金額 - (派彩 + 紅利 + 活動彩金 + Jackpot)
            return TotalBetAmount - TotalPayout;
        }
    }

    /// <summary>
    /// 驗證彙總資料數據是否一致。
    /// </summary>
    public virtual bool ValidateSummaryMatch(SummaryBetBase target)
    {
        if (target is null)
            return false;

        return CurrencySn == target.CurrencySn &&
               TotalBetCount == target.TotalBetCount &&
               TotalBetAmount == target.TotalBetAmount &&
               TotalPayout == target.TotalPayout &&
               TotalBonus == target.TotalBonus &&
               TotalPromotionBonus == target.TotalPromotionBonus &&
               TotalJackpot == target.TotalJackpot &&
               //TotalLoginCount == target.TotalLoginCount &&

               TotalBasicBetCount == target.TotalBasicBetCount &&
               TotalBasicBetAmount == target.TotalBasicBetAmount &&
               TotalBasicPayout == target.TotalBasicPayout &&

               TotalExtraBetCount == target.TotalExtraBetCount &&
               TotalExtraBetAmount == target.TotalExtraBetAmount &&
               TotalExtraPayout == target.TotalExtraPayout &&

               TotalFeatureBuyBetCount == target.TotalFeatureBuyBetCount &&
               TotalFeatureBuyBetAmount == target.TotalFeatureBuyBetAmount &&
               TotalFeatureBuyPayout == target.TotalFeatureBuyPayout;
    }
}