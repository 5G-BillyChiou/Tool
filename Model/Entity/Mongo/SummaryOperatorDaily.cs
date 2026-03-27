using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 個別營運商的每日下注彙總表
/// <para>排程每日會去 "營運每小時下注彙總表" 中，統整出每個營運商該日的下注</para>
/// </summary>
[CollectionName("summary_operator_daily")]
[BsonIgnoreExtraElements]
public class SummaryOperatorDaily : SummaryOperatorBase
{
}