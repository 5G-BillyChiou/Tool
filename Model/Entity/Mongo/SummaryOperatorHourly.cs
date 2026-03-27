using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 個別營運商的每小時下注彙總表
/// <para>排程每小時會去 "營運每分鐘下注彙總表" 中，統整出每個營運商該小時的下注</para>
/// </summary>
[CollectionName("summary_operator_hourly")]
[BsonIgnoreExtraElements]
public class SummaryOperatorHourly : SummaryOperatorBase
{
}