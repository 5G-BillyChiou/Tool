using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 彙總表資訊
/// </summary>
public class SummaryBase : BaseDocument
{
    /// <summary>
    /// 彙總的時間區間 - 起
    /// </summary>
    [BsonElement("period_start_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset PeriodStartAt { get; set; }

    /// <summary>
    /// 彙總的時間區間 - 迄
    /// </summary>
    [BsonElement("period_end_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset PeriodEndAt { get; set; }

    /// <summary>
    /// 屬於哪個代理商
    /// </summary>
    [BsonElement("agent_id")]
    [BsonRepresentation(BsonType.Int32)]
    public uint AgentId { get; set; }

    /// <summary>
    /// 代理商階層 Path
    /// <para>該指主要判斷該時期的代理商所屬 (因為會有轉出轉入問題)</para>
    /// </summary>
    [BsonElement("agent_path")]
    [BsonRepresentation(BsonType.String)]
    public string AgentPath { get; set; }

    /// <summary>
    /// 屬於哪個營運商
    /// </summary>
    [BsonElement("operator_id")]
    [BsonRepresentation(BsonType.String)]
    public string OperatorId { get; set; }

    /// <summary>
    /// 記錄資料所屬的時區。
    /// </summary>
    [BsonElement("timezone")]
    [BsonRepresentation(BsonType.String)]
    public string? Timezone { get; set; }

    /// <summary>
    /// 最後異動時間
    /// </summary>
    [BsonElement("updated_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset? UpdatedAt { get; set; }
}