using Tool.Model.Entity.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Tool.Model.Repository.Mongo;

/// <summary>
/// 代理商下注彙總表。
/// </summary>
public class SummaryOperatorRepository<TSummary>(IMongoDatabase mongoDbContext) : MongoRepository<TSummary>(mongoDbContext),
                                                                                               ISummaryOperatorRepository<TSummary>
                                                                                               where TSummary : SummaryOperatorBase
{
    /// <summary>
    /// 取得指定時間區間內的總注單數量。
    /// <para>PS：這邊只會撈 +0 時區的彙總 </para>
    /// <para>(總排程)</para>
    /// </summary>
    public long GetTotalBetCount(DateTimeOffset startAt, DateTimeOffset endAt, string? timezone = null)
    {
        if (string.IsNullOrEmpty(timezone))
            timezone = TimeSpan.Zero.ToString();

        var query = this.GetAll().Where(x => x.PeriodStartAt >= startAt)
                            .Where(x => x.PeriodEndAt <= endAt)
                            .Where(x => x.Timezone == null ||
                                        x.Timezone == timezone);

        return query.Select(x => (long)x.TotalBetCount).Sum();
    }

    /// <summary>
    /// 取得指定時間區間內的所有彙總記錄。
    /// </summary>
    public List<TSummary> GetByPeriod(DateTimeOffset startAt, DateTimeOffset endAt, string? timezone = null)
    {
        var query = _mongoCollection.AsQueryable()
            .Where(x => x.PeriodStartAt >= startAt.UtcDateTime && x.PeriodStartAt < endAt.UtcDateTime);

        if (string.IsNullOrEmpty(timezone))
        {
            timezone = TimeSpan.Zero.ToString();
            query = query.Where(x => x.Timezone == null || x.Timezone == timezone);
        }
        else
        {
            query = query.Where(x => x.Timezone == timezone);
        }

        return query.ToList();
    }

    /// <summary>
    /// 批次取得指定時間區間內，按 PeriodStartAt 分組的注單數量
    /// </summary>
    public Dictionary<DateTimeOffset, long> GetBetCountGroupByPeriod(DateTimeOffset startAt, DateTimeOffset endAt, string? timezone = null)
    {
        var query = _mongoCollection.AsQueryable()
           .Where(x => x.PeriodStartAt >= startAt.UtcDateTime
                    && x.PeriodStartAt < endAt.UtcDateTime
                    && (x.Timezone == null || x.Timezone == timezone));

        if (string.IsNullOrEmpty(timezone))
        {
            timezone = TimeSpan.Zero.ToString();
            query = query.Where(x => x.Timezone == null || x.Timezone == timezone);
        }
        else
        {
            query = query.Where(x => x.Timezone == timezone);
        }

        var result = query
            .GroupBy(x => x.PeriodStartAt)
            .Select(g => new
            {
                PeriodStartAt = g.Key,
                TotalBetCount = g.Sum(x => x.TotalBetCount)
            })
            .ToList();

        return result.ToDictionary(
            x => x.PeriodStartAt,
            x => (long)x.TotalBetCount
        );
    }
}

/// <summary>
/// 下注彙總表 介面。
/// </summary>
public interface ISummaryOperatorRepository<TSummary> : IMongoRepository<TSummary>
                                                        where TSummary : SummaryOperatorBase
{
    /// <summary>
    /// 取得指定時間區間內的總注單數量。
    /// <para>PS：這邊只會撈 +0 時區的彙總 </para>
    /// <para>(總排程)</para>
    /// </summary>
    long GetTotalBetCount(DateTimeOffset startAt, DateTimeOffset endAt, string? timezone = null);

    /// <summary>
    /// 取得指定時間區間內的所有彙總記錄。
    /// </summary>
    List<TSummary> GetByPeriod(DateTimeOffset startAt, DateTimeOffset endAt, string? timezone = null);

    /// <summary>
    /// 批次取得指定時間區間內，按 PeriodStartAt 分組的注單數量
    /// </summary>
    Dictionary<DateTimeOffset, long> GetBetCountGroupByPeriod(DateTimeOffset startAt, DateTimeOffset endAt, string? timezone = null);
}