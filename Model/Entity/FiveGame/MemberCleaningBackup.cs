using Tool.Enum;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tool.Model.Entity.FiveGame;


/// <summary>
/// 使用者(玩家)資訊
/// </summary>
[Table("member_cleaning_backup")]
public class MemberCleaningBackup
{
    /// <summary>
    /// 編號
    /// </summary>
    [Column("id")]
    [Key]
    public string Id { get; set; }

    /// <summary>
    /// 營運商
    /// </summary>
    [Column("operator_id")]
    public string OperatorId { get; set; }

    /// <summary>
    /// 帳號
    /// </summary>
    [Column("account")]
    public string Account { get; set; }

    /// <summary>
    /// 密碼
    /// </summary>
    [Column("password")]
    public string? Password { get; set; }

    /// <summary>
    /// 狀態 (-2 = 未知、-1 = 訪客、0 = 鎖定、1 = 正常使用、2 = 測試、3 = 風險玩家)
    /// </summary>
    [Column("status")]
    public MemberStatusEnum Status { get; set; }

    /// <summary>
    /// 預設 Rate 索引
    /// </summary>
    [Column("default_rate_idx")]
    public ushort? DefaultRateIdx { get; set; }

    /// <summary>
    /// 首次下注時間(用來統計遊戲新增人數)
    /// </summary>
    [Column("frist_account_at")]
    public DateTimeOffset? FristAccountAt { get; set; }

    /// <summary>
    /// 最後登入時間
    /// </summary>
    [Column("last_login_at")]
    public DateTimeOffset LastLoginAt { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    [Column("created_at")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTimeOffset CreatedAt { get; set; }
}