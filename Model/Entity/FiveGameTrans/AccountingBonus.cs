using System.ComponentModel.DataAnnotations.Schema;

namespace Tool.Model.Entity.FiveGameTrans;

/// <summary>
/// 會員遊戲紀錄（Bonus）
/// </summary>
[Table("accounting_bonus")]
public class AccountingBonus : BaseGameTransEntity
{
    /// <summary>
    /// 關聯的記錄ID（對應Accounting的Id，UUID）
    /// </summary>
    [Column("accounting_id")]
    public string AccountingId { get; set; }

    /// <summary>
    /// win
    /// </summary>
    [Column("win")]
    public ulong Win { get; set; }

    /// <summary>
    /// game_module
    /// </summary>
    [Column("game_module")]
    public string GameModule { get; set; }

    /// <summary>
    /// game data
    /// </summary>
    [Column("game_data")]
    public string GameData { get; set; }

    /// <summary>
    /// game_result
    /// </summary>
    [Column("game_result")]
    public string GameResult { get; set; }

    /// <summary>
    /// 是否遊戲斷線
    /// </summary>
    [Column("offline")]
    public bool Offline { get; set; }

    /// <summary>
    /// game time
    /// </summary>
    [Column("game_time")]
    public DateTimeOffset GameTime { get; set; }
}