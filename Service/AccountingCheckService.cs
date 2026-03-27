using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using Tool.Helper;
using Tool.Model.Repository.Mongo;
using Tool.ViewModel.Options;

namespace Tool.Service;

/// <summary>
///
/// </summary>
public class AccountingCheckService(IDBHelper _dbBHelper,
                                    IOptions<RepoOption> _options) : IAccountingCheckService
{
    private AccountingRepository? _accountingRepository;

    /// <summary>
    /// 檢查注單是否存在
    /// </summary>
    public void CheckData()
    {
        var agentMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentWarmMongoConnection);
        _accountingRepository = new AccountingRepository(agentMongoDBContext, _options);

        // 取得桌面路徑
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var filePath = Path.Combine(desktopPath, "5 Gaming verify 1.xlsx");

        // 嘗試不同的副檔名
        if (!File.Exists(filePath))
        {
            filePath = Path.Combine(desktopPath, "5 Gaming verify 1.xls");
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"找不到檔案: 5 Gaming verify 1.xlsx 或 5 Gaming verify 1.xls");
            Console.WriteLine($"請確認檔案位於桌面: {desktopPath}");
            return;
        }

        Console.WriteLine($"正在讀取檔案: {filePath}");

        try
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1); // 取得第一個工作表

            // 取得 B 欄從第 2 行開始的資料，並記錄行號對應
            var rowIdMapping = new Dictionary<int, string>(); // row -> id
            var idRowsMapping = new Dictionary<string, List<int>>(); // id -> rows (用於檢測重複)
            var ids = new List<string>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            Console.WriteLine($"資料列數: {lastRow - 1} (從第2行到第{lastRow}行)");

            for (int row = 2; row <= lastRow; row++)
            {
                var cellValue = worksheet.Cell(row, 2).GetValue<string>(); // B 欄 = 第 2 欄
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    var id = cellValue.Trim();
                    rowIdMapping[row] = id;
                    ids.Add(id);

                    // 記錄每個 ID 出現的行號
                    if (!idRowsMapping.ContainsKey(id))
                        idRowsMapping[id] = new List<int>();
                    idRowsMapping[id].Add(row);
                }
            }

            // 檢查並列出重複的 ID
            var duplicateIds = idRowsMapping.Where(x => x.Value.Count > 1).ToList();
            if (duplicateIds.Count > 0)
            {
                Console.WriteLine($"\n========== 重複的 ID ({duplicateIds.Count} 個) ==========");
                foreach (var dup in duplicateIds)
                {
                    Console.WriteLine($"ID: {dup.Key} (出現 {dup.Value.Count} 次, 行號: {string.Join(", ", dup.Value)})");
                }
                Console.WriteLine();
            }

            if (ids.Count == 0)
            {
                Console.WriteLine("B 欄沒有找到任何資料");
                return;
            }

            Console.WriteLine($"共讀取 {ids.Count} 筆 ID，開始分批檢查 (每批 5000 筆)...\n");

            // 批次檢查 ID 是否存在，並顯示進度
            var checkResult = _accountingRepository.CheckExistsByIds(ids, 5000, (processed, total) =>
            {
                var percentage = (double)processed / total * 100;
                Console.Write($"\r進度: {processed:N0} / {total:N0} ({percentage:F1}%)    ");
            });

            Console.WriteLine("\n\n正在寫入結果到 F 欄...");

            // 將結果寫入 F 欄
            foreach (var (row, id) in rowIdMapping)
            {
                var exists = checkResult.TryGetValue(id, out var value) && value;
                worksheet.Cell(row, 6).Value = exists ? id : "不存在"; // F 欄 = 第 6 欄
            }

            // 儲存檔案
            workbook.Save();
            Console.WriteLine($"已儲存結果到: {filePath}");

            // 統計結果 (基於實際行數，而非去重後的 ID)
            var existCount = 0;
            var notExistCount = 0;
            foreach (var (row, id) in rowIdMapping)
            {
                if (checkResult.TryGetValue(id, out var exists) && exists)
                    existCount++;
                else
                    notExistCount++;
            }

            Console.WriteLine("\n========== 檢查結果 ==========");
            Console.WriteLine($"總共檢查: {rowIdMapping.Count} 筆");
            Console.WriteLine($"存在: {existCount} 筆");
            Console.WriteLine($"不存在: {notExistCount} 筆");
            Console.WriteLine($"不重複 ID 數: {checkResult.Count} 筆");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"處理 Excel 檔案時發生錯誤: {ex.Message}");
        }
    }
}

/// <summary>
///
/// </summary>
public interface IAccountingCheckService : IServiceBase
{
    /// <summary>
    /// 檢查注單是否存在
    /// </summary>
    void CheckData();
}
