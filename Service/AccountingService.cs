using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Tool.Helper;
using Tool.Model.Repository.FiveGame;
using Tool.Model.Repository.Mongo;
using Tool.ViewModel;
using Tool.ViewModel.Options;

namespace Tool.Service;

public class AccountingService( IDBHelper _dbBHelper,
                                IOptions<RepoOption> _options,
                                IOperatorRepository _operatorRepository) : IAccountingService
{
    /// <summary>
    /// 計算 bonus 碼量
    /// </summary>
    public void SummaryBonusData()
    {
        var totalStopwatch = new StopwatchHelper();
        totalStopwatch.BeginTiming();

        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var exchangeRateRepository = new ExchangeRateRepository(mongoDBContext);
        var operatorList = _operatorRepository.GetList()
                                              .Where(x => x.FirstBetAt != null)
                                              .ToList();

        // 匯率查詢優化：轉成 Dictionary 加速查詢 O(n) -> O(1)
        var rateDataList = exchangeRateRepository.GetRateListByCurrency("USD");
        var rateDict = rateDataList?.ToDictionary(x => x.CurrencySn, x => x.Rate)
                       ?? [];

        var startAt = new DateTimeOffset(2026, 1, 27, 16, 0, 0, TimeSpan.Zero);
        var endAt = new DateTimeOffset(2026, 2, 10, 10, 0, 0, TimeSpan.Zero);

        const string targetGameId = "S5G-H5-99935";
        const int maxConcurrency = 10; // 併發數量，可依 DB 負載調整

        // 建立輸出檔案
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outputPath = Path.Combine("Logs", $"SummaryBonusData_{timestamp}.txt");
        Directory.CreateDirectory("Logs");

        // 使用 ConcurrentDictionary 儲存併發結果
        var operatorResults = new ConcurrentDictionary<string, (decimal Bet0Sum, decimal Bet1Sum, int Count0, int Count1)>();

        var totalHours = (int)Math.Ceiling((endAt - startAt).TotalHours);
        var totalOperators = operatorList.Count;
        var processedOperators = 0;

        Console.WriteLine($"開始統計 Bonus 資料...");
        Console.WriteLine($"營運商數量: {totalOperators}, 時間範圍: {totalHours} 小時, 併發數: {maxConcurrency}");

        // 併發處理營運商 - 每個執行緒建立自己的 Repository 避免競爭
        Parallel.ForEach(
            operatorList,
            new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency },
            () => new AccountingRepository(_dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentWarmMongoConnection), _options),
            (op, state, localRepo) =>
            {
                var stopwatch = new StopwatchHelper();
                stopwatch.BeginTiming();

                decimal totalBet0 = 0, totalBet1 = 0;
                int count0 = 0, count1 = 0;

                // 切成每小時查詢，避免一次取太多資料 OOM
                var currentStart = startAt;
                while (currentStart < endAt)
                {
                    var currentEnd = currentStart.AddHours(1);
                    if (currentEnd > endAt)
                        currentEnd = endAt;

                    // 只取需要的欄位
                    var accountings = localRepo.GetBonusListProjected(currentStart, currentEnd, op.Id, targetGameId);

                    foreach (var (_, _, CurrencySn, Bet, GameData) in accountings)
                    {
                        // 使用 Dictionary 快速查詢匯率
                        var rate = rateDict.GetValueOrDefault(CurrencySn, 1);
                        var betAmount = Bet * rate;

                        var gameData = GameData ?? string.Empty;
                        var atIndex = gameData.IndexOf('@');
                        if (atIndex > 0)
                        {
                            var charBeforeAt = gameData[atIndex - 1];
                            if (charBeforeAt == '0')
                            {
                                totalBet0 += betAmount;
                                count0++;
                            }
                            else if (charBeforeAt == '1')
                            {
                                totalBet1 += betAmount;
                                count1++;
                            }
                        }
                    }

                    currentStart = currentEnd;
                }

                // 只儲存有資料的營運商
                if (count0 > 0 || count1 > 0)
                {
                    operatorResults[op.Id] = (totalBet0, totalBet1, count0, count1);
                }

                var current = Interlocked.Increment(ref processedOperators);
                var progress = (double)current / totalOperators * 100;
                var elapsed = stopwatch.TotalSeconds;
                Console.WriteLine($"營運商 [{current}/{totalOperators}] {op.Id} ({op.OperatorName}) 完成 ({progress:F1}%) - [0]:{count0} [1]:{count1} - 耗時: {elapsed:F2}s");

                return localRepo;
            },
            _ => { });

        // 寫入檔案（依營運商 ID 排序）
        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

        writer.WriteLine($"開始統計 Bonus 資料");
        writer.WriteLine($"時間範圍: {startAt:yyyy-MM-dd HH:mm} ~ {endAt:yyyy-MM-dd HH:mm}");
        writer.WriteLine($"目標遊戲: {targetGameId}");
        writer.WriteLine($"匯率轉換: USD");
        writer.WriteLine(new string('=', 80));

        foreach (var (opId, result) in operatorResults.OrderBy(x => x.Key))
        {
            writer.WriteLine($"營運商: {opId}");
            writer.WriteLine($"  [0] Bet 總和 (USD): {result.Bet0Sum:N2}, 筆數: {result.Count0}");
            writer.WriteLine($"  [1] Bet 總和 (USD): {result.Bet1Sum:N2}, 筆數: {result.Count1}");
            writer.WriteLine();
        }

        // 輸出總計
        writer.WriteLine(new string('=', 80));
        writer.WriteLine("總計:");
        var grandTotal0 = operatorResults.Values.Sum(x => x.Bet0Sum);
        var grandTotal1 = operatorResults.Values.Sum(x => x.Bet1Sum);
        var grandCount0 = operatorResults.Values.Sum(x => x.Count0);
        var grandCount1 = operatorResults.Values.Sum(x => x.Count1);
        writer.WriteLine($"  [0] Bet 總和 (USD): {grandTotal0:N2}, 筆數: {grandCount0}");
        writer.WriteLine($"  [1] Bet 總和 (USD): {grandTotal1:N2}, 筆數: {grandCount1}");
        writer.WriteLine($"統計完成，共 {operatorResults.Count} 個營運商有資料");

        totalStopwatch.EndTiming($"全部完成", true);
        Console.WriteLine($"統計完成，結果已輸出至: {outputPath}");
    }

    /// <summary>
    /// 讀取 SummaryBonusData 產出的 txt 檔，將營運商 ID 替換成營運商名稱
    /// </summary>
    public void ConvertOperatorIdToName(string inputFilePath)
    {
        if (!File.Exists(inputFilePath))
        {
            Console.WriteLine($"檔案不存在: {inputFilePath}");
            return;
        }

        // 建立營運商 ID -> Name 的對照表
        var operatorDict = _operatorRepository.GetList()
            .ToDictionary(x => x.Id, x => x.OperatorName);

        var content = File.ReadAllText(inputFilePath, System.Text.Encoding.UTF8);

        // 替換 "營運商: {id}" 為 "營運商: {name}"
        foreach (var (id, name) in operatorDict)
        {
            content = content.Replace($"營運商: {id}", $"營運商: {name} ({id})");
        }

        // 輸出到新檔案
        var outputPath = inputFilePath.Replace(".txt", "_WithName.txt");
        File.WriteAllText(outputPath, content, System.Text.Encoding.UTF8);

        Console.WriteLine($"轉換完成，結果已輸出至: {outputPath}");
    }

    /// <summary>
    /// 讀取 SummaryBonusData 產出的 txt 檔，將金額除以 100
    /// </summary>
    public void ConvertAmountDivide100(string inputFilePath)
    {
        if (!File.Exists(inputFilePath))
        {
            Console.WriteLine($"檔案不存在: {inputFilePath}");
            return;
        }

        var lines = File.ReadAllLines(inputFilePath, System.Text.Encoding.UTF8);
        var outputLines = new List<string>();

        // 匹配金額的正則：Bet 總和 (USD): 1,234,567.89
        var regex = new System.Text.RegularExpressions.Regex(@"Bet 總和 \(USD\): ([\d,]+\.?\d*)");

        foreach (var line in lines)
        {
            var newLine = regex.Replace(line, match =>
            {
                var amountStr = match.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(amountStr, out var amount))
                {
                    var newAmount = amount / 100;
                    return $"Bet 總和 (USD): {newAmount:N2}";
                }
                return match.Value;
            });
            outputLines.Add(newLine);
        }

        // 輸出到新檔案
        var outputPath = inputFilePath.Replace(".txt", "_Divided100.txt");
        File.WriteAllLines(outputPath, outputLines, System.Text.Encoding.UTF8);

        Console.WriteLine($"轉換完成，結果已輸出至: {outputPath}");
    }
}


public interface IAccountingService : IServiceBase
{
    /// <summary>
    /// 計算 bonus 碼量
    /// </summary>
    void SummaryBonusData();

    /// <summary>
    /// 讀取 SummaryBonusData 產出的 txt 檔，將營運商 ID 替換成營運商名稱
    /// </summary>
    void ConvertOperatorIdToName(string inputFilePath);

    /// <summary>
    /// 讀取 SummaryBonusData 產出的 txt 檔，將金額除以 100
    /// </summary>
    void ConvertAmountDivide100(string inputFilePath);
}
