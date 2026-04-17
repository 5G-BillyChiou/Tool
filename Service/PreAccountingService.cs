using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Tool.Enum;
using Tool.Model.Entity.FiveGameTrans;
using Tool.Model.Repository.FiveGameTrans;

namespace Tool.Service;

public class PreAccountingService : IPreAccountingService
{
    /// <summary>
    /// 產生入帳 SQL
    /// </summary>
    public void CreateAccountingInsertSql()
    {
        var dataList = ReadIdsFromExcel();
        if (dataList.Count == 0)
            return;

        var options = new DbContextOptionsBuilder<FiveGameTransEntities>()
            .UseMySql(ConfigManager.ConnectionStrings.FiveGameTransConnection, new MySqlServerVersion(new Version(8, 0, 32)), o => o.UseMicrosoftJson())
            .Options;

        using var dbContext = new FiveGameTransEntities(options);
        var accountingRepository = new AccountingRepository(dbContext);
        var preAccountingResultRepository = new PreAccountingResultRepository(dbContext);
        var memberWalletRepository = new MemberWalletRepository(dbContext);
        
        //---------------------------------------//
        //      獨立錢包 全轉 acc 且有餘額要入帳     //
        //   共用錢包 只轉沒有贏的 且 M 欄沒有 O 的   //
        //---------------------------------------//

        // 比對 Excel 匯入的 ID 是否存在於 pre_accounting_result
        var allImportedIds         = dataList.Select(x => x.Id).ToList();
        var existingPreAccIds      = preAccountingResultRepository.GetExistingIds(allImportedIds);
        var notFoundInPreAccIds    = allImportedIds.Except(existingPreAccIds).ToList();
        if (notFoundInPreAccIds.Count > 0)
        {
            Console.WriteLine($"⚠ 以下 {notFoundInPreAccIds.Count} 筆 id 在 pre_accounting_result 中不存在：");
            notFoundInPreAccIds.ForEach(id => Console.WriteLine($"  - {id}"));
        }

        // 獨立
        var singleWalletIds = dataList.Where(x => x.WalletType == "獨立").Select(x => x.Id).ToList();
        var singleWalletPreAccountingList = preAccountingResultRepository.GetListByIds(singleWalletIds).ToList();
        var singleWalletPreAccountingEndList = preAccountingResultRepository.GetListByIds(singleWalletIds)
                                                                         .Where(x => x.GameEnd == AccountingGameEndEnum.AccountingGameEnd_End)
                                                                         .ToList();
        var singleWalletPreAccountingNotEndList = preAccountingResultRepository.GetListByIds(singleWalletIds)
                                                                         .Where(x => x.GameEnd == AccountingGameEndEnum.AccountingGameEnd_NotEnd)
                                                                         .ToList();
        Console.WriteLine($"獨立錢包查詢到 {singleWalletPreAccountingList.Count} 筆 ({singleWalletPreAccountingEndList.Count} 筆 GameEnd == true，{singleWalletPreAccountingNotEndList.Count} 筆 GameEnd == false)");

        // 共用
        var sharedWalletIds = dataList.Where(x => x.WalletType == "共用").Where(x => x.ProcessType != "O").Select(x => x.Id).ToList();
        var sharedWalletPreAccountingEndList = preAccountingResultRepository.GetListByIds(sharedWalletIds)
                                                                         .Where(x => x.TotalWin == 0)
                                                                         .Where(x => x.GameEnd == AccountingGameEndEnum.AccountingGameEnd_End)
                                                                         .ToList();
        Console.WriteLine($"共用錢包查詢到 {sharedWalletPreAccountingEndList.Count} 筆 GameEnd == true and TotalWin == 0 的");

        // 總計
        var totalCount = singleWalletPreAccountingEndList.Count + sharedWalletPreAccountingEndList.Count;
        Console.WriteLine($"查詢到 {totalCount} 筆（Excel 共 {dataList.Count} 筆）");

        //----------------------------------------------------------------------------------------------------//

        // 建立 Id → ExcelData 對應表（大小寫不分，防止 DB 與 Excel 大小寫不一致）
        var dataById = dataList.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        // 回填用的 Id 集合（用 Excel 原始 ID，避免 DB 回傳值與 Excel 值大小寫不同導致比對失敗）
        // 先宣告空集合，確認實際 insert / wallet 後再填入
        var accSqlIds    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var walletSqlIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        //----------------------------------------------------------------------------------------------------//

        // 產生 accounting INSERT SQL（獨立 + 共用）
        var allCandidates   = singleWalletPreAccountingEndList.Concat(sharedWalletPreAccountingEndList).ToList();
        var candidateAccIds = allCandidates.Select(x => x.Id).ToList();

        // 檢查 accounting 是否已存在，避免重複 INSERT
        var existingAccIds  = accountingRepository.GetExistingIds(candidateAccIds);

        var duplicateAccIds = existingAccIds.Intersect(candidateAccIds).ToList();
        if (duplicateAccIds.Count > 0)
        {
            Console.WriteLine($"⚠ 以下 {duplicateAccIds.Count} 筆 id 在 accounting 中已存在，已略過：");
            duplicateAccIds.ForEach(id => Console.WriteLine($"  - {id}"));
        }

        var insertList = allCandidates.Where(x => !existingAccIds.Contains(x.Id)).ToList();

        // 以 DB ID 反查 Excel 原始 ID，再加入 accSqlIds（確保回填時用的是 Excel 字串）
        var insertDbIds = insertList.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var excelId in allImportedIds.Where(id => insertDbIds.Contains(id)))
            accSqlIds.Add(excelId);

        var sqlLines  = insertList.Select(ToInsertSql).ToList();
        var sqlOutputPath = Path.Combine(desktopPath, $"accounting_insert_{timestamp}.sql");
        File.WriteAllLines(sqlOutputPath, sqlLines);
        Console.WriteLine($"已產生 {sqlLines.Count} 筆 INSERT SQL，輸出至: {sqlOutputPath}");

        //----------------------------------------------------------------------------------------------------//

        // 產生獨立錢包有贏分的 member_wallet UPDATE SQL（只處理 TotalWin > 0）
        var walletCandidates = singleWalletPreAccountingEndList.Where(x => x.TotalWin > 0).ToList();

        // 檢查 member_wallet 是否存在，避免 UPDATE 找不到資料
        var candidateMemberIds = walletCandidates.Select(x => x.MemberId).Distinct().ToList();
        var existingMemberIds  = memberWalletRepository.GetExistingMemberIds(candidateMemberIds);

        var missingMemberIds = candidateMemberIds.Except(existingMemberIds).ToList();
        if (missingMemberIds.Count > 0)
        {
            Console.WriteLine($"⚠ 以下 {missingMemberIds.Count} 個 member_id 在 member_wallet 中不存在，已略過：");
            missingMemberIds.ForEach(id => Console.WriteLine($"  - {id}"));
        }

        var walletUpdateList = walletCandidates.Where(x => existingMemberIds.Contains(x.MemberId)).ToList();

        // 同樣以 DB ID 反查 Excel 原始 ID
        var walletDbIds = walletUpdateList.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var excelId in allImportedIds.Where(id => walletDbIds.Contains(id)))
            walletSqlIds.Add(excelId);
        var walletSqlLines   = walletUpdateList.Select(ToWalletUpdateSql).ToList();
        var walletOutputPath = Path.Combine(desktopPath, $"member_wallet_update_{timestamp}.sql");
        File.WriteAllLines(walletOutputPath, walletSqlLines);
        Console.WriteLine($"已產生 {walletSqlLines.Count} 筆 member_wallet UPDATE SQL，輸出至: {walletOutputPath}");

        //----------------------------------------------------------------------------------------------------//

        // 獨立錢包 GameEnd == NotEnd → 退還 Bet 到 member_wallet，並刪除 pre_accounting_result
        var notEndMemberIds        = singleWalletPreAccountingNotEndList.Select(x => x.MemberId).Distinct().ToList();
        var existingNotEndMemberIds = memberWalletRepository.GetExistingMemberIds(notEndMemberIds);

        var missingNotEndMemberIds = notEndMemberIds.Except(existingNotEndMemberIds).ToList();
        if (missingNotEndMemberIds.Count > 0)
        {
            Console.WriteLine($"⚠ [NotEnd] 以下 {missingNotEndMemberIds.Count} 個 member_id 在 member_wallet 中不存在，已略過退款：");
            missingNotEndMemberIds.ForEach(id => Console.WriteLine($"  - {id}"));
        }

        var betRefundList = singleWalletPreAccountingNotEndList
            .Where(x => existingNotEndMemberIds.Contains(x.MemberId))
            .ToList();

        // 退款 ID 也回填到 walletSqlIds（O 欄標 Y）
        var betRefundDbIds = betRefundList.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var excelId in allImportedIds.Where(id => betRefundDbIds.Contains(id)))
            walletSqlIds.Add(excelId);

        var betRefundLines  = betRefundList.Select(ToBetRefundSql).ToList();
        var betRefundOutputPath = Path.Combine(desktopPath, $"member_wallet_bet_refund_{timestamp}.sql");
        File.WriteAllLines(betRefundOutputPath, betRefundLines);
        Console.WriteLine($"已產生 {betRefundLines.Count} 筆 Bet Refund SQL，輸出至: {betRefundOutputPath}");

        //----------------------------------------------------------------------------------------------------//

        // 產生 pre_accounting_result DELETE SQL
        // ・GameEnd == End   → 已轉入 accounting 的那批（insertList）
        // ・GameEnd == NotEnd → 退款的那批（singleWalletPreAccountingNotEndList 全部，不限 member 存在）
        var deleteLines = insertList
            .Select(r => $"DELETE FROM `pre_accounting_result` WHERE `id` = '{Escape(r.Id)}'; -- GameEnd")
            .Concat(singleWalletPreAccountingNotEndList
                .Select(r => $"DELETE FROM `pre_accounting_result` WHERE `id` = '{Escape(r.Id)}'; -- NotEnd"))
            .ToList();
        var deleteOutputPath = Path.Combine(desktopPath, $"pre_accounting_delete_{timestamp}.sql");
        File.WriteAllLines(deleteOutputPath, deleteLines);
        Console.WriteLine($"已產生 {deleteLines.Count} 筆 DELETE SQL（{insertList.Count} GameEnd + {singleWalletPreAccountingNotEndList.Count} NotEnd），輸出至: {deleteOutputPath}");

        // 回填 Excel N、O 欄
        WriteBackToExcel(dataById, accSqlIds, walletSqlIds);
        Console.WriteLine("已回填 Excel N、O 欄位");
    }

    /// <summary>
    /// 將 PreAccountingResult 轉換為 accounting INSERT SQL
    /// <para>begin_at / finished_at 無對應欄位，以 created_at 代填</para>
    /// <para>bonus_win / test_account / offline 無對應欄位，給預設值 0 / false / false</para>
    /// </summary>
    private static string ToInsertSql(PreAccountingResult r)
    {
        var createdAt   = r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var gameModule  = r.GameModule  != null ? $"'{Escape(r.GameModule)}'"  : "NULL";
        var gameData    = r.GameData    != null ? $"'{Escape(r.GameData)}'"    : "NULL";
        var gameResult  = r.GameResult  != null ? $"'{Escape(r.GameResult)}'"  : "NULL";
        var campaignId  = r.CampaignId  != null ? $"'{Escape(r.CampaignId)}'"  : "NULL";
        var replayData  = r.ReplayData  != null ? $"0x{Convert.ToHexString(r.ReplayData)}" : "NULL";

        return
            "INSERT INTO `accounting` " +
            "(`id`,`created_at`,`begin_at`,`finished_at`, `agent_id`,`agent_path`,`operator_id`,`currency_sn`," +
            "`game_id`,`member_id`,`member_account`, `init_cent`,`actual_bet`,`denom`,`bet`,`total_win`,`bonus_win`," +
            "`game_module`,`bonus`,`campaign_type`,`campaign_id`, `end_cent`,`game_end`,`test_account`," +
            "`game_data`,`game_result`,`replay_data`,`offline`, `bet_type_mask`,`status_mask`) " +
            "VALUES (" +
            $"'{Escape(r.Id)}','{createdAt}','{createdAt}','{createdAt}', {r.AgentId},'{Escape(r.AgentPath)}','{Escape(r.OperatorId)}',{r.CurrencySn}," +
            $"'{Escape(r.GameId)}','{Escape(r.MemberId)}','{Escape(r.MemberAccount)}', {r.InitCent}, {r.ActualBet},{r.Denom},{r.Bet},{r.TotalWin}, 0," +
            $"{gameModule},{(r.Bonus ? 1 : 0)},{(int)r.CampaignType},{campaignId}, {r.EndCent},{(int)r.GameEnd}, 0," +
            $"{gameData},{gameResult},{replayData},0, {r.BetTypeMask},{r.StatusMask});";
    }

    /// <summary>
    /// 獨立錢包 GameEnd == End，有贏分：將 TotalWin 加入 member_wallet
    /// </summary>
    private static string ToWalletUpdateSql(PreAccountingResult r)
        => $"UPDATE `member_wallet` SET `balance` = `balance` + {r.TotalWin}, `updated_at` = UTC_TIMESTAMP() WHERE `member_id` = '{Escape(r.MemberId)}';";

    /// <summary>
    /// 獨立錢包 GameEnd == NotEnd：退還 Bet 到 member_wallet
    /// </summary>
    private static string ToBetRefundSql(PreAccountingResult r)
        => $"UPDATE `member_wallet` SET `balance` = `balance` + {r.Bet}, `updated_at` = UTC_TIMESTAMP() WHERE `member_id` = '{Escape(r.MemberId)}';";

    /// <summary>
    /// 回填 Excel N 欄（產acc SQL）、O 欄（update wallet SQL ）
    /// </summary>
    private static void WriteBackToExcel(
        Dictionary<string, ExcelData> dataById,
        HashSet<string> accSqlIds,
        HashSet<string> walletSqlIds)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var filePath = Path.Combine(desktopPath, "未完成注單0415.xlsx");

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);

        // 確保標題列有欄位名稱
        worksheet.Cell(1, 14).Value = "產acc SQL";      // N 欄
        worksheet.Cell(1, 15).Value = "update wallet sql"; // O 欄

        foreach (var (id, data) in dataById)
        {
            worksheet.Cell(data.RowNumber, 14).Value = accSqlIds.Contains(id)    ? "Y" : "";
            worksheet.Cell(data.RowNumber, 15).Value = walletSqlIds.Contains(id) ? "Y" : "";
        }

        workbook.Save();
    }

    private static string Escape(string? value)
        => (value ?? "").Replace("\\", "\\\\").Replace("'", "\\'"  );

    /// <summary>
    /// 從桌面的 未完成注單.xlsx 讀取單號列表（B欄，跳過第一列標題）
    /// </summary>
    private static List<ExcelData> ReadIdsFromExcel()
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var filePath = Path.Combine(desktopPath, "未完成注單0415.xlsx");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"找不到檔案: {filePath}");
            return new List<ExcelData>();
        }

        Console.WriteLine($"正在讀取: {filePath}");

        var dataList = new List<ExcelData>();

        try
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var id = worksheet.Cell(row, 2).GetValue<string>().Trim();           // B 欄 = 單號
                var walletType = worksheet.Cell(row, 10).GetValue<string>().Trim();  // J 欄 = 錢包類型
                var processType = worksheet.Cell(row, 12).GetValue<string>().Trim(); // L 欄 = 處理類型

                if (!string.IsNullOrWhiteSpace(id))
                    dataList.Add(new ExcelData { RowNumber = row, Id = id, WalletType = walletType, ProcessType = processType });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"讀取 Excel 時發生錯誤: {ex.Message}");
            return [];
        }

        Console.WriteLine($"讀取到 {dataList.Count} 筆單號");
        return dataList;
    }

    
}

public class ExcelData
{
    public int    RowNumber   { get; set; }
    public string Id          { get; set; }
    public string WalletType  { get; set; }
    public string ProcessType { get; set; }
}

public interface IPreAccountingService
{
    /// <summary>
    /// 產生入帳 SQL
    /// </summary>
    void CreateAccountingInsertSql();
}
