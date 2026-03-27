using Tool.Enum;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 會員遊戲下注紀錄(結果區段)
/// </summary>
[CollectionName("pre_accounting_result")]
[BsonIgnoreExtraElements]
public class PreAccountingResult : BaseDocument
{
    /// <summary>
    /// 屬於哪個代理商
    /// </summary>
    [BsonElement("agent_id")]
    [BsonRepresentation(BsonType.Int32)]
    public uint AgentId { get; set; }

    /// <summary>
    /// 代理商階層 Path
    /// <para>該指主要判斷該時期的代理商所屬 (因為會有轉出轉入問題)</para>
    /// </summary>
    [BsonElement("agent_path")]
    [BsonRepresentation(BsonType.String)]
    public string AgentPath { get; set; }

    /// <summary>
    /// 營運商ID
    /// </summary>
    [BsonElement("operator_id")]
    [BsonRepresentation(BsonType.String)]
    public string OperatorId { get; set; }

    /// <summary>
    /// 下注的幣別內碼
    /// 原Currency改為CurrencySn
    /// </summary>
    [BsonElement("currency_sn")]
    [BsonRepresentation(BsonType.Int32)]
    public uint CurrencySn { get; set; }

    /// <summary>
    /// 遊戲 ID
    /// </summary>
    [BsonElement("game_id")]
    [BsonRepresentation(BsonType.String)]
    public string GameId { get; set; }

    /// <summary>
    /// 會員編號
    /// </summary>
    [BsonElement("member_id")]
    [BsonRepresentation(BsonType.String)]
    public string MemberId { get; set; }

    /// <summary>
    /// 會員帳號
    /// </summary>
    [BsonElement("member_account")]
    [BsonRepresentation(BsonType.String)]
    public string MemberAccount { get; set; }

    /// <summary>
    /// 起始金額
    /// </summary>
    [BsonElement("init_cent")]
    [BsonRepresentation(BsonType.Int64)]
    public ulong InitCent { get; set; }

    /// <summary>
    /// 面額
    /// </summary>
    [BsonElement("denom")]
    [BsonRepresentation(BsonType.Int32)]
    public long Denom { get; set; }

    /// <summary>
    /// 押注
    /// </summary>
    [BsonElement("bet")]
    [BsonRepresentation(BsonType.Int64)]
    public long Bet { get; set; }

    /// <summary>
    /// 總贏分 (+/-)
    /// </summary>
    [BsonElement("total_win")]
    [BsonRepresentation(BsonType.Int64)]
    public ulong TotalWin { get; set; }

    /// <summary>
    /// Module ID
    /// </summary>
    [BsonElement("game_module")]
    [BsonRepresentation(BsonType.String)]
    public string GameModule { get; set; }

    /// <summary>
    /// 是否有進 Bonus (FreeSpin)
    /// </summary>
    [BsonElement("bonus")]
    [BsonRepresentation(BsonType.Boolean)]
    public bool Bonus { get; set; }

    /// <summary>
    /// 活動類型
    /// </summary>
    [BsonElement("campaign_type")]
    [BsonRepresentation(BsonType.Int32)]
    public CampaignTypeEnum CampaignType { get; set; }

    /// <summary>
    /// 結束金額
    /// </summary>
    [BsonElement("end_cent")]
    [BsonRepresentation(BsonType.Int64)]
    public ulong EndCent { get; set; }

    /// <summary>
    /// 遊戲是否結束
    /// </summary>
    [BsonElement("game_end")]
    [BsonRepresentation(BsonType.Int32)]
    public AccountingGameEndEnum GameEnd { get; set; }

    /// <summary>
    /// 遊戲資訊
    /// </summary>
    [BsonElement("game_data")]
    [BsonRepresentation(BsonType.String)]
    public string GameData { get; set; }

    /// <summary>
    /// 遊戲盤面結果
    /// </summary>
    [BsonElement("game_result")]
    [BsonRepresentation(BsonType.String)]
    public string GameResult { get; set; }

    /// <summary>
    /// 活動ID
    /// </summary>
    [BsonElement("campaign_id")]
    [BsonRepresentation(BsonType.String)]
    public string? CampaignId { get; set; }

    /// <summary>
    /// 注單類型 (Flags)。
    /// <para>該值為 AccountingBetTypeEnum + AccountingBetCategoryEnum 的結果</para>
    /// </summary>
    [BsonElement("bet_type_mask")]
    [BsonRepresentation(BsonType.Int32)]
    public int BetTypeMask { get; set; }
}