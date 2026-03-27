using Tool.Helper;
using Tool.Model.Entity.Mongo;
using Tool.Model.Repository.Mongo;

namespace Tool.Service;

public class SummaryCheckV1Service(IDBHelper _dbBHelper) : ISummaryCheckV1Service
{
    private static readonly string[] DailyAndMonthlyTimezones = ["00:00:00", "08:00:00", "-04:00:00"];

    /// <summary>
    /// 比對分鐘統計：原始集合 vs _v1 集合（每個 Operator / Member 的數據是否一致）
    /// </summary>
    public void CheckSummaryMinuteV1()
    {
        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var startAt = ConfigManager.SummarySetting.StartAt;
        var endAt = ConfigManager.SummarySetting.EndAt;

        Console.WriteLine($"\n{new string('=', 100)}");
        Console.WriteLine($"  [分鐘統計 BQ比對]  時間區間: {startAt:yyyy-MM-dd HH:mm} ~ {endAt:yyyy-MM-dd HH:mm}");
        Console.WriteLine(new string('=', 100));

        var memberGameRepository = new SummaryMemberGameRepository<SummaryMemberGameMinute>(mongoDBContext);
        var memberGameV1Repository = new SummaryMemberGameRepository<SummaryMemberGameMinuteV1>(mongoDBContext);
        var operatorRepository = new SummaryOperatorRepository<SummaryOperatorMinute>(mongoDBContext);
        var operatorV1Repository = new SummaryOperatorRepository<SummaryOperatorMinuteV1>(mongoDBContext);

        var currentTime = startAt;

        while (currentTime < endAt)
        {
            RunMemberV1Comparison($"分鐘 Member [{currentTime:yyyy-MM-dd HH:mm}]", memberGameRepository.GetByPeriod(currentTime, currentTime.AddMinutes(1)), memberGameV1Repository.GetByPeriod(currentTime, currentTime.AddMinutes(1)));
            RunOperatorV1Comparison($"分鐘 Operator [{currentTime:yyyy-MM-dd HH:mm}]", operatorRepository.GetByPeriod(currentTime, currentTime.AddMinutes(1)), operatorV1Repository.GetByPeriod(currentTime, currentTime.AddMinutes(1)));

            currentTime = currentTime.AddMinutes(1);
        }
    }

    /// <summary>
    /// 比對小時統計：原始集合 vs _v1 集合（每個 Operator / Member 的數據是否一致）
    /// </summary>
    public void CheckSummaryHourlyV1()
    {
        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var startAt = ConfigManager.SummarySetting.StartAt;
        var endAt = ConfigManager.SummarySetting.EndAt;

        Console.WriteLine($"\n{new string('=', 100)}");
        Console.WriteLine($"  [小時統計 BQ比對]  時間區間: {startAt:yyyy-MM-dd HH:mm} ~ {endAt:yyyy-MM-dd HH:mm}");
        Console.WriteLine(new string('=', 100));

        var memberGameRepository = new SummaryMemberGameRepository<SummaryMemberGameHourly>(mongoDBContext);
        var memberGameV1Repository = new SummaryMemberGameRepository<SummaryMemberGameHourlyV1>(mongoDBContext);
        var operatorRepository = new SummaryOperatorRepository<SummaryOperatorHourly>(mongoDBContext);
        var operatorV1Repository = new SummaryOperatorRepository<SummaryOperatorHourlyV1>(mongoDBContext);

        var currentTime = startAt;

        while (currentTime < endAt)
        {
            RunMemberV1Comparison($"小時 Member [{currentTime:yyyy-MM-dd HH:mm}]", memberGameRepository.GetByPeriod(currentTime, currentTime.AddHours(1)), memberGameV1Repository.GetByPeriod(currentTime, currentTime.AddHours(1)));
            RunOperatorV1Comparison($"小時 Operator [{currentTime:yyyy-MM-dd HH:mm}]", operatorRepository.GetByPeriod(currentTime, currentTime.AddHours(1)), operatorV1Repository.GetByPeriod(currentTime, currentTime.AddHours(1)));

            currentTime = currentTime.AddHours(1);
        }
    }

    /// <summary>
    /// 比對日統計：原始集合 vs _v1 集合，針對 +0 / +8 / -4 三個時區各別比對
    /// </summary>
    public void CheckSummaryDailyV1()
    {
        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var startAt = ConfigManager.SummarySetting.StartAt;
        var endAt = ConfigManager.SummarySetting.EndAt;

        Console.WriteLine($"\n{new string('=', 100)}");
        Console.WriteLine($"  [日統計 BQ比對]  時間區間: {startAt:yyyy-MM-dd HH:mm} ~ {endAt:yyyy-MM-dd HH:mm}");
        Console.WriteLine(new string('=', 100));

        var memberGameRepository   = new SummaryMemberGameRepository<SummaryMemberGameDaily>(mongoDBContext);
        var memberGameV1Repository = new SummaryMemberGameRepository<SummaryMemberGameDailyV1>(mongoDBContext);
        var operatorRepository       = new SummaryOperatorRepository<SummaryOperatorDaily>(mongoDBContext);
        var operatorV1Repository     = new SummaryOperatorRepository<SummaryOperatorDailyV1>(mongoDBContext);

        var currentTime = startAt;

        while (currentTime < endAt)
        {
            RunMemberV1Comparison($"日 Member [{currentTime:yyyy-MM-dd HH:mm}]", memberGameRepository.GetByPeriod(currentTime, currentTime.AddDays(1)), memberGameV1Repository.GetByPeriod(currentTime, currentTime.AddDays(1)));
            RunOperatorV1Comparison($"日 Operator [{currentTime:yyyy-MM-dd HH:mm}]", operatorRepository.GetByPeriod(currentTime, currentTime.AddDays(1)), operatorV1Repository.GetByPeriod(currentTime, currentTime.AddDays(1)));

            currentTime = currentTime.AddDays(1);
        }

        //foreach (var tz in DailyAndMonthlyTimezones)
        //{
        //    var currentTime = startAt;

        //    while (currentTime < endAt)
        //    {
        //        RunMemberV1Comparison($"日 Member [{currentTime:yyyy-MM-dd HH:mm}][tz={tz}]", memberGameRepository.GetByPeriod(currentTime, currentTime.AddDays(1)), memberGameV1Repository.GetByPeriod(currentTime, currentTime.AddDays(1)));
        //        RunOperatorV1Comparison($"日 Operator [{currentTime:yyyy-MM-dd HH:mm}][tz={tz}]", operatorRepository.GetByPeriod(currentTime, currentTime.AddDays(1)), operatorV1Repository.GetByPeriod(currentTime, currentTime.AddDays(1)));

        //        currentTime = currentTime.AddDays(1);
        //    }
        //}
    }

    /// <summary>
    /// 比對月統計：原始集合 vs _v1 集合，針對 +0 / +8 / -4 三個時區各別比對
    /// </summary>
    public void CheckSummaryMonthlyV1()
    {
        var mongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
        var startAt = ConfigManager.SummarySetting.StartAt;
        var endAt = ConfigManager.SummarySetting.EndAt;

        Console.WriteLine($"\n{new string('=', 100)}");
        Console.WriteLine($"  [月統計 BQ比對]  時間區間: {startAt:yyyy-MM-dd HH:mm} ~ {endAt:yyyy-MM-dd HH:mm}");
        Console.WriteLine(new string('=', 100));

        var memberGameRepository   = new SummaryMemberGameRepository<SummaryMemberGameMonthly>(mongoDBContext);
        var memberGameV1Repository = new SummaryMemberGameRepository<SummaryMemberGameMonthlyV1>(mongoDBContext);
        var operatorRepository       = new SummaryOperatorRepository<SummaryOperatorMonthly>(mongoDBContext);
        var operatorV1Repository     = new SummaryOperatorRepository<SummaryOperatorMonthlyV1>(mongoDBContext);

        var currentTime = startAt;

        while (currentTime < endAt)
        {
            RunMemberV1Comparison($"月 Member [{currentTime:yyyy-MM-dd HH:mm}]", memberGameRepository.GetByPeriod(startAt, currentTime.AddMonths(1)), memberGameV1Repository.GetByPeriod(startAt, currentTime.AddMonths(1)));
            RunOperatorV1Comparison($"月 Operator [{currentTime:yyyy-MM-dd HH:mm}]", operatorRepository.GetByPeriod(startAt, currentTime.AddMonths(1)), operatorV1Repository.GetByPeriod(startAt, currentTime.AddMonths(1)));

            currentTime = currentTime.AddMonths(1);
        }

        //foreach (var tz in DailyAndMonthlyTimezones)
        //{
        //    var currentTime = startAt;

        //    while (currentTime < endAt)
        //    {
        //        RunMemberV1Comparison($"月 Member [{currentTime:yyyy-MM-dd HH:mm}][tz={tz}]", memberGameRepository.GetByPeriod(startAt, currentTime.AddMonths(1)), memberGameV1Repository.GetByPeriod(startAt, currentTime.AddMonths(1)));
        //        RunOperatorV1Comparison($"月 Operator [{currentTime:yyyy-MM-dd HH:mm}][tz={tz}]", operatorRepository.GetByPeriod(startAt, currentTime.AddMonths(1)), operatorV1Repository.GetByPeriod(startAt, currentTime.AddMonths(1)));

        //        currentTime = currentTime.AddMonths(1);
        //    }
        //}
    }

    // ─── Private helpers ────────────────────────────────────────────────────────

    private static void RunMemberV1Comparison<TOrig, TV1>(string label, List<TOrig> original, List<TV1> v1)
        where TOrig : SummaryMemberGameBase
        where TV1 : SummaryMemberGameBase
    {
        static string Key(SummaryMemberGameBase x) =>
            $"{x.PeriodStartAt:O}|{x.OperatorId}|{x.MemberId}|{x.AgentPath}|{x.GameId}|{x.CurrencySn}|{(int)x.BetCategory}|{x.Timezone ?? "00:00:00"}";

        if(original.Any(x => x.Timezone == null))
            for(int i = 0; i < original.Count; i++)
                if (original[i].Timezone == null)
                    original[i].Timezone = "00:00:00";

        RunV1Comparison(label, original, v1, Key, Key);
    }

    private static void RunOperatorV1Comparison<TOrig, TV1>(string label, List<TOrig> original, List<TV1> v1)
        where TOrig : SummaryOperatorBase
        where TV1 : SummaryOperatorBase
    {
        static string Key(SummaryOperatorBase x) =>
            $"{x.PeriodStartAt:O}|{x.OperatorId}|{x.AgentPath}|{x.GameId}|{x.CurrencySn}|{(int)x.BetCategory}|{x.Timezone ?? "00:00:00"}";

        if (original.Any(x => x.Timezone == null))
            for (int i = 0; i < original.Count; i++)
                if (original[i].Timezone == null)
                    original[i].Timezone = "00:00:00";

        RunV1Comparison(label, original, v1, Key, Key);
        RunOperatorEstimatorComparison(label, original, v1, Key, Key);
    }

    private static void RunOperatorEstimatorComparison<TOrig, TV1>(
        string label,
        List<TOrig> original,
        List<TV1> v1,
        Func<TOrig, string> origKey,
        Func<TV1, string> v1Key)
        where TOrig : SummaryOperatorBase
        where TV1 : SummaryOperatorBase
    {
        // Build first-record-per-key dicts (no merging — HyperLogLog bytes cannot be aggregated)
        var origDict = new Dictionary<string, TOrig>();
        foreach (var r in original)
            origDict.TryAdd(origKey(r), r);

        var v1Dict = new Dictionary<string, TV1>();
        foreach (var r in v1)
            v1Dict.TryAdd(v1Key(r), r);

        var inBoth = origDict.Keys.Intersect(v1Dict.Keys).ToList();
        var estimatorMismatches = new List<(string Key, string Field, int OrigLen, int V1Len, bool SameLen)>();

        foreach (var k in inBoth)
        {
            var o = origDict[k];
            var vv = v1Dict[k];
            CheckBytes(k, "EstimatorBytes",           o.EstimatorBytes,           vv.EstimatorBytes,           estimatorMismatches);
            CheckBytes(k, "BasicEstimatorBytes",      o.BasicEstimatorBytes,      vv.BasicEstimatorBytes,      estimatorMismatches);
            CheckBytes(k, "ExtraEstimatorBytes",      o.ExtraEstimatorBytes,      vv.ExtraEstimatorBytes,      estimatorMismatches);
            CheckBytes(k, "FeatureBuyEstimatorBytes", o.FeatureBuyEstimatorBytes, vv.FeatureBuyEstimatorBytes, estimatorMismatches);
        }

        const int maxDetail = 50;
        if (estimatorMismatches.Count > 0)
        {
            Console.WriteLine($"    [EstimatorBytes 不一致] (最多 {maxDetail} 筆):");
            foreach (var (key, field, origLen, v1Len, sameLen) in estimatorMismatches.Take(maxDetail))
                Console.WriteLine($"      Field: {field,-28}  原始長度: {origLen,6}  BQ長度: {v1Len,6}{(sameLen ? "  (長度相同但內容不同)" : "")}  Key: {key}");
        }
        else if (inBoth.Count > 0)
        {
            Console.WriteLine($"  [EstimatorBytes OK] 全部一致 ({inBoth.Count} 筆)");
        }
    }

    private static void CheckBytes(string key, string field, byte[] orig, byte[] v1,
        List<(string, string, int, int, bool)> mismatches)
    {
        if (!orig.SequenceEqual(v1))
            mismatches.Add((key, field, orig.Length, v1.Length, orig.Length == v1.Length));
    }

    private static void RunV1Comparison<TOrig, TV1>(
        string label,
        List<TOrig> originalData,
        List<TV1> v1Data,
        Func<TOrig, string> origKey,
        Func<TV1, string> v1Key)
        where TOrig : SummaryBetBase
        where TV1 : SummaryBetBase
    {
        var originalDict = BuildAggregatedDict(originalData, origKey);
        var v1Dict = BuildAggregatedDict(v1Data, v1Key);

        var allKeys = originalDict.Keys.Union(v1Dict.Keys).ToHashSet();
        var onlyInOrig = allKeys.Where(k => !v1Dict.ContainsKey(k)).ToList();
        var onlyInV1   = allKeys.Where(k => !originalDict.ContainsKey(k)).ToList();
        var inBoth     = allKeys.Where(k => originalDict.ContainsKey(k) && v1Dict.ContainsKey(k)).ToList();
        var mismatches = inBoth.Where(k => !originalDict[k].Matches(v1Dict[k])).ToList();

        var allMatch = onlyInOrig.Count == 0 && onlyInV1.Count == 0 && mismatches.Count == 0;
        var status   = allMatch ? "OK  " : "FAIL";

        // 每個時間段一行摘要：全部一致時不再展開細節
        Console.WriteLine($"  [{status}] {label,-38}  原始={originalData.Count,4}  BQ={v1Data.Count,4}  兩邊皆有={inBoth.Count,4}  BQ缺={onlyInOrig.Count,3}  原始缺={onlyInV1.Count,3}  不一致={mismatches.Count,3}");

        if (allMatch) return;

        // ── 有問題才展開明細 ──────────────────────────────────────────────────
        const int maxDetail = 50;

        if (onlyInOrig.Count > 0)
        {
            Console.WriteLine($"    [BQ缺少的Key] (最多 {maxDetail} 筆):");
            foreach (var k in onlyInOrig.Take(maxDetail))
                Console.WriteLine($"      {k}");
        }

        if (onlyInV1.Count > 0)
        {
            Console.WriteLine($"    [原始缺少的Key] (最多 {maxDetail} 筆):");
            foreach (var k in onlyInV1.Take(maxDetail))
                Console.WriteLine($"      {k}");
        }

        if (mismatches.Count > 0)
        {
            Console.WriteLine($"    [數據不一致明細] (最多 {maxDetail} 筆):");
            Console.WriteLine($"    {new string('-', 96)}");
            foreach (var key in mismatches.Take(maxDetail))
            {
                var orig = originalDict[key];
                var v1   = v1Dict[key];
                Console.WriteLine($"    Key: {key}");
                PrintFieldDiff("TotalBetCount",           orig.TotalBetCount,           v1.TotalBetCount);
                PrintFieldDiff("TotalBetAmount",          orig.TotalBetAmount,          v1.TotalBetAmount);
                PrintFieldDiff("TotalPayout",             orig.TotalPayout,             v1.TotalPayout);
                PrintFieldDiff("TotalBonus",              orig.TotalBonus,              v1.TotalBonus);
                PrintFieldDiff("TotalPromotionBonus",     orig.TotalPromotionBonus,     v1.TotalPromotionBonus);
                PrintFieldDiff("TotalJackpot",            orig.TotalJackpot,            v1.TotalJackpot);
                PrintFieldDiff("TotalBasicBetCount",      orig.TotalBasicBetCount,      v1.TotalBasicBetCount);
                PrintFieldDiff("TotalBasicBetAmount",     orig.TotalBasicBetAmount,     v1.TotalBasicBetAmount);
                PrintFieldDiff("TotalBasicPayout",        orig.TotalBasicPayout,        v1.TotalBasicPayout);
                PrintFieldDiff("TotalExtraBetCount",      orig.TotalExtraBetCount,      v1.TotalExtraBetCount);
                PrintFieldDiff("TotalExtraBetAmount",     orig.TotalExtraBetAmount,     v1.TotalExtraBetAmount);
                PrintFieldDiff("TotalExtraPayout",        orig.TotalExtraPayout,        v1.TotalExtraPayout);
                PrintFieldDiff("TotalFeatureBuyBetCount", orig.TotalFeatureBuyBetCount, v1.TotalFeatureBuyBetCount);
                PrintFieldDiff("TotalFeatureBuyBetAmount",orig.TotalFeatureBuyBetAmount,v1.TotalFeatureBuyBetAmount);
                PrintFieldDiff("TotalFeatureBuyPayout",   orig.TotalFeatureBuyPayout,   v1.TotalFeatureBuyPayout);
                Console.WriteLine();
            }
        }
    }

    private static Dictionary<string, SummaryBetAggregated> BuildAggregatedDict<T>(
        List<T> records, Func<T, string> keySelector) where T : SummaryBetBase
    {
        var dict = new Dictionary<string, SummaryBetAggregated>();
        foreach (var r in records)
        {
            var key = keySelector(r);
            if (dict.TryGetValue(key, out var existing))
                existing.Add(r);
            else
                dict[key] = SummaryBetAggregated.From(r);
        }
        return dict;
    }

    private static void PrintFieldDiff(string field, object original, object v1)
    {
        if (!Equals(original, v1))
            Console.WriteLine($"    {field,-30}: 原始={original}, BQ={v1}");
    }
}

public interface ISummaryCheckV1Service
{
    /// <summary>比對分鐘統計：原始集合 vs _v1 集合</summary>
    void CheckSummaryMinuteV1();
    /// <summary>比對小時統計：原始集合 vs _v1 集合</summary>
    void CheckSummaryHourlyV1();
    /// <summary>比對日統計：原始集合 vs _v1 集合</summary>
    void CheckSummaryDailyV1();
    /// <summary>比對月統計：原始集合 vs _v1 集合</summary>
    void CheckSummaryMonthlyV1();
}

/// <summary>
/// 用於 V1 比對時，將同一 Key 的多筆資料合計後進行比較的內部 DTO。
/// </summary>
internal class SummaryBetAggregated
{
    public long TotalBetCount { get; set; }
    public decimal TotalBetAmount { get; set; }
    public decimal TotalPayout { get; set; }
    public decimal TotalBonus { get; set; }
    public decimal TotalPromotionBonus { get; set; }
    public decimal TotalJackpot { get; set; }
    public long TotalLoginCount { get; set; }
    public long TotalBasicBetCount { get; set; }
    public decimal TotalBasicBetAmount { get; set; }
    public decimal TotalBasicPayout { get; set; }
    public long TotalExtraBetCount { get; set; }
    public decimal TotalExtraBetAmount { get; set; }
    public decimal TotalExtraPayout { get; set; }
    public long TotalFeatureBuyBetCount { get; set; }
    public decimal TotalFeatureBuyBetAmount { get; set; }
    public decimal TotalFeatureBuyPayout { get; set; }

    public static SummaryBetAggregated From(SummaryBetBase r) => new()
    {
        TotalBetCount = r.TotalBetCount,
        TotalBetAmount = r.TotalBetAmount,
        TotalPayout = r.TotalPayout,
        TotalBonus = r.TotalBonus,
        TotalPromotionBonus = r.TotalPromotionBonus,
        TotalJackpot = r.TotalJackpot,
        //TotalLoginCount = r.TotalLoginCount,
        TotalBasicBetCount = r.TotalBasicBetCount,
        TotalBasicBetAmount = r.TotalBasicBetAmount,
        TotalBasicPayout = r.TotalBasicPayout,
        TotalExtraBetCount = r.TotalExtraBetCount,
        TotalExtraBetAmount = r.TotalExtraBetAmount,
        TotalExtraPayout = r.TotalExtraPayout,
        TotalFeatureBuyBetCount = r.TotalFeatureBuyBetCount,
        TotalFeatureBuyBetAmount = r.TotalFeatureBuyBetAmount,
        TotalFeatureBuyPayout = r.TotalFeatureBuyPayout,
    };

    public void Add(SummaryBetBase r)
    {
        TotalBetCount += r.TotalBetCount;
        TotalBetAmount += r.TotalBetAmount;
        TotalPayout += r.TotalPayout;
        TotalBonus += r.TotalBonus;
        TotalPromotionBonus += r.TotalPromotionBonus;
        TotalJackpot += r.TotalJackpot;
        //TotalLoginCount += r.TotalLoginCount;
        TotalBasicBetCount += r.TotalBasicBetCount;
        TotalBasicBetAmount += r.TotalBasicBetAmount;
        TotalBasicPayout += r.TotalBasicPayout;
        TotalExtraBetCount += r.TotalExtraBetCount;
        TotalExtraBetAmount += r.TotalExtraBetAmount;
        TotalExtraPayout += r.TotalExtraPayout;
        TotalFeatureBuyBetCount += r.TotalFeatureBuyBetCount;
        TotalFeatureBuyBetAmount += r.TotalFeatureBuyBetAmount;
        TotalFeatureBuyPayout += r.TotalFeatureBuyPayout;
    }

    public bool Matches(SummaryBetAggregated o) =>
        TotalBetCount == o.TotalBetCount &&
        TotalBetAmount == o.TotalBetAmount &&
        TotalPayout == o.TotalPayout &&
        TotalBonus == o.TotalBonus &&
        TotalPromotionBonus == o.TotalPromotionBonus &&
        TotalJackpot == o.TotalJackpot &&
        TotalLoginCount == o.TotalLoginCount &&
        TotalBasicBetCount == o.TotalBasicBetCount &&
        TotalBasicBetAmount == o.TotalBasicBetAmount &&
        TotalBasicPayout == o.TotalBasicPayout &&
        TotalExtraBetCount == o.TotalExtraBetCount &&
        TotalExtraBetAmount == o.TotalExtraBetAmount &&
        TotalExtraPayout == o.TotalExtraPayout &&
        TotalFeatureBuyBetCount == o.TotalFeatureBuyBetCount &&
        TotalFeatureBuyBetAmount == o.TotalFeatureBuyBetAmount &&
        TotalFeatureBuyPayout == o.TotalFeatureBuyPayout;
}
