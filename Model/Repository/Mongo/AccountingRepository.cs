using Tool.Model.Entity.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Tool.Enum;
using Microsoft.Extensions.Options;
using Tool.ViewModel.Options;

namespace Tool.Model.Repository.Mongo;

/// <summary>
/// 會員下注記錄 Repository
/// </summary>
public class AccountingRepository : MongoRepository<Accounting>, IAccountingRepository
{
    private readonly string[] _testOperatorIds;

    public AccountingRepository(IMongoDatabase mongoDbContext)
        : this(mongoDbContext, null)
    {
    }

    public AccountingRepository(IMongoDatabase mongoDbContext, IOptions<RepoOption>? options)
        : base(mongoDbContext)
    {
        _testOperatorIds = options?.Value?.TestOperatorIds ?? Array.Empty<string>();
    }

    /// <summary>
    /// 取得指定營運商及會員的下注記錄數量 (非同步)
    /// </summary>
    public Task<long> GetCountByOperatorIdAndMemberIdAsync(string operatorId, string memberId)
    {
        var filter = Builders<Accounting>.Filter.And(
            Builders<Accounting>.Filter.Eq(x => x.OperatorId, operatorId),
            Builders<Accounting>.Filter.Eq(x => x.MemberId, memberId)
        );

        return _mongoCollection.CountDocumentsAsync(filter);
    }

    /// <summary>
    /// 將指定的 deleteMemberId 會員下注記錄更新為 keepMemberId
    /// </summary>
    public async Task SetMemberIdByOperatorIdAndMemberIdAsync(string operatorId, string keepMemberId, string deleteMemberId)
    {
        var filter = Builders<Accounting>.Filter.And(
            Builders<Accounting>.Filter.Eq(x => x.OperatorId, operatorId),
            Builders<Accounting>.Filter.Eq(x => x.MemberId, deleteMemberId)
        );

        var update = Builders<Accounting>.Update
            .Set(x => x.MemberId, keepMemberId);

        await _mongoCollection.UpdateManyAsync(filter, update);
    }

    /// <summary>
    /// 取得指定時間區間的下注記錄總數
    /// </summary>
    public long GetTotalAccountingCount(DateTimeOffset startAt, DateTimeOffset endAt)
        => GetAll()
           .Where(a => a.FinishedAt >= startAt && a.FinishedAt < endAt)
           .Where(a => _testOperatorIds == null || _testOperatorIds.Contains(a.OperatorId) == false)
           .Where(a => a.TestAccount == false)                                  // 排除測試帳號
           .Where(a => a.CampaignType == CampaignTypeEnum.CampaignType_Default) // 排除活動贈送的注單
           .Count();

    /// <summary>
    /// 批次取得指定時間區間內，按分鐘分組的注單數量
    /// </summary>
    public Dictionary<DateTimeOffset, long> GetAccountingCountByMinute(DateTimeOffset startAt, DateTimeOffset endAt)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "FinishedAt", new BsonDocument { { "$gte", startAt.UtcDateTime }, { "$lt", endAt.UtcDateTime } } },
                { "TestAccount", false },
                { "CampaignType", (int)CampaignTypeEnum.CampaignType_Default },
                { "OperatorId", new BsonDocument("$nin", new BsonArray(_testOperatorIds)) }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "year", new BsonDocument("$year", "$FinishedAt") },
                        { "month", new BsonDocument("$month", "$FinishedAt") },
                        { "day", new BsonDocument("$dayOfMonth", "$FinishedAt") },
                        { "hour", new BsonDocument("$hour", "$FinishedAt") },
                        { "minute", new BsonDocument("$minute", "$FinishedAt") }
                    }
                },
                { "count", new BsonDocument("$sum", 1) }
            })
        };

        var result = _mongoCollection.Aggregate<BsonDocument>(pipeline).ToList();

        return result.ToDictionary(
            x => new DateTimeOffset(
                x["_id"]["year"].AsInt32,
                x["_id"]["month"].AsInt32,
                x["_id"]["day"].AsInt32,
                x["_id"]["hour"].AsInt32,
                x["_id"]["minute"].AsInt32,
                0, TimeSpan.Zero),
            x => (long)x["count"].AsInt32
        );
    }

    /// <summary>
    /// 批次取得指定時間區間內，按小時分組的注單數量
    /// </summary>
    public Dictionary<DateTimeOffset, long> GetAccountingCountByHour(DateTimeOffset startAt, DateTimeOffset endAt)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "FinishedAt", new BsonDocument { { "$gte", startAt.UtcDateTime }, { "$lt", endAt.UtcDateTime } } },
                { "TestAccount", false },
                { "CampaignType", (int)CampaignTypeEnum.CampaignType_Default },
                { "OperatorId", new BsonDocument("$nin", new BsonArray(_testOperatorIds)) }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "year", new BsonDocument("$year", "$FinishedAt") },
                        { "month", new BsonDocument("$month", "$FinishedAt") },
                        { "day", new BsonDocument("$dayOfMonth", "$FinishedAt") },
                        { "hour", new BsonDocument("$hour", "$FinishedAt") }
                    }
                },
                { "count", new BsonDocument("$sum", 1) }
            })
        };

        var result = _mongoCollection.Aggregate<BsonDocument>(pipeline).ToList();

        return result.ToDictionary(
            x => new DateTimeOffset(
                x["_id"]["year"].AsInt32,
                x["_id"]["month"].AsInt32,
                x["_id"]["day"].AsInt32,
                x["_id"]["hour"].AsInt32,
                0, 0, TimeSpan.Zero),
            x => (long)x["count"].AsInt32
        );
    }

    /// <summary>
    /// 批次檢查指定的 ObjectId 列表是否存在於 accounting 資料庫
    /// </summary>
    /// <param name="ids">要檢查的 ID 列表</param>
    /// <param name="batchSize">每批次查詢的數量，預設 5000</param>
    /// <param name="onProgress">進度回調 (已處理筆數, 總筆數)</param>
    public Dictionary<string, bool> CheckExistsByIds(List<string> ids, int batchSize = 5000, Action<int, int>? onProgress = null)
    {
        var result = new Dictionary<string, bool>();

        // 將字串轉換為 ObjectId，並建立對應關係
        var idMapping = new Dictionary<ObjectId, string>();
        foreach (var id in ids)
        {
            if (ObjectId.TryParse(id, out var objectId))
            {
                idMapping[objectId] = id;
                result[id] = false; // 預設為不存在
            }
            else
            {
                result[id] = false; // 無效的 ObjectId
            }
        }

        if (idMapping.Count == 0)
            return result;

        var objectIds = idMapping.Keys.ToList();
        var totalCount = objectIds.Count;
        var processedCount = 0;

        // 分批查詢
        for (int i = 0; i < totalCount; i += batchSize)
        {
            var batchIds = objectIds.Skip(i).Take(batchSize).ToList();

            // 查詢這批次存在的 ID
            var filter = Builders<Accounting>.Filter.In(x => x.Id, batchIds);
            var existingDocs = _mongoCollection.Find(filter)
                .Project(x => x.Id)
                .ToList();

            // 標記存在的 ID
            foreach (var existingId in existingDocs)
            {
                if (idMapping.TryGetValue(existingId, out var originalId))
                {
                    result[originalId] = true;
                }
            }

            processedCount += batchIds.Count;
            onProgress?.Invoke(processedCount, totalCount);
        }

        return result;
    }

    /// <summary>
    /// 取得指定時間區間內，有觸發 bonus 的注單列表 (只取需要的欄位)
    /// </summary>
    public List<(string OperatorId, string GameId, uint CurrencySn, long Bet, string GameData)> GetBonusListProjected(DateTimeOffset startAt, DateTimeOffset endAt, string operatorId, string gameId)
    {
        // 查詢順序優化：先過濾 OperatorId 和 GameId (通常有索引)，再過濾時間
        return this.GetAll()
                   .Where(x => x.OperatorId == operatorId)
                   .Where(x => x.GameId == gameId)
                   .Where(x => x.Bonus == true)
                   .Where(x => x.FinishedAt >= startAt && x.FinishedAt < endAt)
                   .Select(x => new { x.OperatorId, x.GameId, x.CurrencySn, x.Bet, x.GameData })
                   .ToList()
                   .Select(x => (x.OperatorId, x.GameId, x.CurrencySn, x.Bet, x.GameData ?? string.Empty))
                   .ToList();
    }
}

/// <summary>
/// 會員下注記錄 Repository 介面
/// </summary>
public interface IAccountingRepository : IMongoRepository<Accounting>
{
    /// <summary>
    /// 取得指定營運商及會員的下注記錄數量 (非同步)
    /// </summary>
    Task<long> GetCountByOperatorIdAndMemberIdAsync(string operatorId, string memberId);

    /// <summary>
    /// 將指定的 deleteMemberId 會員下注記錄更新為 keepMemberId
    /// </summary>
    Task SetMemberIdByOperatorIdAndMemberIdAsync(string operatorId, string keepMemberId, string deleteMemberId);

    /// <summary>
    /// 取得指定時間區間內的總注單數量。
    /// <para>(代理排程)</para>
    /// </summary>
    long GetTotalAccountingCount(DateTimeOffset startAt, DateTimeOffset endAt);

    /// <summary>
    /// 批次取得指定時間區間內，按分鐘分組的注單數量
    /// </summary>
    Dictionary<DateTimeOffset, long> GetAccountingCountByMinute(DateTimeOffset startAt, DateTimeOffset endAt);

    /// <summary>
    /// 批次取得指定時間區間內，按小時分組的注單數量
    /// </summary>
    Dictionary<DateTimeOffset, long> GetAccountingCountByHour(DateTimeOffset startAt, DateTimeOffset endAt);

    /// <summary>
    /// 批次檢查指定的 ObjectId 列表是否存在於 accounting 資料庫
    /// </summary>
    /// <param name="ids">要檢查的 ID 列表</param>
    /// <param name="batchSize">每批次查詢的數量，預設 5000</param>
    /// <param name="onProgress">進度回調 (已處理筆數, 總筆數)</param>
    Dictionary<string, bool> CheckExistsByIds(List<string> ids, int batchSize = 5000, Action<int, int>? onProgress = null);

    /// <summary>
    /// 取得指定時間區間內，有觸發 bonus 的注單列表 (只取需要的欄位)
    /// </summary>
    List<(string OperatorId, string GameId, uint CurrencySn, long Bet, string GameData)> GetBonusListProjected(DateTimeOffset startAt, DateTimeOffset endAt, string operatorId, string gameId);
}