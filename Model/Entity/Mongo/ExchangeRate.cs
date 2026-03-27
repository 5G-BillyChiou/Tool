using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 貨幣匯率表
/// </summary>
[CollectionName("exchange_rate")]
[BsonIgnoreExtraElements]
public class ExchangeRate : BaseDocument
{
    /// <summary>
    /// 幣別代號
    /// </summary>
    [BsonElement("currency")]
    [BsonRepresentation(BsonType.String)]
    public string Currency { get; set; }

    /// <summary>
    /// 匯率列表
    /// </summary>
    [BsonElement("rate_list")]
    public RateData[] RateDatas { get; set; }

    /// <summary>
    /// 匯率的時間戳記，通常匯率每分鐘更新
    /// </summary>
    [BsonElement("timestamp")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset Timestamp { get; set; }

}


/// <summary>
/// 幣別對應的匯率資訊。
/// </summary>
public class RateData
{
    /// <summary>
    /// 幣別內碼
    /// </summary>
    public uint CurrencySn { get; set; }

    /// <summary>
    /// 幣別代號
    /// </summary>
    public string CurrencyCode { get; set; }

    /// <summary>
    /// 匯率
    /// </summary>
    public decimal Rate { get; set; }
}