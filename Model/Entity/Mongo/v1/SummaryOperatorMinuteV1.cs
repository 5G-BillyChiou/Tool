using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 個別營運商的每分鐘下注彙總表 (V1 新版本集合)
/// </summary>
[CollectionName("summary_operator_minute_v1")]
[BsonIgnoreExtraElements]
public class SummaryOperatorMinuteV1 : SummaryOperatorBase
{
}
