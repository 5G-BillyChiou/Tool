using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 營運商每小時新增人數彙總表
/// </summary>
[CollectionName("summary_operator_new_member_daily")]
[BsonIgnoreExtraElements]
public class SummaryOperatorNewMemberDaily : SummaryOperatorNewMemberBase
{
}