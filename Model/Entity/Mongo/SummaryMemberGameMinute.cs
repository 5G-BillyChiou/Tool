using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 個別會員針對每個遊戲的每分鐘下注彙總表
/// <para>排程每分鐘會去 "下注紀錄" 中，統整出每個會員該分鐘的下注</para>
/// </summary>
[CollectionName("summary_member_game_minute")]
[BsonIgnoreExtraElements]
public class SummaryMemberGameMinute : SummaryMemberGameBase
{
}