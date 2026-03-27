using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 個別營運商的每分鐘下注彙總表
/// <para>排程每分鐘會去 "會員每分鐘下注彙總表" 中，統整出每個營運商該分鐘的下注</para>
/// </summary>
[CollectionName("summary_operator_minute")]
[BsonIgnoreExtraElements]
public class SummaryOperatorMinute : SummaryOperatorBase
{
}