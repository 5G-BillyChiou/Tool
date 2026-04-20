using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 個別營運商的每小時下注彙總表 (V1 新版本集合)
/// </summary>
[CollectionName("summary_bigquery_operator_hourly")]
[BsonIgnoreExtraElements]
public class SummaryBigQueryOperatorHourly : SummaryOperatorBase
{
}
