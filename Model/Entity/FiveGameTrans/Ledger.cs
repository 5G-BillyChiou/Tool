using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tool.Model.Entity.FiveGameTrans;

[Table("ledger")]
public class Ledger
{
    [Key]
    [Column("id")]
    public long Id { get; set; } 

    [Column("member_id")]
    public string MemberId { get; set; } = string.Empty;

    [Column("type")]
    public int Type { get; set; }

    [Column("reference_id")]
    public string ReferenceId { get; set; } = string.Empty;

    [Column("currency_sn")]
    public int CurrencySn { get; set; }

    [Column("amount_cent")]
    public long AmountCent { get; set; }

    [Column("operator_id")]
    public string? OperatorId { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
