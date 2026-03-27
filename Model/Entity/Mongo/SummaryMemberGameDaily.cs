using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 個別會員針對每個遊戲的每日下注彙總表
/// <para>排程每日會去 "會員每小時彙總表" 中，統整出每個會員該日的下注</para>
/// </summary>
[CollectionName("summary_member_game_daily")]
[BsonIgnoreExtraElements]
public class SummaryMemberGameDaily : SummaryMemberGameBase
{
}