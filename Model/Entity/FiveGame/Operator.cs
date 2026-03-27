using Tool.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Tool.Model.Entity.MySQL;


/// <summary>
/// 營運商 模型
/// </summary>
[Table("operator")]
public class Operator
{
    /// <summary>
    /// 營運商編號
    /// </summary>
    [Key]
    [Column("id")]
    public string Id { get; private set; }


    #region 基本資料

    /// <summary>
    /// 營運商名稱
    /// </summary>
    [Column("operator_name")]
    [MaxLength(50)]
    public string OperatorName { get; set; }

    /// <summary>
    /// 營運商串接聯繫窗口的 Email
    /// </summary>
    [Column("email")]
    [MaxLength(50)]
    public string? Email { get; set; }

    /// <summary>
    /// 上層代理
    /// </summary>
    [Column("agent_id")]
    public uint AgentId { get; set; }

    /// <summary>
    /// 錢包類型 (0: 共用, 1: 獨立)
    /// </summary>
    [Column("wallet_type")]
    public WalletTypeEnum WalletType { get; set; }

    /// <summary>
    /// API是否加密
    /// </summary>
    [Column("api_encryption")]
    public bool ApiEncryption { get; set; }

    /// <summary>
    /// 帳單貨幣內碼
    /// </summary>
    [Column("currency_sn")]
    public uint CurrencySn { get; set; }

    /// <summary>
    /// 顯示幣別圖示檔名編號 (顯示於遊戲中的幣別符號)
    /// </summary>
    [Column("currency_image_id")]
    public uint CurrencyImageId { get; set; }

    /// <summary>
    /// 營運商狀態
    /// </summary>
    [Column("status")]
    public OperatorStatusEnum Status { get; set; }

    /// <summary>
    /// 是否啟用反串接設定。
    /// </summary>
    [Column("bridge_enabled")]
    public bool BridgeEnabled { get; set; }

    /// <summary>
    /// 反串接編號。
    /// </summary>
    [Column("bridge_sn")]
    public uint? BridgeSn { get; set; }

    /// <summary>
    /// 是否啟用伺服器紀錄
    /// </summary>
    [Column("server_log_enabled")]
    public bool ServerLogEnabled { get; set; }

    #endregion


    #region 營運設定

    /// <summary>
    /// 網域群組編號
    /// </summary>
    [Column("domain_group_id")]
    public uint DomainGroupId { get; set; }

    /// <summary>
    /// 伺服器位址編號
    /// </summary>
    [Column("server_group_sn")]
    public uint ServerGroupSn { get; set; }

    /// <summary>
    /// 彩金伺服器編號 (Jackpot)
    /// </summary>
    [Column("jackpot_server_id")]
    public uint? JackpotServerId { get; set; }

    /// <summary>
    /// GameServer 前墜
    /// </summary>
    [Column("game_server_prefix")]
    [MaxLength(50)]
    public string? GameServerPrefix { get; set; }

    /// <summary>
    /// API 前墜
    /// </summary>
    [Column("api_prefix")]
    [MaxLength(50)]
    public string? ApiPrefix { get; set; }

    /// <summary>
    /// Download 前墜
    /// </summary>
    [Column("download_prefix")]
    [MaxLength(50)]
    public string DownloadPrefix { get; set; }

    /// <summary>
    /// API 組態編號
    /// </summary>
    [Column("api_info_id")]
    public uint ApiInfoId { get; set; }

    /// <summary>
    /// API 組態是否為參照別的營運商
    /// </summary>
    [Column("uses_external_api_info")]
    public bool UsesExternalApiInfo { get; set; }

    /// <summary>
    /// 營運商遊戲分數的小數點顯示
    /// </summary>
    [Column("decimal_display")]
    public OperatorDecimalDisplay DecimalDisplay { get; set; }

    /// <summary>
    /// 營運商遊戲分數的千分位符號
    /// </summary>
    [Column("thousands_separator")]
    public OperatorThousandsSeparator ThousandsSeparator { get; set; }

    /// <summary>
    /// 營運商遊戲的彩金分數縮寫方式
    /// </summary>
    [Column("jackpot_display")]
    public OperatorJackpotDisplay JackpotDisplay { get; set; }

    /// <summary>
    /// 帳戶單位 (單位 cent)
    /// </summary>
    [Column("accounting_unit")]
    public int AccountingUnit { get; set; }

    /// <summary>
    /// 返回首頁按鈕類型
    /// </summary>
    [Column("return_home")]
    public GameReturnButtonEnum ReturnButtonType { get; set; }

    /// <summary>
    /// 客戶站台名稱 (顯示於視窗標題)
    /// </summary>
    [Column("site_name")]
    [MaxLength(50)]
    public string? SiteName { get; set; }

    /// <summary>
    /// 是否啟動返還押注
    /// </summary>
    [Column("bet_refund_enable")]
    public bool BetRefundEnable { get; set; }

    /// <summary>
    /// 營運商圖示
    /// </summary>
    [Column("logo")]
    public string? Logo { get; set; }

    /// <summary>
    /// 遊客試玩金額
    /// </summary>
    [Column("guest_balance")]
    public ulong GuestBalance { get; set; }

    /// <summary>
    /// 是否開放遊客試玩
    /// </summary>
    [Column("guest_enable")]
    public bool GuestEnable { get; set; }

    /// <summary>
    /// 是否開放彩金 (Jackpot)
    /// </summary>
    [Column("jackpot_enable")]
    public bool JackpotEnable { get; set; }

    /// <summary>
    /// 是否開啟防休眠
    /// </summary>
    [Column("prevent_sleep_enable")]
    public bool PreventSleepEnable { get; set; }

    /// <summary>
    /// 是否開啟斷線回復盤面的功能
    /// </summary>
    [Column("recovery_enable")]
    public bool RecoveryEnable { get; set; }

    /// <summary>
    /// 免遊優先開啟
    /// </summary>
    [Column("redirect_enable")]
    public bool RedirectEnable { get; set; }

    /// <summary>
    /// 手動全屏開啟
    /// </summary>
    [Column("toggle_full_screen_enable")]
    public bool ToggleFullScreenEnable { get; set; }

    /// <summary>
    /// 備註
    /// </summary>
    [Column("note")]
    public string? Note { get; set; }

    /// <summary>
    /// 是否為營銷用途
    /// </summary>
    [Column("marketing_enabled")]
    public bool MarketingEnabled { get; set; }

    /// <summary>
    /// 是否顯示Loading Logo
    /// </summary>
    [Column("show_loading_logo")]
    public bool ShowLoadingLogo { get; set; }

    /// <summary>
    /// 是否允許系統字動調整遊戲視窗尺寸
    /// </summary>
    [Column("allow_window_resize")]
    public bool AllowWindowResize { get; set; }

    /// <summary>
    /// 是否顯示遊戲轉頻提示
    /// </summary>
    [Column("show_frequency_change_notice")]
    public bool ShowFrequencyChangeNotice { get; set; }

    /// <summary>
    /// 轉滾預滾功能 (false:前滾, true:後滾)
    /// </summary>
    [Column("reel_spin_mode")]
    public bool ReelSpinMode { get; set; }

    /// <summary>
    /// 是否開啟 GS 事件執行時間記錄
    /// </summary>
	[Obsolete("棄用，請改用 OperatorApiInfo 的 ServerLogEnabled " +
              "(最終改用 Operator.ServerLogEnabled，因為 OperatorApiInfo 是共用的，而 ServerLogEnabled 必須針對 Operator)")]
    [Column("performance_log_enabled")]
    public bool PerformanceLogEnabled { get; set; }

    #endregion

    /// <summary>
    /// RTP預設群組
    /// </summary>
    [Column("rtp_def_group")]
    public string? RtpDefGroup { get; set; }

    /// <summary>
    /// 面額群組
    /// </summary>
    [Column("chips_group_sn")]
    public uint? ChipsGroupSn { get; set; }

    /// <summary>
    /// 交收方案
    /// </summary>
    [Column("settlement_setting_id")]
    public uint? SettlementSettingId { get; set; }

    /// <summary>
    /// 負數承擔
    /// </summary>
    [Column("negative_pay")]
    public OperatorNegativePayEnum? NegativePay { get; set; }

    /// <summary>
    /// 派彩金額是否客製化
    /// </summary>
    [Column("is_payout_limit_custom")]
    public bool IsPayoutLimitCustom { get; set; }

    /// <summary>
    /// 派彩金額上限
    /// </summary>
    [Column("payout_limit_custom")]
    public uint? PayoutLimitCustom { get; set; }

    /// <summary>
    /// 派彩倍數上限
    /// </summary>
    [Column("payout_multiplier_limit")]
    public uint? PayoutMultiplierLimit { get; set; }

    /// <summary>
    /// 營運商下的首筆會員 Bet 時間
    /// </summary>
    [Column("first_bet_at")]
    public DateTimeOffset? FirstBetAt { get; set; }

    /// <summary>
    /// 創建者
    /// </summary>
    [Column("creator_id")]
    [MaxLength(36)]
    public string CreatorId { get; set; }

    /// <summary>
    /// 創建時間
    /// </summary>
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新者
    /// </summary>
    [Column("updater_id")]
    [MaxLength(36)]
    public string? UpdaterId { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// 刪除標記
    /// </summary>
    [Column("deleted")]
    public bool Deleted { get; set; }

    /// <summary>
    /// JOIN 會員
    /// </summary>
    public List<Member>? Member { get; set; }
}