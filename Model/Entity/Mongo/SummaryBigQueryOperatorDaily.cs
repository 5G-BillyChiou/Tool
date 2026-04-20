using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 個別營運商的每日下注彙總表 (V1 新版本集合)
/// </summary>
[CollectionName("summary_bigquery_operator_daily")]
[BsonIgnoreExtraElements]
public class SummaryBigQueryOperatorDaily : SummaryOperatorBase
{
}
