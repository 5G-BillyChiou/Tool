using System.ComponentModel.DataAnnotations.Schema;
using Tool.Enum;

namespace Tool.Model.Entity.FiveGameTrans;

/// <summary>
/// 會員遊戲下注紀錄(結果區段)
/// </summary>
[Table("pre_accounting_result")]
public class PreAccountingResult : BaseGameTransEntity
{
    /// <summary>
    /// 屬於哪個代理商
    /// </summary>
    [Column("agent_id")]
    public uint AgentId { get; set; }

    /// <summary>
    /// 代理商階層 Path
    /// </summary>
    [Column("agent_path")]
    public string AgentPath { get; set; }

    /// <summary>
    /// 營運商ID
    /// </summary>
    [Column("operator_id")]
    public string OperatorId { get; set; }

    /// <summary>
    /// 下注的幣別內碼
    /// </summary>
    [Column("currency_sn")]
    public uint CurrencySn { get; set; }

    /// <summary>
    /// 遊戲 ID
    /// </summary>
    [Column("game_id")]
    public string GameId { get; set; }

    /// <summary>
    /// 會員編號
    /// </summary>
    [Column("member_id")]
    public string MemberId { get; set; }

    /// <summary>
    /// 會員帳號
    /// </summary>
    [Column("member_account")]
    public string MemberAccount { get; set; }

    /// <summary>
    /// 起始金額
    /// </summary>
    [Column("init_cent")]
    public ulong InitCent { get; set; }

    /// <summary>
    /// 實際的基礎下注額 (base * level * denom)
    /// </summary>
    [Column("actual_bet")]
    public long ActualBet { get; set; }

    /// <summary>
    /// 面額
    /// </summary>
    [Column("denom")]
    public long Denom { get; set; }

    /// <summary>
    /// 押注
    /// </summary>
    [Column("bet")]
    public long Bet { get; set; }

    /// <summary>
    /// 總贏分 (+/-)
    /// </summary>
    [Column("total_win")]
    public ulong TotalWin { get; set; }

    /// <summary>
    /// Module ID
    /// </summary>
    [Column("game_module")]
    public string? GameModule { get; set; }

    /// <summary>
    /// 是否有進 Bonus (FreeSpin)
    /// </summary>
    [Column("bonus")]
    public bool Bonus { get; set; }

    /// <summary>
    /// 活動類型
    /// </summary>
    [Column("campaign_type")]
    public CampaignTypeEnum CampaignType { get; set; }

    /// <summary>
    /// 結束金額
    /// </summary>
    [Column("end_cent")]
    public ulong EndCent { get; set; }

    /// <summary>
    /// 遊戲是否結束
    /// </summary>
    [Column("game_end")]
    public AccountingGameEndEnum GameEnd { get; set; }

    /// <summary>
    /// 遊戲資訊
    /// </summary>
    [Column("game_data")]
    public string? GameData { get; set; }

    /// <summary>
    /// 遊戲盤面結果
    /// </summary>
    [Column("game_result")]
    public string? GameResult { get; set; }

    /// <summary>
    /// 供前端回放用的 proto
    /// </summary>
    [Column("replay_data")]
    public byte[]? ReplayData { get; set; }

    /// <summary>
    /// 活動ID
    /// </summary>
    [Column("campaign_id")]
    public string? CampaignId { get; set; }

    /// <summary>
    /// 注單類型 (Flags)。
    /// <para>該值為 AccountingBetTypeEnum + AccountingBetCategoryEnum 的結果</para>
    /// </summary>
    [Column("bet_type_mask")]
    public int BetTypeMask { get; set; }
    
    /// <summary>
    /// 注單狀態標記（Flags）。
    /// <para>該值為 AccountingStatusEnum 的結果</para>
    /// </summary>
    [Column("status_mask")]
    public int StatusMask { get; set; }
}
