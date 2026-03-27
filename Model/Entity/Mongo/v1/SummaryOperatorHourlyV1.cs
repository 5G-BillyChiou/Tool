using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 個別營運商的每小時下注彙總表 (V1 新版本集合)
/// </summary>
[CollectionName("summary_operator_hourly_v1")]
[BsonIgnoreExtraElements]
public class SummaryOperatorHourlyV1 : SummaryOperatorBase
{
}
