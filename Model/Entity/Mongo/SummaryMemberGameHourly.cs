using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 個別會員針對每個遊戲的每小時下注彙總表
/// <para>排程每小時會去 "會員每分鐘彙總表" 中，統整出每個會員該小時的下注</para>
/// </summary>
[CollectionName("summary_member_game_hourly")]
[BsonIgnoreExtraElements]
public class SummaryMemberGameHourly : SummaryMemberGameBase
{
}