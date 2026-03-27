using Tool.Model.Entity.MySQL;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Tool.Enum;

namespace Tool.Model.Entity.FiveGame;


/// <summary>
/// MemberTransferLog 模型
/// </summary>
[Table("member_transfer_log")]
public class MemberTransferLog
{
    /// <summary>
    /// 流水號
    /// </summary>
    [Key]
    [Column("sn")]
    public uint Sn { get; set; }

    /// <summary>
    /// 交易單號
    /// </summary>
    [Column("txn_id")]
    public string TxnId { get; set; }

    /// <summary>
    /// 會員內碼
    /// </summary>
    [Column("member_id")]
    public string MemberId { get; set; }

    /// <summary>
    /// 營運商
    /// </summary>
    [Column("operator_id")]
    public string OperatorId { get; set; }

    /// <summary>
    /// 幣別 sn
    /// </summary>
    [Column("currency_sn")]
    public uint CurrencySn { get; set; }

    /// <summary>
    /// 交易類型 (1 = 存入、2 = 提出)
    /// </summary>
    [Column("type")]
    public MemberTransferTypeEnum Type { get; set; }

    /// <summary>
    /// 交易時間
    /// </summary>
    [Column("transfer_at")]
    public DateTimeOffset TransferAt { get; set; }

    /// <summary>
    /// 交易前金額
    /// </summary>
    [Column("before_cent")]
    public long BeforeCent { get; set; }

    /// <summary>
    /// 交易金額
    /// </summary>
    [Column("transfer_cent")]
    public long TransferCent { get; set; }

    /// <summary>
    /// 交易後金額
    /// </summary>
    [Column("after_cent")]
    public long AfterCent { get; set; }

    /// <summary>
    /// 交易狀態
    /// </summary>
    [Column("status")]
    public MemberTransferStatusEnum Status { get; set; }

    /// <summary>
    /// 備註
    /// </summary>
    [Column("note")]
    public string? Note { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// JOIN 營運商
    /// </summary>
    [ForeignKey("OperatorId")]
    public Operator Operator { get; set; }

    /// <summary>
    /// JOIN 會員
    /// </summary>
    [ForeignKey("MemberId")]
    public Member Member { get; set; }
}