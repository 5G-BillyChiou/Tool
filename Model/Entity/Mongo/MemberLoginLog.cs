using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Tool.Enum;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 會員登入紀錄
/// </summary>
[CollectionName("member_login_log")]
[BsonIgnoreExtraElements]
public class MemberLoginLog : BaseDocument
{
    /// <summary>
    /// 登出時間
    /// </summary>
    [BsonElement("logout_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset? LogoutAt { get; set; }

    /// <summary>
    /// 會員編號
    /// </summary>
    [BsonElement("member_id")]
    [BsonRepresentation(BsonType.String)]
    public string MemberId { get; set; }

    /// <summary>
    /// 營運商
    /// </summary>
    [BsonElement("operator_id")]
    [BsonRepresentation(BsonType.String)]
    public string OperatorId { get; set; }

    /// <summary>
    /// 登入哪個遊戲館
    /// </summary>
    [BsonElement("game_id")]
    [BsonRepresentation(BsonType.String)]
    public string GameId { get; set; }

    /// <summary>
    /// 登入時的裝置 IP 位置
    /// </summary>
    [BsonElement("ip")]
    [BsonRepresentation(BsonType.String)]
    public string Ip { get; set; }

    /// <summary>
    /// 登入時的裝置類型
    /// </summary>
    [BsonElement("device_type")]
    [BsonRepresentation(BsonType.String)]
    public MemberDeviceTypeEnum DeviceType { get; set; }

    /// <summary>
    /// 裝置敘述
    /// </summary>
    [BsonElement("device_description")]
    [BsonRepresentation(BsonType.String)]
    public string DeviceDescription { get; set; }

    /// <summary>
    /// 是否為測試帳號
    /// </summary>
    [BsonElement("test_account")]
    [BsonRepresentation(BsonType.Boolean)]
    public bool TestAccount { get; set; }

}