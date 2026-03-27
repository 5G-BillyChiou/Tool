using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tool.Model.Entity.FiveGame;

/// <summary>
/// 玩家連線資訊
/// </summary>
[Table("member_session")]
public class MemberSession
{
    /// <summary>
    /// Sedion ID
    /// </summary>
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public string Id { get; set; }

    /// <summary>
    /// 屬於哪個代理商
    /// </summary>
    [Column("agent_id")]
    public uint AgentId { get; set; }

    /// <summary>
    /// 營運商ID
    /// </summary>
    [Column("operator_id")]
    public string OperatorId { get; set; }

    /// <summary>
    /// 會員ID
    /// </summary>
    [Column("member_id")]
    public string MemberId { get; set; }

    /// <summary>
    /// 會員帳號
    /// </summary>
    [Column("member_account")]
    public string MemberAccount { get; set; }

    /// <summary>
    /// 遊戲代號
    /// </summary>
    [Column("game_id")]
    public string GameId { get; set; }

    /// <summary>
    /// 主機ID
    /// </summary>
    [Column("server_id")]
    public string ServerId { get; set; }

    /// <summary>
    /// 是否被踢出的標記
    /// </summary>
    [Column("kick_out")]
    public bool KickOut { get; set; }

    /// <summary>
    /// 被踢出的時間
    /// </summary>
    [Column("kick_out_at")]
    public DateTimeOffset? KickOutAt { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}