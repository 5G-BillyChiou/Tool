using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 玩家餘額
/// </summary>
[CollectionName("member_wallet")]
[BsonIgnoreExtraElements]
public class MemberWallet : BaseDocument
{
    /// <summary>
    /// 玩家 ID
    /// </summary>
    [BsonElement("member_id")]
    [BsonRepresentation(BsonType.String)]
    public string MemberId { get; set; }

    /// <summary>
    /// 當前餘額
    /// </summary>
    [BsonElement("balance")]
    [BsonRepresentation(BsonType.Int64)]
    public long Balance { get; set; }

    /// <summary>
    /// 最後異動時間
    /// </summary>
    [BsonElement("updated_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset? UpdatedAt { get; set; }
}

