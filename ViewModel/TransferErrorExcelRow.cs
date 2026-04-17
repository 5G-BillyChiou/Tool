namespace Tool.ViewModel;

/// <summary>
/// 匯出轉帳錯誤 Excel 列資料
/// </summary>
public class TransferErrorExcelRow
{
    public string TxnId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string MemberAccount { get; set; } = string.Empty;
    public string TransferAt { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TransferCent { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}