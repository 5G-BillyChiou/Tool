using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 個別會員針對每個遊戲的每月下注彙總表
/// <para>排程每月會去 "會員每日彙總表" 中，統整出每個會員該月的下注</para>
/// </summary>
[CollectionName("summary_member_game_monthly")]
[BsonIgnoreExtraElements]
public class SummaryMemberGameMonthly : SummaryMemberGameBase
{
}