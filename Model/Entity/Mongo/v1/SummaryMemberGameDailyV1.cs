using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 個別會員針對每個遊戲的每日下注彙總表 (V1 新版本集合)
/// </summary>
[CollectionName("summary_member_game_daily_v1")]
[BsonIgnoreExtraElements]
public class SummaryMemberGameDailyV1 : SummaryMemberGameBase
{
}
