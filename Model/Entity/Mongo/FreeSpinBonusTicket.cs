using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 贈送免費旋轉票券
/// </summary>
[CollectionName("free_spin_bonus_ticket")]
[BsonIgnoreExtraElements]
public class FreeSpinBonusTicket : BaseDocument
{
    /// <summary>
    /// 營運商編號
    /// </summary>
    [BsonElement("operator_id")]
    [BsonRepresentation(BsonType.String)]
    public string OperatorId { get; set; }

    /// <summary>
    /// 會員編號
    /// </summary>
    [BsonElement("member_id")]
    [BsonRepresentation(BsonType.String)]
    public string MemberId { get; set; }

    /// <summary>
    /// 活動ID
    /// </summary>
    [BsonElement("campaign_id")]
    [BsonRepresentation(BsonType.String)]
    public string CampaignId { get; set; }

    /// <summary>
    /// 遊戲ID
    /// </summary>
    [BsonElement("game_id")]
    [BsonRepresentation(BsonType.String)]
    public string GameId { get; set; }

    /// <summary>
    /// 獲得的獎勵門檻
    /// </summary>
    [BsonElement("threshold")]
    [BsonRepresentation(BsonType.Int32)]
    public uint Threshold { get; set; }

    /// <summary>
    /// 票券的面額
    /// </summary>
    [BsonElement("rate")]
    [BsonRepresentation(BsonType.Int32)]
    public uint Rate { get; set; }

    /// <summary>
    /// 票券的押注額
    /// </summary>
    [BsonElement("bet")]
    [BsonRepresentation(BsonType.Int32)]
    public uint Bet { get; set; }

    /// <summary>
    /// 派彩倍數上限
    /// </summary>
    [BsonElement("payout_multiplier_limit")]
    [BsonRepresentation(BsonType.Int32)]
    public int PayoutMultiplierLimit { get; set; }

    /// <summary>
    /// 獲得的獎勵次數
    /// </summary>
    [BsonElement("reward_count")]
    [BsonRepresentation(BsonType.Int32)]
    public uint RewardCount { get; set; }

    /// <summary>
    /// 結束FSB的時間
    /// </summary>
    [BsonElement("finished_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    /// FSB的更新時間
    /// 發生於最後結算以及中斷斷線
    /// </summary>
    [BsonElement("updated_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// 總贏分 (+/-)
    /// </summary>
    [BsonElement("total_win")]
    [BsonRepresentation(BsonType.Int64)]
    public ulong? TotalWin { get; set; }

    /// <summary>
    /// 加密驗證資訊
    /// </summary>
    [BsonElement("encrypted")]
    public byte[] Encrypted { get; set; } = Array.Empty<byte>();

}