using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 個別營運商的每月下注彙總表 (V1 新版本集合)
/// </summary>
[CollectionName("summary_operator_monthly_v1")]
[BsonIgnoreExtraElements]
public class SummaryOperatorMonthlyV1 : SummaryOperatorBase
{
}
