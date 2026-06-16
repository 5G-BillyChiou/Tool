using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;

/// <summary>
/// 營運商新增人數彙總表
/// </summary>
public class SummaryOperatorNewMemberBase : SummaryBase
{
    /// <summary>
    /// 新增人數
    /// </summary>
    [BsonElement("new_member_count")]
    [BsonRepresentation(BsonType.Int32)]
    public long NewMemberCount { get; set; }

    /// <summary>
    /// 驗證彙總資料數據是否一致。
    /// </summary>
    public virtual bool ValidateSummaryMatch(SummaryOperatorNewMemberBase target)
    {
        if (target is null)
            return false;

        return NewMemberCount == target.NewMemberCount;
    }
}
