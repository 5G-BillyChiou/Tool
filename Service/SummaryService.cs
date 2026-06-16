using Microsoft.EntityFrameworkCore;
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
    //private static readonly string[] DailyAndMonthlyTimezones = ["00:00:00", "08:00:00", "-04:00:00"];
    private static readonly string[] DailyAndMonthlyTimezones = ["08:00:00"];

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
    /// 檢查天統計：依設定的各時區，以「日彙總」為主，將其與對應期間的「小時彙總加總」比對（會員、營運商各自比對）。
    /// <para>StartAt / EndAt 為 +0 時間，取其牆鐘值後，依各時區重新對齊到當地午夜。</para>
    /// </summary>
    public void CheckSummaryDaily()
    {
        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var memberHourlyRepo = new SummaryMemberGameRepository<SummaryMemberGameHourly>(mongoDBContext);
        var operatorHourlyRepo = new SummaryOperatorRepository<SummaryOperatorHourly>(mongoDBContext);
        var memberDailyRepo = new SummaryMemberGameRepository<SummaryMemberGameDaily>(mongoDBContext);
        var operatorDailyRepo = new SummaryOperatorRepository<SummaryOperatorDaily>(mongoDBContext);

        var startAt = ConfigManager.SummarySetting.StartAt;
        var endAt = ConfigManager.SummarySetting.EndAt;

        foreach (var tz in DailyAndMonthlyTimezones)
        {
            // 日彙總依時區分桶，需帶 tz 過濾，否則會撈不到非 +0 的資料
            var memberDailyData = memberDailyRepo.GetBetCountGroupByPeriod(startAt, endAt, tz);
            var operatorDailyData = operatorDailyRepo.GetBetCountGroupByPeriod(startAt, endAt, tz);

            // 小時彙總只有 +0 資料，依當地日界線換算成 UTC 區間後加總
            // 會員：小時彙總加總 vs 日彙總
            RunSumComparison($"日統計 Member [tz={tz}]", startAt, endAt,
                (s, e) => memberHourlyRepo.GetTotalBetCount(s.ToUniversalTime(), e.ToUniversalTime()),
                memberDailyData,
                t => t.AddDays(1));

            // 營運商：小時彙總加總 vs 日彙總
            RunSumComparison($"日統計 Operator [tz={tz}]", startAt, endAt,
                (s, e) => operatorHourlyRepo.GetTotalBetCount(s.ToUniversalTime(), e.ToUniversalTime()),
                operatorDailyData,
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
        Console.WriteLine($"[{label}] 開始比對時間區間: {startAt.ToUniversalTime():yyyy-MM-dd HH:mm} ~ {endAt.ToUniversalTime():yyyy-MM-dd HH:mm} (+0)");
        Console.WriteLine("正在查詢資料...");
        Console.WriteLine(new string('-', 110));
        Console.WriteLine($"{"時間",-14} | {"Accounting",12} | {"Member彙總",12} | {"Member差額",12} | {"Operator彙總",12} | {"Operator差額",12} | 狀態");
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
            Console.WriteLine($"{currentTime.ToUniversalTime():yyyy-MM-dd HH:mm} | {sourceCount,12} | {summaryMemberCount,12} | {memberDiff,12} | {summaryOperatorCount,12} | {operatorDiff,12} | {status}");

            currentTime = advanceTime(currentTime);
        }

        Console.WriteLine(new string('-', 110));
        Console.WriteLine($"比對完成，共有 {mismatchCount} 筆不一致");
    }

    /// <summary>
    /// 檢查月統計：依設定的各時區，將「月彙總」與對應期間的「日彙總加總」比對（會員、營運商各自比對）。
    /// <para>StartAt / EndAt 為 +0 時間，取其牆鐘值後，依各時區重新對齊到當地月初。</para>
    /// </summary>
    public void CheckSummaryMonthly()
    {
        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var memberDailyRepo = new SummaryMemberGameRepository<SummaryMemberGameDaily>(mongoDBContext);
        var operatorDailyRepo = new SummaryOperatorRepository<SummaryOperatorDaily>(mongoDBContext);
        var memberMonthlyRepo = new SummaryMemberGameRepository<SummaryMemberGameMonthly>(mongoDBContext);
        var operatorMonthlyRepo = new SummaryOperatorRepository<SummaryOperatorMonthly>(mongoDBContext);

        var startAt = ConfigManager.SummarySetting.StartAt.DateTime;
        var endAt = ConfigManager.SummarySetting.EndAt.DateTime;

        foreach (var tz in DailyAndMonthlyTimezones)
        {
            // 月彙總依時區分桶，需帶 tz 過濾
            var memberMonthlyData = memberMonthlyRepo.GetBetCountGroupByPeriod(startAt, endAt, tz);
            var operatorMonthlyData = operatorMonthlyRepo.GetBetCountGroupByPeriod(startAt, endAt, tz);

            // 會員：日彙總加總 vs 月彙總
            RunSumComparison($"月統計 Member [tz={tz}]", startAt, endAt,
                (s, e) => memberDailyRepo.GetTotalBetCount(s, e, tz),
                memberMonthlyData,
                t => t.AddMonths(1));

            // 營運商：日彙總加總 vs 月彙總
            RunSumComparison($"月統計 Operator [tz={tz}]", startAt, endAt,
                (s, e) => operatorDailyRepo.GetTotalBetCount(s, e, tz),
                operatorMonthlyData,
                t => t.AddMonths(1));
        }
    }

    /// <summary>
    /// 比對「下層彙總加總」與「上層彙總」（例如：日彙總加總 vs 月彙總）。
    /// </summary>
    /// <param name="getLowerSum">取得指定期間 [start, end) 內下層彙總的加總</param>
    /// <param name="upperSummaryByPeriod">上層彙總，依 PeriodStartAt 分組</param>
    /// <param name="advanceTime">推進到下一個比對期間（例如 +1 個月）</param>
    private static void RunSumComparison(
        string label,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Func<DateTimeOffset, DateTimeOffset, long> getLowerSum,
        Dictionary<DateTimeOffset, long> upperSummaryByPeriod,
        Func<DateTimeOffset, DateTimeOffset> advanceTime)
    {
        Console.WriteLine($"\n[{label}] 開始比對時間區間: {startAt.ToUniversalTime():yyyy-MM-dd HH:mm} ~ {endAt.ToUniversalTime():yyyy-MM-dd HH:mm} (+0)");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"{"時間",-14} | {"下層加總",10} | {"上層彙總",10} | {"差額",10} | 狀態");
        Console.WriteLine(new string('-', 80));

        var mismatchCount = 0;
        var currentTime = startAt;

        while (currentTime < endAt)
        {
            var nextTime = advanceTime(currentTime);
            var lowerSum = getLowerSum(currentTime, nextTime);
            var upperCount = upperSummaryByPeriod.GetValueOrDefault(currentTime, 0);

            var diff = lowerSum - upperCount;
            var isMatch = diff == 0;
            if (!isMatch) mismatchCount++;

            var status = isMatch ? "一致" : "不一致";
            Console.WriteLine($"{currentTime.ToUniversalTime():yyyy-MM-dd HH:mm} | {lowerSum,14} | {upperCount,14} | {diff,14} | {status}");

            currentTime = nextTime;
        }

        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"[{label}] 比對完成，共有 {mismatchCount} 筆不一致");
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