using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 個別會員針對每個遊戲的下注彙總表
/// </summary>
public class SummaryMemberGameBase : SummaryBetBase
{
    /// <summary>
    /// 下注的會員編號
    /// </summary>
    [BsonElement("member_id")]
    [BsonRepresentation(BsonType.String)]
    public string MemberId { get; set; }

    /// <summary>
    /// 下注的會員帳號
    /// </summary>
    [BsonElement("member_account")]
    [BsonRepresentation(BsonType.String)]
    public string MemberAccount { get; set; }

}