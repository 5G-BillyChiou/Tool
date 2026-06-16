using Tool.Helper;
using Tool.Model.Entity.Mongo;
using Tool.Model.Repository.FiveGame;
using Tool.Model.Repository.Mongo;

namespace Tool.Service;

/// <summary>
/// 新增會員統計比對服務：驗證 Hourly / Daily / Monthly 彙總數量是否與來源一致
/// </summary>
public class SummaryNewMemberCheckService(IDBHelper _dbHelper,
                                           IMemberRepository _memberRepository) : ISummaryNewMemberCheckService
{
    private static readonly string[] DailyAndMonthlyTimezones = ["00:00:00", "08:00:00", "-04:00:00"];

    /// <summary>
    /// 比對小時統計 vs 來源 (+0 時區)
    /// </summary>
    public void CheckHourly()
    {
        var mongoDBContext = _dbHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var hourlyRepo = new SummaryOperatorNewMemberRepository<SummaryOperatorNewMemberHourly>(mongoDBContext);

        var startAt = ConfigManager.SummarySetting.StartAt;
        var endAt   = ConfigManager.SummarySetting.EndAt;

        var summaryByPeriod = hourlyRepo.GetListByTimeRange(startAt, endAt)
            .GroupBy(r => r.PeriodStartAt)
            .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.OperatorId, r => r.NewMemberCount));

        RunComparison("[小時統計 新增會員]", startAt, endAt, summaryByPeriod,
            _memberRepository.GetNewMemberCountDict,
            t => t.AddHours(1));
    }

    /// <summary>
    /// 比對日統計 vs 來源，針對 +0 / +8 / -4 三個時區各別比對
    /// </summary>
    public void CheckDaily()
    {
        var mongoDBContext = _dbHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var dailyRepo = new SummaryOperatorNewMemberRepository<SummaryOperatorNewMemberDaily>(mongoDBContext);

        var localStart = ConfigManager.SummarySetting.StartAt.DateTime;
        var localEnd   = ConfigManager.SummarySetting.EndAt.DateTime;

        foreach (var tz in DailyAndMonthlyTimezones)
        {
            var offset  = TimeSpan.Parse(tz);
            var startAt = new DateTimeOffset(localStart, offset);
            var endAt   = new DateTimeOffset(localEnd, offset);

            var summaryByPeriod = dailyRepo.GetListByTimeRange(startAt, endAt, timezone: tz)
                .GroupBy(r => r.PeriodStartAt)
                .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.OperatorId, r => r.NewMemberCount));

            RunComparison($"[日統計 新增會員 tz={tz}]", startAt, endAt, summaryByPeriod,
                _memberRepository.GetNewMemberCountDict,
                t => t.AddDays(1));
        }
    }

    /// <summary>
    /// 比對月統計 vs 來源，針對 +0 / +8 / -4 三個時區各別比對
    /// </summary>
    public void CheckMonthly()
    {
        var mongoDBContext = _dbHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var monthlyRepo = new SummaryOperatorNewMemberRepository<SummaryOperatorNewMemberMonthly>(mongoDBContext);

        var localStart = ConfigManager.SummarySetting.StartAt.DateTime;
        var localEnd   = ConfigManager.SummarySetting.EndAt.DateTime;

        foreach (var tz in DailyAndMonthlyTimezones)
        {
            var offset  = TimeSpan.Parse(tz);
            var startAt = new DateTimeOffset(localStart, offset);
            var endAt   = new DateTimeOffset(localEnd, offset);

            var summaryByPeriod = monthlyRepo.GetListByTimeRange(startAt, endAt, timezone: tz)
                .GroupBy(r => r.PeriodStartAt)
                .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.OperatorId, r => r.NewMemberCount));

            RunComparison($"[月統計 新增會員 tz={tz}]", startAt, endAt, summaryByPeriod,
                _memberRepository.GetNewMemberCountDict,
                t => t.AddMonths(1));
        }
    }

    private static void RunComparison(
        string label,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Dictionary<DateTimeOffset, Dictionary<string, long>> summaryByPeriod,
        Func<DateTimeOffset, DateTimeOffset, Dictionary<string, int>> getSourceDict,
        Func<DateTimeOffset, DateTimeOffset> advanceTime)
    {
        Console.WriteLine($"\n{label} 開始比對時間區間: {startAt:yyyy-MM-dd HH:mm zzz} ~ {endAt:yyyy-MM-dd HH:mm zzz}");
        Console.WriteLine("正在查詢資料...");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"{"時間",-18} | {"來源總計",10} | {"彙總總計",10} | {"差額",8} | {"不一致Op",8} | 狀態");
        Console.WriteLine(new string('-', 80));

        var mismatchPeriodCount = 0;
        var currentTime = startAt;

        while (currentTime < endAt)
        {
            var nextTime    = advanceTime(currentTime);
            var sourceDict  = getSourceDict(currentTime, nextTime);
            var summaryDict = summaryByPeriod.GetValueOrDefault(currentTime, []);

            var allOperators  = sourceDict.Keys.Union(summaryDict.Keys).ToList();
            var sourceTotal   = allOperators.Sum(op => (long)sourceDict.GetValueOrDefault(op, 0));
            var summaryTotal  = allOperators.Sum(op => summaryDict.GetValueOrDefault(op, 0));

            var mismatchOps = allOperators
                .Where(op => (long)sourceDict.GetValueOrDefault(op, 0) != summaryDict.GetValueOrDefault(op, 0))
                .OrderBy(op => op)
                .ToList();

            var isMatch = mismatchOps.Count == 0;
            if (!isMatch) mismatchPeriodCount++;

            var timeStr = currentTime.ToString("yyyy-MM-dd HH:mm");
            var status  = isMatch ? "一致" : "不一致";
            Console.WriteLine($"{timeStr,-18} | {sourceTotal,10} | {summaryTotal,10} | {sourceTotal - summaryTotal,8} | {mismatchOps.Count,8} | {status}");

            if (!isMatch)
            {
                Console.WriteLine($"  {"OperatorId",-30} | {"來源",10} | {"彙總",10} | {"差額",8}");
                foreach (var op in mismatchOps)
                {
                    var src  = (long)sourceDict.GetValueOrDefault(op, 0);
                    var sum  = summaryDict.GetValueOrDefault(op, 0);
                    Console.WriteLine($"  {op,-30} | {src,10} | {sum,10} | {src - sum,8}");
                }
            }

            currentTime = nextTime;
        }

        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"{label} 比對完成，共有 {mismatchPeriodCount} 個時間段不一致");
    }
}

/// <summary>
/// 新增會員統計比對服務介面
/// </summary>
public interface ISummaryNewMemberCheckService : IServiceBase
{
    void CheckHourly();
    void CheckDaily();
    void CheckMonthly();
}
