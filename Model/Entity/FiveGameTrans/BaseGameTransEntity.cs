using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tool.Model.Entity.FiveGameTrans;

public class BaseGameTransEntity
{
    /// <summary>
    /// 主鍵 Id
    /// </summary>
    [Key]
    [Column("id")]
    public virtual string Id { get; set; } = ObjectId.GenerateNewId().ToString(); //WarmDB 切換前必須相容 MongoDB 的 ObjectId

    /// <summary>
    /// 建立時間
    /// </summary>
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}