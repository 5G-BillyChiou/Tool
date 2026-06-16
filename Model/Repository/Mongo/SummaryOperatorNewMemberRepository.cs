using MongoDB.Bson;
using MongoDB.Driver;
using System.Data.Entity;
using Tool.Model.Entity.Mongo;
using Tool.ViewModel;

namespace Tool.Model.Repository.Mongo;


/// <summary>
/// 營運商新增會員數 Repository 實作。
/// </summary>
public class SummaryOperatorNewMemberRepository<TSummary>(IMongoDatabase mongoDbContext) : MongoRepository<TSummary>(mongoDbContext),
                                                                                           ISummaryOperatorNewMemberRepository<TSummary>
                                                                                           where TSummary : SummaryOperatorNewMemberBase
{
    /// <summary>
    /// 取得指定時區的最早一筆記錄的開始時間
    /// <para>PS：這邊預設會撈 +0 時區</para>
    /// <para>(總排程)</para>
    /// </summary>
    public DateTimeOffset? GetMinStartAt(string? timezone = null)
    {
        var query = GetAll().AsNoTracking();

        if (string.IsNullOrWhiteSpace(timezone) || timezone == TimeSpan.Zero.ToString())
        {
            timezone = TimeSpan.Zero.ToString();
            query = query.Where(x => x.Timezone == null ||
                                     x.Timezone == timezone);
        }
        else
        {
            query = query.Where(x => x.Timezone != null)
                         .Where(x => x.Timezone == timezone);
        }

        return query.OrderBy(x => x.PeriodStartAt)
                    .FirstOrDefault()?.PeriodStartAt;
    }

    /// <summary>
    /// 取得下一個可用時間
    /// 排程抓資料時是用PeriodStartAt >= startAt && x.PeriodStartAt < endAt，這裡currentTime傳入startAt
    /// 那下一筆是要抓PeriodStartAt > currentTime的PeriodStartAt
    /// <para>PS：這邊預設會撈 +0 時區</para>
    /// <para>(總排程)</para>
    /// </summary>
    public DateTimeOffset? GetNextAvailableTime(DateTimeOffset currentTime, string? timezone = null)
    {
        var query = GetAll().Where(x => x.PeriodStartAt > currentTime)
                            .AsNoTracking();

        if (string.IsNullOrWhiteSpace(timezone) || timezone == TimeSpan.Zero.ToString())
        {
            timezone = TimeSpan.Zero.ToString();
            query = query.Where(x => x.Timezone == null ||
                                     x.Timezone == timezone);
        }
        else
        {
            query = query.Where(x => x.Timezone != null)
                         .Where(x => x.Timezone == timezone);
        }

        return query.OrderBy(x => x.PeriodStartAt)
                    .FirstOrDefault()?.PeriodStartAt;
    }

    /// <summary>
    /// 將 Repository 中指定時間範圍內的資料彙總為指定型別。
    /// <para>PS：這邊預設會撈 +0 時區</para>
    /// <para>(總排程)</para>
    /// </summary>
    public Task<List<TResult>> SummarizeToAsync<TResult>(DateTimeOffset startAt, DateTimeOffset endAt, IEnumerable<string>? operatorIds, string? timezone, int? batchSize) where TResult : SummaryOperatorNewMemberBase, new()
    {
        var filterBuilder = Builders<TSummary>.Filter;

        // 創建過濾條件
        FilterDefinition<TSummary> filter;

        if (string.IsNullOrWhiteSpace(timezone) || timezone == TimeSpan.Zero.ToString())
        {
            timezone = TimeSpan.Zero.ToString();
            filter = filterBuilder.Gte(x => x.PeriodStartAt, startAt) &
                     filterBuilder.Lt(x => x.PeriodStartAt, endAt) &
                     (filterBuilder.Eq(x => x.Timezone, null) | filterBuilder.Eq(x => x.Timezone, timezone));
        }
        else
        {
            filter = filterBuilder.Gte(x => x.PeriodStartAt, startAt) &
                     filterBuilder.Lt(x => x.PeriodStartAt, endAt) &
                     filterBuilder.Ne(x => x.Timezone, null) &
                     filterBuilder.Eq(x => x.Timezone, timezone);
        }

        if (operatorIds?.Any() ?? false)
        {
            if (operatorIds.Count() == 1)
                filter &= filterBuilder.Eq(x => x.OperatorId, operatorIds.First());
            else
                filter &= filterBuilder.In(x => x.OperatorId, operatorIds);
        }

        var options = new AggregateOptions
        {
            BatchSize = batchSize ?? 10000,
            AllowDiskUse = true
        };

        return Task.Run(() =>
        {
            var aggregateResults = new List<dynamic>();

            // 使用 cursor 批次讀取
            using (var cursor = _mongoCollection.Aggregate(options)
                                                .Match(filter)
                                                .Group(x => new
                                                {
                                                    x.OperatorId,
                                                    x.AgentPath
                                                },
                                                g => new
                                                {
                                                    OperatorId = g.Key.OperatorId,
                                                    AgentPath = g.Key.AgentPath,

                                                    // 取第一筆的資料
                                                    AgentId = g.First().AgentId,

                                                    // 總計欄位
                                                    NewMemberCount = g.Sum(x => x.NewMemberCount),
                                                })
                                                .ToCursor())
            {
                while (cursor.MoveNext())
                {
                    aggregateResults.AddRange(cursor.Current);
                }
            }

            // 轉換為最終結果物件
            var data = aggregateResults.Select(x =>
            {
                var summaryData = new TResult
                {
                    Id = ObjectId.GenerateNewId(),
                    PeriodStartAt = startAt,
                    PeriodEndAt = endAt,
                    OperatorId = x.OperatorId,
                    AgentId = x.AgentId,
                    AgentPath = x.AgentPath,
                    NewMemberCount = x.NewMemberCount,
                    Timezone = timezone,
                };

                return summaryData;
            }).ToList();

            return data;
        });
    }

    /// <summary>
    /// 取得指定時間區間內的營運商新增會員數。
    /// <para>PS：這邊預設會撈 +0 時區</para>
    /// <para>(總排程)</para>
    /// </summary>
    public long GetOperatorTotalNewMemberCount(DateTimeOffset startAt, DateTimeOffset endAt, string? timezone = null)
    {
        if (timezone == null)
            timezone = TimeSpan.Zero.ToString();

        return this.GetAll().Where(x => x.PeriodStartAt >= startAt)
                            .Where(x => x.PeriodEndAt <= endAt)
                            .Where(x => x.Timezone == null ||
                                        x.Timezone == timezone)
                            .Select(x => x.NewMemberCount)
                            .Sum();
    }

    /// <summary>
    /// 取得時間區間內的資料。
    /// <para>(總排程)</para>
    /// </summary>
    public List<TSummary> GetListByTimeRange(DateTimeOffset startAt, DateTimeOffset endAt, IEnumerable<string>? operatorIds = null, string? timezone = null)
    {
        var query = this.GetAll().Where(x => x.PeriodStartAt >= startAt)
                                 .Where(x => x.PeriodEndAt <= endAt);

        if (string.IsNullOrWhiteSpace(timezone) || timezone == TimeSpan.Zero.ToString())
        {
            timezone = TimeSpan.Zero.ToString();
            query = query.Where(x => x.Timezone == null ||
                                     x.Timezone == timezone);
        }
        else
            query = query.Where(x => x.Timezone == timezone);

        if (operatorIds?.Any() ?? false)
            query = query.Where(x => operatorIds.Contains(x.OperatorId));

        return query.ToList();
    }

    /// <summary>
    /// 批次 Update，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// <para>PS：失敗後只會重試一次</para>
    /// </summary>
    ///
    public void UnorderedRetryUpdate(List<TSummary> datas, bool retry = true, Action<double>? onProgress = null)
    {
        // 一次一萬筆
        int batch = 10000;
        var count = 0;
        var progress = 0.0d;

        for (var i = 0; i < datas.Count; i += batch)
        {
            count = Math.Min(batch, datas.Count - i);

            ExecuteUnorderedRetryUpdate(datas.GetRange(i, count), retry);

            progress = Math.Round((double)(i + count) / datas.Count, 2);
            onProgress?.Invoke(progress);
        }
    }

    /// <summary>
    /// 批次 Update，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// <para>PS：失敗後只會重試一次</para>
    /// <para> 總排程 </para>
    /// </summary>
    ///
    public void UnorderedRetryUpdate(List<TSummary> datas, bool retry, Action<double>? onProgress, int? batchSize)
    {
        int batch = batchSize == null ? 10000 : batchSize.Value;
        var count = 0;
        var progress = 0.0d;

        for (var i = 0; i < datas.Count; i += batch)
        {
            count = Math.Min(batch, datas.Count - i);

            ExecuteUnorderedRetryUpdate(datas.GetRange(i, count), retry);

            progress = Math.Round((double)(i + count) / datas.Count, 2);
            onProgress?.Invoke(progress);
        }
    }

    /// <summary>
    /// 批次 Update，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// <para>PS：失敗後只會重試一次</para>
    /// </summary>
    private void ExecuteUnorderedRetryUpdate(ICollection<TSummary> datas, bool retry = true)
    {
        if (datas.Count <= 0)
            return;

        try
        {
            var updates = new List<WriteModel<TSummary>>();

            foreach (var data in datas)
            {
                // 因為有使用 operator_id 當作分片鍵，所以必須加上 operator_id 當作條件之一，否則容易錯誤且效能差
                var filter = Builders<TSummary>.Filter.And(Builders<TSummary>.Filter.Eq(x => x.Id, data.Id),
                                                            Builders<TSummary>.Filter.Eq(x => x.OperatorId, data.OperatorId));

                var update = Builders<TSummary>.Update.Set(x => x.NewMemberCount, data.NewMemberCount);

                updates.Add(new UpdateOneModel<TSummary>(filter, update) { IsUpsert = false });
            }

            var bulkOptions = new BulkWriteOptions() { IsOrdered = false }; // 設定為 `false` 讓 MongoDB 平行處理
            _mongoCollection.BulkWrite(updates, bulkOptions);

        }
        catch (MongoBulkWriteException<TSummary> ex)
        {
            if (retry == false)
                throw;

            // 取得失敗的資料
            var errorDatas = ex.WriteErrors.Select(error => datas.ElementAt(error.Index))
                                           .ToArray();

            // 重新再更新一次
            // TODO：如果再次失敗的話，該怎麼處理？
            ExecuteUnorderedRetryUpdate(errorDatas, false);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// 取得時間範圍內的 新增會員數量
    /// <para>(Shared)</para>
    /// </summary>
    public async Task<List<NewMemberAccountData>> GetNewMemberListDataAsync(DateTimeOffset startAt, DateTimeOffset endAt, uint? agentId, List<string>? operatorIds, string? timezone = null)
    {
        var filterBuilder = Builders<TSummary>.Filter;
        var filter = filterBuilder.Gte(x => x.PeriodStartAt, startAt) &
                     filterBuilder.Lt(x => x.PeriodStartAt, endAt);

        if (!string.IsNullOrEmpty(timezone))
            filter &= filterBuilder.Eq(x => x.Timezone, timezone);

        if (agentId.HasValue)
            filter &= filterBuilder.Eq(x => x.AgentId, agentId.Value);

        if (operatorIds != null && operatorIds.Any())
        {
            if (operatorIds.Count == 1)
                filter &= filterBuilder.Eq(x => x.OperatorId, operatorIds.First());
            else
                filter &= filterBuilder.In(x => x.OperatorId, operatorIds);
        }

        var pipeline = _mongoCollection.Aggregate().Match(filter);

        // 直接用 period_start_at 分組
        var groupStage = new BsonDocument("$group",
                                          new BsonDocument
                                          {
                                              { "_id", "$period_start_at" },
                                              { "NewMemberCount", new BsonDocument("$sum", "$new_member_count") }
                                          });

        var projectStage = new BsonDocument("$project",
                                            new BsonDocument
                                            {
                                                { "_id", 0 },
                                                { "PeriodStartAt", "$_id" },
                                                { "NewMemberCount", 1 }
                                            });

        var result = await pipeline
                           .AppendStage<BsonDocument>(groupStage)
                           .AppendStage<BsonDocument>(projectStage)
                           .As<NewMemberAccountData>()
                           .ToListAsync();

        return result;
    }
}


/// <summary>
/// 營運商新增會員數 Repository 介面。
/// </summary>
public interface ISummaryOperatorNewMemberRepository<TSummary> : IMongoRepository<TSummary>
                                                                 where TSummary : SummaryOperatorNewMemberBase
{
    /// <summary>
    /// 取得指定時區的最早一筆記錄的開始時間 (排程)
    /// <para>PS：這邊預設會撈 +0 時區</para>
    /// <para>(總排程)</para>
    /// </summary>
    DateTimeOffset? GetMinStartAt(string? timezone = null);

    /// <summary>
    /// 取得下一筆可用時間 (排程)
    /// <para>PS：這邊預設會撈 +0 時區</para>
    /// <para>(總排程)</para>
    /// </summary>
    DateTimeOffset? GetNextAvailableTime(DateTimeOffset currentTime, string? timezone = null);

    /// <summary>
    /// 將 Repository 中指定時間範圍內的資料彙總為指定型別。
    /// <para>PS：這邊預設會撈 +0 時區</para>
    /// <para>(總排程)</para>
    /// </summary>
    Task<List<TResult>> SummarizeToAsync<TResult>(DateTimeOffset startAt, DateTimeOffset endAt, IEnumerable<string>? operatorIds, string? timezone, int? batchSize) where TResult : SummaryOperatorNewMemberBase, new();

    /// <summary>
    /// 取得指定時間區間內的營運商新增會員數。
    /// <para>PS：這邊預設會撈 +0 時區</para>
    /// <para>(總排程)</para>
    /// </summary>
    long GetOperatorTotalNewMemberCount(DateTimeOffset startAt, DateTimeOffset endAt, string? timezone = null);

    /// <summary>
    /// 取得時間區間內的資料。
    /// <para>(總排程)</para>
    /// </summary>
    List<TSummary> GetListByTimeRange(DateTimeOffset startAt, DateTimeOffset endAt, IEnumerable<string>? operatorIds = null, string? timezone = null);

    /// <summary>
    /// 批次 Update，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// <para>PS：失敗後只會重試一次</para>
    /// </summary>
    void UnorderedRetryUpdate(List<TSummary> datas, bool retry = true, Action<double>? onProgress = null);

    /// <summary>
    /// 批次 Update，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// <para>PS：失敗後只會重試一次</para>
    /// <para> 總排程 </para>
    /// </summary>
    void UnorderedRetryUpdate(List<TSummary> datas, bool retry, Action<double>? onProgress, int? batchSize);

    /// <summary>
    /// 取得時間範圍內的 新增會員數量
    /// <para>(Shared)</para>
    /// </summary>
    Task<List<NewMemberAccountData>> GetNewMemberListDataAsync(DateTimeOffset startAt, DateTimeOffset endAt, uint? agentId, List<string>? operatorIds, string? timezone = null);
}