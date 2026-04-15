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

        var memberGameRepository      = new SummaryMemberGameRepository<SummaryMemberGameMinute>(mongoDBContext);
        var memberGameV1Repository    = new SummaryMemberGameRepository<SummaryMemberGameMinuteV1>(mongoDBContext);
        var operatorRepository        = new SummaryOperatorRepository<SummaryOperatorMinute>(mongoDBContext);
        var operatorV1Repository      = new SummaryOperatorRepository<SummaryOperatorMinuteV1>(mongoDBContext);
        var memberGameHourlyRepository = new SummaryMemberGameRepository<SummaryMemberGameHourly>(mongoDBContext);
        var operatorHourlyRepository   = new SummaryOperatorRepository<SummaryOperatorHourly>(mongoDBContext);

        var currentTime     = startAt;
        var currentHourStart = new DateTimeOffset(startAt.Year, startAt.Month, startAt.Day, startAt.Hour, 0, 0, startAt.Offset);
        var hourlyMemberMinutes   = new List<SummaryMemberGameMinute>();
        var hourlyOperatorMinutes = new List<SummaryOperatorMinute>();

        while (currentTime < endAt)
        {
            var memberMinutes   = memberGameRepository.GetByPeriod(currentTime, currentTime.AddMinutes(1));
            var memberV1Minutes = memberGameV1Repository.GetByPeriod(currentTime, currentTime.AddMinutes(1));
            var operatorMinutes   = operatorRepository.GetByPeriod(currentTime, currentTime.AddMinutes(1));
            var operatorV1Minutes = operatorV1Repository.GetByPeriod(currentTime, currentTime.AddMinutes(1));

            RunMemberV1Comparison($"分鐘 Member [{currentTime:yyyy-MM-dd HH:mm}]",   memberMinutes,   memberV1Minutes);
            RunOperatorV1Comparison($"分鐘 Operator [{currentTime:yyyy-MM-dd HH:mm}]", operatorMinutes, operatorV1Minutes);

            hourlyMemberMinutes.AddRange(memberMinutes);
            hourlyOperatorMinutes.AddRange(operatorMinutes);

            currentTime = currentTime.AddMinutes(1);

            // 當累積的分鐘資料滿一個完整的整點小時，與小時統計資料進行比對
            if (currentTime >= currentHourStart.AddHours(1))
            {
                var nextHourStart = currentHourStart.AddHours(1);

                // 只比對完整的整點小時（起點必須在 startAt 之前或等於）
                if (startAt <= currentHourStart && hourlyMemberMinutes.Count > 0)
                {
                    Console.WriteLine($"\n  {new string('-', 98)}");
                    Console.WriteLine($"  [分鐘加總 vs 小時比對]  {currentHourStart:yyyy-MM-dd HH:mm} ~ {nextHourStart:yyyy-MM-dd HH:mm}");
                    Console.WriteLine($"  {new string('-', 98)}");

                    var hourlyMembers   = memberGameHourlyRepository.GetByPeriod(currentHourStart, nextHourStart);
                    var hourlyOperators = operatorHourlyRepository.GetByPeriod(currentHourStart, nextHourStart);

                    RunMemberCrossLevelComparison($"分鐘加總 vs 小時 Member [{currentHourStart:yyyy-MM-dd HH:mm}]",   hourlyMemberMinutes, hourlyMembers);
                    RunOperatorCrossLevelComparison($"分鐘加總 vs 小時 Operator [{currentHourStart:yyyy-MM-dd HH:mm}]", hourlyOperatorMinutes, hourlyOperators);
                    Console.WriteLine($"  {new string('-', 98)}");
                    Console.WriteLine($"  {new string('-', 98)}");
                }

                hourlyMemberMinutes.Clear();
                hourlyOperatorMinutes.Clear();
                currentHourStart = nextHourStart;
            }
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

        var memberGameRepository      = new SummaryMemberGameRepository<SummaryMemberGameHourly>(mongoDBContext);
        var memberGameV1Repository    = new SummaryMemberGameRepository<SummaryMemberGameHourlyV1>(mongoDBContext);
        var operatorRepository        = new SummaryOperatorRepository<SummaryOperatorHourly>(mongoDBContext);
        var operatorV1Repository      = new SummaryOperatorRepository<SummaryOperatorHourlyV1>(mongoDBContext);
        var memberGameDailyRepository = new SummaryMemberGameRepository<SummaryMemberGameDaily>(mongoDBContext);
        var operatorDailyRepository   = new SummaryOperatorRepository<SummaryOperatorDaily>(mongoDBContext);

        var currentTime    = startAt;
        var currentDayStart = new DateTimeOffset(startAt.Year, startAt.Month, startAt.Day, 0, 0, 0, startAt.Offset);
        var dailyMemberHourlies   = new List<SummaryMemberGameHourly>();
        var dailyOperatorHourlies = new List<SummaryOperatorHourly>();

        while (currentTime < endAt)
        {
            var memberHourlies   = memberGameRepository.GetByPeriod(currentTime, currentTime.AddHours(1));
            var memberV1Hourlies = memberGameV1Repository.GetByPeriod(currentTime, currentTime.AddHours(1));
            var operatorHourlies   = operatorRepository.GetByPeriod(currentTime, currentTime.AddHours(1));
            var operatorV1Hourlies = operatorV1Repository.GetByPeriod(currentTime, currentTime.AddHours(1));

            RunMemberV1Comparison($"小時 Member [{currentTime:yyyy-MM-dd HH:mm}]",   memberHourlies,   memberV1Hourlies);
            RunOperatorV1Comparison($"小時 Operator [{currentTime:yyyy-MM-dd HH:mm}]", operatorHourlies, operatorV1Hourlies);

            dailyMemberHourlies.AddRange(memberHourlies);
            dailyOperatorHourlies.AddRange(operatorHourlies);

            currentTime = currentTime.AddHours(1);

            // 當累積的小時資料滿一整天，與日統計資料進行比對
            if (currentTime >= currentDayStart.AddDays(1))
            {
                var nextDayStart = currentDayStart.AddDays(1);

                if (startAt <= currentDayStart && dailyMemberHourlies.Count > 0)
                {
                    Console.WriteLine($"\n  {new string('-', 98)}");
                    Console.WriteLine($"  [小時加總 vs 日比對]  {currentDayStart:yyyy-MM-dd} ~ {nextDayStart:yyyy-MM-dd}");
                    Console.WriteLine($"  {new string('-', 98)}");

                    var dailyMembers   = memberGameDailyRepository.GetByPeriod(currentDayStart, nextDayStart);
                    var dailyOperators = operatorDailyRepository.GetByPeriod(currentDayStart, nextDayStart);

                    RunMemberCrossLevelComparison($"小時加總 vs 日 Member [{currentDayStart:yyyy-MM-dd}]",   dailyMemberHourlies, dailyMembers);
                    RunOperatorCrossLevelComparison($"小時加總 vs 日 Operator [{currentDayStart:yyyy-MM-dd}]", dailyOperatorHourlies, dailyOperators);
                }

                dailyMemberHourlies.Clear();
                dailyOperatorHourlies.Clear();
                currentDayStart = nextDayStart;
            }
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

        var memberGameRepository        = new SummaryMemberGameRepository<SummaryMemberGameDaily>(mongoDBContext);
        var memberGameV1Repository      = new SummaryMemberGameRepository<SummaryMemberGameDailyV1>(mongoDBContext);
        var operatorRepository          = new SummaryOperatorRepository<SummaryOperatorDaily>(mongoDBContext);
        var operatorV1Repository        = new SummaryOperatorRepository<SummaryOperatorDailyV1>(mongoDBContext);
        var memberGameMonthlyRepository = new SummaryMemberGameRepository<SummaryMemberGameMonthly>(mongoDBContext);
        var operatorMonthlyRepository   = new SummaryOperatorRepository<SummaryOperatorMonthly>(mongoDBContext);

        var currentTime       = startAt;
        var currentMonthStart = new DateTimeOffset(startAt.Year, startAt.Month, 1, 0, 0, 0, startAt.Offset);
        var monthlyMemberDailies   = new List<SummaryMemberGameDaily>();
        var monthlyOperatorDailies = new List<SummaryOperatorDaily>();

        while (currentTime < endAt)
        {
            var memberDailies   = memberGameRepository.GetByPeriod(currentTime, currentTime.AddDays(1));
            var memberV1Dailies = memberGameV1Repository.GetByPeriod(currentTime, currentTime.AddDays(1));
            var operatorDailies   = operatorRepository.GetByPeriod(currentTime, currentTime.AddDays(1));
            var operatorV1Dailies = operatorV1Repository.GetByPeriod(currentTime, currentTime.AddDays(1));

            RunMemberV1Comparison($"日 Member [{currentTime:yyyy-MM-dd}]",   memberDailies,   memberV1Dailies);
            RunOperatorV1Comparison($"日 Operator [{currentTime:yyyy-MM-dd}]", operatorDailies, operatorV1Dailies);

            monthlyMemberDailies.AddRange(memberDailies);
            monthlyOperatorDailies.AddRange(operatorDailies);

            currentTime = currentTime.AddDays(1);

            // 當累積的日資料滿一整個月，與月統計資料進行比對
            if (currentTime >= currentMonthStart.AddMonths(1))
            {
                var nextMonthStart = currentMonthStart.AddMonths(1);

                if (startAt <= currentMonthStart && monthlyMemberDailies.Count > 0)
                {
                    Console.WriteLine($"\n  {new string('-', 98)}");
                    Console.WriteLine($"  [日加總 vs 月比對]  {currentMonthStart:yyyy-MM} ~ {nextMonthStart:yyyy-MM}");
                    Console.WriteLine($"  {new string('-', 98)}");

                    var monthlyMembers   = memberGameMonthlyRepository.GetByPeriod(currentMonthStart, nextMonthStart);
                    var monthlyOperators = operatorMonthlyRepository.GetByPeriod(currentMonthStart, nextMonthStart);

                    RunMemberCrossLevelComparison($"日加總 vs 月 Member [{currentMonthStart:yyyy-MM}]",   monthlyMemberDailies, monthlyMembers);
                    RunOperatorCrossLevelComparison($"日加總 vs 月 Operator [{currentMonthStart:yyyy-MM}]", monthlyOperatorDailies, monthlyOperators);
                }

                monthlyMemberDailies.Clear();
                monthlyOperatorDailies.Clear();
                currentMonthStart = nextMonthStart;
            }
        }
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

    /// <summary>
    /// 跨層比對 Member：key 不含 PeriodStartAt，讓多筆分鐘記錄 group 後與小時記錄對應。
    /// </summary>
    private static void RunMemberCrossLevelComparison<TOrig, TV1>(string label, List<TOrig> minutes, List<TV1> hourly)
        where TOrig : SummaryMemberGameBase
        where TV1 : SummaryMemberGameBase
    {
        static string Key(SummaryMemberGameBase x) =>
            $"{x.OperatorId}|{x.MemberId}|{x.AgentPath}|{x.GameId}|{x.CurrencySn}|{(int)x.BetCategory}|{x.Timezone ?? "00:00:00"}";

        if (minutes.Any(x => x.Timezone == null))
            for (int i = 0; i < minutes.Count; i++)
                if (minutes[i].Timezone == null)
                    minutes[i].Timezone = "00:00:00";

        RunV1Comparison(label, minutes, hourly, Key, Key);
    }

    /// <summary>
    /// 跨層比對 Operator：key 不含 PeriodStartAt，讓多筆分鐘記錄 group 後與小時記錄對應。
    /// 數值欄位用 RunV1Comparison 加總比對，EstimatorBytes 用 RunMergedEstimatorComparison merge 後比 Count。
    /// </summary>
    private static void RunOperatorCrossLevelComparison<TOrig, TV1>(string label, List<TOrig> minutes, List<TV1> hourly)
        where TOrig : SummaryOperatorBase
        where TV1 : SummaryOperatorBase
    {
        static string Key(SummaryOperatorBase x) =>
            $"{x.OperatorId}|{x.AgentPath}|{x.GameId}|{x.CurrencySn}|{(int)x.BetCategory}|{x.Timezone ?? "00:00:00"}";

        if (minutes.Any(x => x.Timezone == null))
            for (int i = 0; i < minutes.Count; i++)
                if (minutes[i].Timezone == null)
                    minutes[i].Timezone = "00:00:00";

        RunV1Comparison(label, minutes, hourly, Key, Key);
        RunMergedEstimatorComparison(label, minutes, hourly);
    }

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
        var estimatorMismatches = new List<(string Key, string Field, int OrigLen, int V1Len, bool SameLen, long OrigCount, long V1Count)>();

        foreach (var k in inBoth)
        {
            var o = origDict[k];
            var vv = v1Dict[k];
            CheckEstimatorBytes(k, "EstimatorBytes",           o.EstimatorBytes,           vv.EstimatorBytes,           estimatorMismatches);
            CheckEstimatorBytes(k, "BasicEstimatorBytes",      o.BasicEstimatorBytes,      vv.BasicEstimatorBytes,      estimatorMismatches);
            CheckEstimatorBytes(k, "ExtraEstimatorBytes",      o.ExtraEstimatorBytes,      vv.ExtraEstimatorBytes,      estimatorMismatches);
            CheckEstimatorBytes(k, "FeatureBuyEstimatorBytes", o.FeatureBuyEstimatorBytes, vv.FeatureBuyEstimatorBytes, estimatorMismatches);
        }

        const int maxDetail = 50;
        if (estimatorMismatches.Count > 0)
        {
            WriteRed($"    [EstimatorBytes Count 不一致] (最多 {maxDetail} 筆):");
            foreach (var (key, field, origLen, v1Len, sameLen, origCount, v1Count) in estimatorMismatches.Take(maxDetail))
            {
                var countMatch = origCount == v1Count ? "Count 一致" : "Count 不一致";
                WriteRed($"      Field: {field,-28}  原始長度: {origLen,6}  BQ長度: {v1Len,6}{(sameLen ? "  (長度相同但內容不同)" : "")}  原始Count: {origCount,8}  BQ Count: {v1Count,8}  ({countMatch})  Key: {key}");
            }
        }
        else if (inBoth.Count > 0)
        {
            Console.WriteLine($"  [EstimatorBytes OK] Count 全部一致 ({inBoth.Count} 筆)");
        }
    }

    private static void CheckEstimatorBytes(string key, string field, byte[] orig, byte[] v1,
        List<(string, string, int, int, bool, long, long)> mismatches)
    {
        if (orig.SequenceEqual(v1)) return;

        var origCount = (long)CardinalityEstimatorHelper.DeserializeAndDecompressEstimator(orig).Count();
        var v1Count   = (long)CardinalityEstimatorHelper.DeserializeAndDecompressEstimator(v1).Count();

        // bytes 不同但 Count 相同視為一致，不列入不一致清單
        if (origCount != v1Count)
            mismatches.Add((key, field, orig.Length, v1.Length, orig.Length == v1.Length, origCount, v1Count));
    }

    /// <summary>
    /// 將下層的 EstimatorBytes 依 key（不含 PeriodStartAt）merge 後，
    /// 與上層的 EstimatorBytes 解出的 Count 進行比對。
    /// </summary>
    private static void RunMergedEstimatorComparison<TOrig, TV1>(
        string label,
        List<TOrig> minuteRecords,
        List<TV1> hourlyRecords)
        where TOrig : SummaryOperatorBase
        where TV1 : SummaryOperatorBase
    {
        // 不含 PeriodStartAt，讓不同分鐘的記錄與小時記錄落在同一個 key
        static string Key(SummaryOperatorBase x) =>
            $"{x.OperatorId}|{x.AgentPath}|{x.GameId}|{x.CurrencySn}|{(int)x.BetCategory}|{x.Timezone ?? "00:00:00"}";

        // 將各分鐘的 bytes 收集成 List，後續 merge
        var minuteDict = new Dictionary<string, (List<byte[]> Est, List<byte[]> Basic, List<byte[]> Extra, List<byte[]> FeatureBuy)>();
        foreach (var r in minuteRecords)
        {
            var k = Key(r);
            if (!minuteDict.TryGetValue(k, out var lists))
            {
                lists = (new(), new(), new(), new());
                minuteDict[k] = lists;
            }
            if (r.EstimatorBytes.Length           > 0) lists.Est.Add(r.EstimatorBytes);
            if (r.BasicEstimatorBytes.Length      > 0) lists.Basic.Add(r.BasicEstimatorBytes);
            if (r.ExtraEstimatorBytes.Length      > 0) lists.Extra.Add(r.ExtraEstimatorBytes);
            if (r.FeatureBuyEstimatorBytes.Length > 0) lists.FeatureBuy.Add(r.FeatureBuyEstimatorBytes);
        }

        var hourlyDict = new Dictionary<string, TV1>();
        foreach (var r in hourlyRecords)
            hourlyDict.TryAdd(Key(r), r);

        var inBoth = minuteDict.Keys.Intersect(hourlyDict.Keys).ToList();
        var countMismatches = new List<(string Key, string Field, long MinuteCount, long HourlyCount)>();

        foreach (var k in inBoth)
        {
            var (est, basic, extra, featureBuy) = minuteDict[k];
            var h = hourlyDict[k];
            CompareEstimatorCount(k, "EstimatorBytes",           est,        h.EstimatorBytes,           countMismatches);
            CompareEstimatorCount(k, "BasicEstimatorBytes",      basic,      h.BasicEstimatorBytes,      countMismatches);
            CompareEstimatorCount(k, "ExtraEstimatorBytes",      extra,      h.ExtraEstimatorBytes,      countMismatches);
            CompareEstimatorCount(k, "FeatureBuyEstimatorBytes", featureBuy, h.FeatureBuyEstimatorBytes, countMismatches);
        }

        const int maxDetail = 50;
        if (countMismatches.Count > 0)
        {
            WriteRed($"    [{label}] [Merge Estimator Count 不一致] (最多 {maxDetail} 筆):");
            foreach (var (key, field, minCount, hourlyCount) in countMismatches.Take(maxDetail))
                WriteRed($"      Field: {field,-28}  分鐘 Merge Count: {minCount,8}  小時 Count: {hourlyCount,8}  Key: {key}");
        }
        else if (inBoth.Count > 0)
        {
            Console.WriteLine($"  [{label}] [Merge Estimator OK] 全部一致 ({inBoth.Count} 筆)");
        }
    }

    private static void CompareEstimatorCount(string key, string field, List<byte[]> minuteBytesList, byte[] hourlyBytes,
        List<(string, string, long, long)> mismatches)
    {
        var merged      = CardinalityEstimatorHelper.MergeBytesList(minuteBytesList.ToArray());
        var minuteCount = (long)CardinalityEstimatorHelper.DeserializeAndDecompressEstimator(merged).Count();
        var hourlyCount = (long)CardinalityEstimatorHelper.DeserializeAndDecompressEstimator(hourlyBytes).Count();

        if (minuteCount != hourlyCount)
            mismatches.Add((key, field, minuteCount, hourlyCount));
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
        if (originalData.Count == 0 && v1Data.Count == 0)
            return;

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
        var summaryLine = $"  [{status}] {label,-38}  原始={originalData.Count,4}  BQ={v1Data.Count,4}  兩邊皆有={inBoth.Count,4}  BQ缺={onlyInOrig.Count,3}  原始缺={onlyInV1.Count,3}  不一致={mismatches.Count,3}";
        if (allMatch)
            Console.WriteLine(summaryLine);
        else
            WriteRed(summaryLine);

        if (allMatch) return;

        // ── 有問題才展開明細 ──────────────────────────────────────────────────
        const int maxDetail = 50;

        if (onlyInOrig.Count > 0)
        {
            WriteRed($"    [BQ缺少的Key] (最多 {maxDetail} 筆):");
            foreach (var k in onlyInOrig.Take(maxDetail))
                WriteRed($"      {k}");
        }

        if (onlyInV1.Count > 0)
        {
            WriteRed($"    [原始缺少的Key] (最多 {maxDetail} 筆):");
            foreach (var k in onlyInV1.Take(maxDetail))
                WriteRed($"      {k}");
        }

        if (mismatches.Count > 0)
        {
            WriteRed($"    [數據不一致明細] (最多 {maxDetail} 筆):");
            WriteRed($"    {new string('-', 96)}");
            foreach (var key in mismatches.Take(maxDetail))
            {
                var orig = originalDict[key];
                var v1   = v1Dict[key];
                WriteRed($"    Key: {key}");
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
            WriteRed($"    {field,-30}: 原始={original}, BQ={v1}");
    }

    private static void WriteRed(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
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
public class SummaryBetAggregated
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
