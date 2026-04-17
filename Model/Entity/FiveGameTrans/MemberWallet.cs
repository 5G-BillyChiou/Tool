using System.ComponentModel.DataAnnotations.Schema;

namespace Tool.Model.Entity.FiveGameTrans;

/// <summary>
/// 玩家餘額
/// </summary>
[Table("member_wallet")]
public class MemberWallet : BaseGameTransEntity
{
    /// <summary>
    /// 玩家 ID
    /// </summary>
    [Column("member_id")]
    public string MemberId { get; set; }

    /// <summary>
    /// 當前餘額
    /// </summary>
    [Column("balance")]
    public long Balance { get; set; }
    
    /// <summary>
    /// 最後異動時間
    /// </summary>
    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.Now;
}