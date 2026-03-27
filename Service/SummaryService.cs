using Microsoft.Extensions.Options;
using Tool.Helper;
using Tool.Model.Entity.Mongo;
using Tool.Model.Repository.Mongo;
using Tool.ViewModel.Options;

namespace Tool.Service;

/// <summary>
/// 統計服務
/// </summary>
public class SummaryService(IDBHelper _dbBHelper,
                            IOptions<RepoOption> _options) : ISummaryService
{
    private static readonly string[] DailyAndMonthlyTimezones = ["00:00:00", "08:00:00", "-04:00:00"];

    /// <summary>
    /// 檢查分鐘統計
    /// </summary>
    public void CheckSummaryMinute()
    {
        var agentMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentWarmMongoConnection);
        var accountingRepository = new AccountingRepository(agentMongoDBContext, _options);

        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var memberRepo = new SummaryMemberGameRepository<SummaryMemberGameMinute>(mongoDBContext);
        var operatorRepo = new SummaryOperatorRepository<SummaryOperatorMinute>(mongoDBContext);

        var startAt = ConfigManager.SummarySetting.StartAt;
        var endAt = ConfigManager.SummarySetting.EndAt;

        var memberData = memberRepo.GetBetCountGroupByPeriod(startAt, endAt);
        var operatorData = operatorRepo.GetBetCountGroupByPeriod(startAt, endAt);

        RunComparison("分鐘統計", startAt, endAt, memberData, operatorData,
            t => accountingRepository.GetTotalAccountingCount(t, t.AddMinutes(1)),
            t => t.AddMinutes(1));
    }

    /// <summary>
    /// 檢查小時統計
    /// </summary>
    public void CheckSummaryHourly()
    {
        var agentMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentWarmMongoConnection);
        var accountingRepository = new AccountingRepository(agentMongoDBContext, _options);

        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var memberRepo = new SummaryMemberGameRepository<SummaryMemberGameHourly>(mongoDBContext);
        var operatorRepo = new SummaryOperatorRepository<SummaryOperatorHourly>(mongoDBContext);

        var startAt = ConfigManager.SummarySetting.StartAt;
        var endAt = ConfigManager.SummarySetting.EndAt;

        var memberData = memberRepo.GetBetCountGroupByPeriod(startAt, endAt);
        var operatorData = operatorRepo.GetBetCountGroupByPeriod(startAt, endAt);

        RunComparison("小時統計", startAt, endAt, memberData, operatorData,
            t => accountingRepository.GetTotalAccountingCount(t, t.AddHours(1)),
            t => t.AddHours(1));
    }

    /// <summary>
    /// 檢查天統計，針對 +0 / +8 / -4 三個時區各別比對
    /// </summary>
    public void CheckSummaryDaily()
    {
        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var hourlyRepo = new SummaryMemberGameRepository<SummaryMemberGameHourly>(mongoDBContext);
        var memberDailyRepo = new SummaryMemberGameRepository<SummaryMemberGameDaily>(mongoDBContext);
        var operatorDailyRepo = new SummaryOperatorRepository<SummaryOperatorDaily>(mongoDBContext);

        var startAt = ConfigManager.SummarySetting.StartAt;
        var endAt = ConfigManager.SummarySetting.EndAt;

        foreach (var tz in DailyAndMonthlyTimezones)
        {
            var memberData = memberDailyRepo.GetBetCountGroupByPeriod(startAt, endAt, tz);
            var operatorData = operatorDailyRepo.GetBetCountGroupByPeriod(startAt, endAt, tz);

            RunComparison($"日統計 [tz={tz}]", startAt, endAt, memberData, operatorData,
                t => hourlyRepo.GetTotalBetCount(t, t.AddDays(1), tz),
                t => t.AddDays(1));
        }
    }

    private static void RunComparison(
        string label,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Dictionary<DateTimeOffset, long> memberData,
        Dictionary<DateTimeOffset, long> operatorData,
        Func<DateTimeOffset, long> getSourceCount,
        Func<DateTimeOffset, DateTimeOffset> advanceTime)
    {
        Console.WriteLine($"[{label}] 開始比對時間區間: {startAt:yyyy-MM-dd HH:mm} ~ {endAt:yyyy-MM-dd HH:mm}");
        Console.WriteLine("正在查詢資料...");
        Console.WriteLine(new string('-', 110));
        Console.WriteLine($"{"時間",-14} | {"Accounting",12} | {"Member彙總",12} | {"Member差額",10} | {"Operator彙總",12} | {"Operator差額",10} | 狀態");
        Console.WriteLine(new string('-', 110));

        var mismatchCount = 0;
        var currentTime = startAt;

        while (currentTime < endAt)
        {
            var sourceCount = getSourceCount(currentTime);
            var summaryMemberCount = memberData.GetValueOrDefault(currentTime, 0);
            var summaryOperatorCount = operatorData.GetValueOrDefault(currentTime, 0);

            var memberDiff = sourceCount - summaryMemberCount;
            var operatorDiff = sourceCount - summaryOperatorCount;
            var isMatch = sourceCount == summaryMemberCount && sourceCount == summaryOperatorCount;

            if (!isMatch) mismatchCount++;

            var status = isMatch ? "一致" : "不一致";
            Console.WriteLine($"{currentTime:yyyy-MM-dd HH:mm} | {sourceCount,12} | {summaryMemberCount,14} | {memberDiff,12} | {summaryOperatorCount,14} | {operatorDiff,12} | {status}");

            currentTime = advanceTime(currentTime);
        }

        Console.WriteLine(new string('-', 110));
        Console.WriteLine($"比對完成，共有 {mismatchCount} 筆不一致");
    }

    /// <summary>
    /// 檢查月統計，針對 +0 / +8 / -4 三個時區各別比對
    /// </summary>
    public void CheckSummaryMonthly()
    {
        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var memberDailyRepo = new SummaryMemberGameRepository<SummaryMemberGameDaily>(mongoDBContext);
        var operatorDailyRepo = new SummaryOperatorRepository<SummaryOperatorDaily>(mongoDBContext);
        var memberMonthlyRepo = new SummaryMemberGameRepository<SummaryMemberGameMonthly>(mongoDBContext);
        var operatorMonthlyRepo = new SummaryOperatorRepository<SummaryOperatorMonthly>(mongoDBContext);

        var startAt = ConfigManager.SummarySetting.StartAt;
        var endAt = ConfigManager.SummarySetting.EndAt;

        Console.WriteLine($"[月統計] 開始比對時間區間: {startAt:yyyy-MM-dd HH:mm} ~ {endAt:yyyy-MM-dd HH:mm}");

        foreach (var tz in DailyAndMonthlyTimezones)
        {
            var memberMonthlyData = memberMonthlyRepo.GetBetCountGroupByPeriod(startAt, endAt, tz);
            var operatorMonthlyData = operatorMonthlyRepo.GetBetCountGroupByPeriod(startAt, endAt, tz);

            Console.WriteLine($"\n[月統計 tz={tz}]");
            Console.WriteLine(new string('-', 110));
            Console.WriteLine($"{"時間",-14} | {"Member日彙總",12} | {"Member月彙總",12} | {"Member差額",10} | {"Operator日彙總",12} | {"Operator月彙總",12} | {"Operator差額",10} | 狀態");
            Console.WriteLine(new string('-', 110));

            var mismatchCount = 0;
            var currentTime = startAt;

            while (currentTime < endAt)
            {
                var summaryMemberMonthlyCount = memberMonthlyData.GetValueOrDefault(currentTime, 0);
                var summaryOperatorMonthlyCount = operatorMonthlyData.GetValueOrDefault(currentTime, 0);

                var summaryMemberDailyCount = memberDailyRepo.GetTotalBetCount(currentTime, currentTime.AddMonths(1), tz);
                var summaryOperatorDailyCount = operatorDailyRepo.GetTotalBetCount(currentTime, currentTime.AddMonths(1), tz);

                var memberDiff = summaryMemberDailyCount - summaryMemberMonthlyCount;
                var operatorDiff = summaryOperatorDailyCount - summaryOperatorMonthlyCount;
                var isMatch = memberDiff == 0 && operatorDiff == 0;

                if (!isMatch) mismatchCount++;

                var status = isMatch ? "一致" : "不一致";
                Console.WriteLine($"{currentTime:yyyy-MM-dd HH:mm} | {summaryMemberDailyCount,14} | {summaryMemberMonthlyCount,14} | {memberDiff,12} | {summaryOperatorDailyCount,14} | {summaryOperatorMonthlyCount,14} | {operatorDiff,12} | {status}");

                currentTime = currentTime.AddDays(1);
            }

            Console.WriteLine(new string('-', 110));
            Console.WriteLine($"[月統計 tz={tz}] 比對完成，共有 {mismatchCount} 筆不一致");
        }
    }
}

/// <summary>
/// 統計服務介面
/// </summary>
public interface ISummaryService : IServiceBase
{
    void CheckSummaryMinute();
    void CheckSummaryHourly();
    void CheckSummaryDaily();
    void CheckSummaryMonthly();
}