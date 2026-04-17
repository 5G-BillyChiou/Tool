using Microsoft.EntityFrameworkCore;
using NPOI.XSSF.UserModel;
using Tool.Enum;
using Tool.Helper;
using Tool.Model.Entity.FiveGame;
using Tool.Model.Entity.FiveGameTrans;
using Tool.Model.Repository.FiveGame;
using Tool.Model.Repository.FiveGameTrans;
using Tool.ViewModel;
using Tool.ViewModel.Npoi;

namespace Tool.Service;

/// <summary>
/// 會員轉帳紀錄 相關服務
/// </summary>
public class MemberTransferLogService(IMemberTransferLogRepository _memberTransferLogRepository,
                                        IMemberRepository _memberRepository,
                                        IOperatorRepository _operatorRepository,
                                        INpoiHelper _npoiHelper) : IMemberTransferLogService
{
    /// <summary>
    /// 匯出轉帳紀錄 儲值失敗
    /// </summary>
    public MemoryStream ExportTransferError()
    {
        var start = new DateTimeOffset(2026, 4, 15, 04, 57, 28, TimeSpan.Zero);
        var endAt = new DateTimeOffset(2026, 4, 15, 06, 35, 17, TimeSpan.Zero);

        Console.WriteLine($"[ExportTransferError] 開始查詢 TransferLog，時間範圍: {start:yyyy-MM-dd HH:mm:ss} ~ {endAt:yyyy-MM-dd HH:mm:ss}");

        var transferList = _memberTransferLogRepository.GetListByTimeRange(start, endAt);

        Console.WriteLine($"[ExportTransferError] 查到 TransferLog 筆數: {transferList.Count}");

        // 建立 FiveGameTrans DbContext（非常駐連線，直接 new）
        var options = new DbContextOptionsBuilder<FiveGameTransEntities>()
            .UseMySql(ConfigManager.ConnectionStrings.FiveGameTransConnection,
                      new MySqlServerVersion(new Version(8, 0, 32)),
                      o => o.UseMicrosoftJson())
            .Options;

        using var dbContext = new FiveGameTransEntities(options);
        var ledgerRepository = new LedgerRepository(dbContext);

        // 依 operator_id 分組，批次比對 Ledger
        var missingLogs = new List<MemberTransferLog>();
        var groups = transferList.GroupBy(x => x.OperatorId).ToList();

        Console.WriteLine($"[ExportTransferError] 共 {groups.Count} 個 operator 群組，開始逐一比對 Ledger");

        foreach (var group in groups)
        {
            var txnIds = group.Select(x => x.TxnId).ToList();
            Console.WriteLine($"[ExportTransferError] 比對群組 OperatorId={group.Key}, TxnId 數量={txnIds.Count}");

            var existingIds = ledgerRepository.GetExistingReferenceIds(group.Key, txnIds);
            Console.WriteLine($"[ExportTransferError] Ledger 中存在的 reference_id 數量={existingIds.Count}");

            var missing = group.Where(x => !existingIds.Contains(x.TxnId)).ToList();
            Console.WriteLine($"[ExportTransferError] 缺漏筆數={missing.Count}");

            missingLogs.AddRange(missing);
        }

        Console.WriteLine($"[ExportTransferError] 比對完成，共 {missingLogs.Count} 筆缺漏，開始產生 Excel");

        var operatorIds = missingLogs.Select(x => x.OperatorId).Distinct().ToList();
        var memberIds = missingLogs.Select(x => x.MemberId).Distinct().ToList();

        var operatorDict = _operatorRepository.GetListByIds(operatorIds, true).ToDictionary(x => x.Id, x => x.OperatorName);
        var memberDict = _memberRepository.GetListByIds(memberIds, true).ToDictionary(x => x.Id, x => x.Account);

        // 轉成 Excel 輸出 ViewModel
        var rows = missingLogs.Select(x => new TransferErrorExcelRow
        {
            TxnId = x.TxnId,
            OperatorId = x.OperatorId,
            OperatorName = operatorDict.ContainsKey(x.OperatorId) ? operatorDict[x.OperatorId] : string.Empty,
            MemberId = x.MemberId,
            MemberAccount = memberDict.ContainsKey(x.MemberId) ? memberDict[x.MemberId] : string.Empty,
            TransferAt = x.TransferAt.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss"),
            Type = x.Type.ToString(),
            TransferCent = x.TransferCent.ToString(),
            Status = x.Status.ToString()
        }).ToList();

        const string sheetName = "TransferError";
        var columnMappings = new List<ColumnMapping>
        {
            ColumnMapping.Create<TransferErrorExcelRow>(x => x.TxnId,        "TxnId",          NpoiDataTypeEnum.String, 30),
            ColumnMapping.Create<TransferErrorExcelRow>(x => x.OperatorId,   "OperatorId",     NpoiDataTypeEnum.String, 20),
            ColumnMapping.Create<TransferErrorExcelRow>(x => x.OperatorName, "OperatorName",   NpoiDataTypeEnum.String, 20),
            ColumnMapping.Create<TransferErrorExcelRow>(x => x.MemberId,     "MemberId",       NpoiDataTypeEnum.String, 20),
            ColumnMapping.Create<TransferErrorExcelRow>(x => x.MemberAccount, "MemberAccount", NpoiDataTypeEnum.String, 20),
            ColumnMapping.Create<TransferErrorExcelRow>(x => x.TransferAt,   "TransferAt(+8)", NpoiDataTypeEnum.String, 22),
            ColumnMapping.Create<TransferErrorExcelRow>(x => x.Type,         "Type",           NpoiDataTypeEnum.String, 12),
            ColumnMapping.Create<TransferErrorExcelRow>(x => x.TransferCent, "TransferCent",   NpoiDataTypeEnum.String, 16),
            ColumnMapping.Create<TransferErrorExcelRow>(x => x.Status,       "Status",         NpoiDataTypeEnum.String, 14),
        };

        var param = new NpoiParam<TransferErrorExcelRow>
        {
            Workbook = new XSSFWorkbook(),
            Sheets = { [sheetName] = rows.Cast<object>() },
            ColumnMappings = { [sheetName] = columnMappings }
        };

        return _npoiHelper.ExportExcel(param);
    }
}

/// <summary>
/// 會員轉帳紀錄 介面
/// </summary>
public interface IMemberTransferLogService : IServiceBase
{
    /// <summary>
    /// 匯出轉帳紀錄 儲值失敗
    /// </summary>
    MemoryStream ExportTransferError();
}