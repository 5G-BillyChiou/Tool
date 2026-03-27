using Tool.Enum;
using Tool.Model.Entity.Mongo;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;

namespace Tool.Model.Repository.Mongo;

/// <summary>
/// 會員針對各遊戲的下注彙總表。
/// </summary>
public class SummaryMemberGameRepository<TSummary>(IMongoDatabase mongoDbContext) : MongoRepository<TSummary>(mongoDbContext),
                                                                                    ISummaryMemberGameRepository<TSummary>
                                                                                    where TSummary : SummaryMemberGameBase
{
    /// <summary>
    /// 合併並更新 MemberId - 如果更新後會產生重複的 key，則合併數據
    /// </summary>
    public async Task MergeAndSetMemberIdByOperatorIdAndMemberIdAsync(string operatorId, string keepMemberId, string keepMemberAccount, string deleteMemberId)
    {
        // 1. 查詢所有要被處理的記錄（deleteMemberId 的記錄）
        var filterToMerge = Builders<TSummary>.Filter.And(
            Builders<TSummary>.Filter.Eq(x => x.OperatorId, operatorId),
            Builders<TSummary>.Filter.Eq(x => x.MemberId, deleteMemberId)
        );
        var recordsToMerge = await _mongoCollection.Find(filterToMerge).ToListAsync();

        if (!recordsToMerge.Any())
            return;

        // 2. 查詢所有 keepMemberId 的記錄（用於檢查重複）
        var filterExisting = Builders<TSummary>.Filter.And(
            Builders<TSummary>.Filter.Eq(x => x.OperatorId, operatorId),
            Builders<TSummary>.Filter.Eq(x => x.MemberId, keepMemberId)
        );
        var existingRecords = await _mongoCollection.Find(filterExisting).ToListAsync();

        // 3. 建立索引以快速查找，使用元組作為 key
        // Key = (PeriodStartAt, AgentPath, GameId, Timezone, CurrencySn, BetCategory)
        // 如果 keepMemberId 本身有重複記錄，需要先合併
        var existingRecordsMap = new Dictionary<(DateTimeOffset, string, string, string?, uint, AccountingBetCategoryEnum), TSummary>();
        var duplicateExistingIds = new List<ObjectId>();

        foreach (var existingRecord in existingRecords)
        {
            var key = GetComparisonKey(existingRecord);

            if (existingRecordsMap.TryGetValue(key, out var firstRecord))
            {
                // 發現重複記錄，需要合併到第一筆
                var filter = Builders<TSummary>.Filter.Eq(x => x.Id, firstRecord.Id);
                var update = Builders<TSummary>.Update
                    .Inc(x => x.TotalBetCount, existingRecord.TotalBetCount)
                    .Inc(x => x.TotalBetAmount, existingRecord.TotalBetAmount)
                    .Inc(x => x.TotalPayout, existingRecord.TotalPayout)
                    .Inc(x => x.TotalBonus, existingRecord.TotalBonus)
                    .Inc(x => x.TotalPromotionBonus, existingRecord.TotalPromotionBonus)
                    .Inc(x => x.TotalJackpot, existingRecord.TotalJackpot)
                    //.Inc(x => x.TotalLoginCount, existingRecord.TotalLoginCount)
                    .Inc(x => x.TotalBasicBetCount, existingRecord.TotalBasicBetCount)
                    .Inc(x => x.TotalBasicBetAmount, existingRecord.TotalBasicBetAmount)
                    .Inc(x => x.TotalBasicPayout, existingRecord.TotalBasicPayout)
                    .Inc(x => x.TotalExtraBetCount, existingRecord.TotalExtraBetCount)
                    .Inc(x => x.TotalExtraBetAmount, existingRecord.TotalExtraBetAmount)
                    .Inc(x => x.TotalExtraPayout, existingRecord.TotalExtraPayout)
                    .Inc(x => x.TotalFeatureBuyBetCount, existingRecord.TotalFeatureBuyBetCount)
                    .Inc(x => x.TotalFeatureBuyBetAmount, existingRecord.TotalFeatureBuyBetAmount)
                    .Inc(x => x.TotalFeatureBuyPayout, existingRecord.TotalFeatureBuyPayout)
                    .Set(x => x.UpdatedAt, DateTimeOffset.UtcNow);

                await _mongoCollection.UpdateOneAsync(filter, update);
                duplicateExistingIds.Add(existingRecord.Id);
            }
            else
            {
                existingRecordsMap[key] = existingRecord;
            }
        }

        // 刪除已合併的重複 existing 記錄
        if (duplicateExistingIds.Any())
        {
            await _mongoCollection.DeleteManyAsync(Builders<TSummary>.Filter.In(x => x.Id, duplicateExistingIds));
        }

        var upsertOperations = new List<WriteModel<TSummary>>();
        var deleteIds = new List<ObjectId>();

        // 4. 處理要合併的記錄
        foreach (var record in recordsToMerge)
        {
            var key = GetComparisonKey(record);

            if (existingRecordsMap.TryGetValue(key, out var existingRecord))
            {
                // 有重複，需要合併數據（累加所有數值欄位）
                var filter = Builders<TSummary>.Filter.Eq(x => x.Id, existingRecord.Id);
                var update = Builders<TSummary>.Update
                    .Inc(x => x.TotalBetCount, record.TotalBetCount)
                    .Inc(x => x.TotalBetAmount, record.TotalBetAmount)
                    .Inc(x => x.TotalPayout, record.TotalPayout)
                    .Inc(x => x.TotalBonus, record.TotalBonus)
                    .Inc(x => x.TotalPromotionBonus, record.TotalPromotionBonus)
                    .Inc(x => x.TotalJackpot, record.TotalJackpot)
                    //.Inc(x => x.TotalLoginCount, record.TotalLoginCount)
                    .Inc(x => x.TotalBasicBetCount, record.TotalBasicBetCount)
                    .Inc(x => x.TotalBasicBetAmount, record.TotalBasicBetAmount)
                    .Inc(x => x.TotalBasicPayout, record.TotalBasicPayout)
                    .Inc(x => x.TotalExtraBetCount, record.TotalExtraBetCount)
                    .Inc(x => x.TotalExtraBetAmount, record.TotalExtraBetAmount)
                    .Inc(x => x.TotalExtraPayout, record.TotalExtraPayout)
                    .Inc(x => x.TotalFeatureBuyBetCount, record.TotalFeatureBuyBetCount)
                    .Inc(x => x.TotalFeatureBuyBetAmount, record.TotalFeatureBuyBetAmount)
                    .Inc(x => x.TotalFeatureBuyPayout, record.TotalFeatureBuyPayout)
                    .Set(x => x.UpdatedAt, DateTimeOffset.UtcNow);

                upsertOperations.Add(new UpdateOneModel<TSummary>(filter, update));
                deleteIds.Add(record.Id);
            }
            else
            {
                // 沒有重複，直接更新 memberId 和 memberAccount
                var filter = Builders<TSummary>.Filter.Eq(x => x.Id, record.Id);
                var update = Builders<TSummary>.Update
                    .Set(x => x.MemberId, keepMemberId)
                    .Set(x => x.MemberAccount, keepMemberAccount)
                    .Set(x => x.UpdatedAt, DateTimeOffset.UtcNow);

                upsertOperations.Add(new UpdateOneModel<TSummary>(filter, update));
            }
        }

        // 5. 批量執行更新操作
        if (upsertOperations.Any())
        {
            await _mongoCollection.BulkWriteAsync(upsertOperations, new BulkWriteOptions { IsOrdered = false });
        }

        // 6. 刪除已合併的重複記錄
        if (deleteIds.Any())
        {
            await _mongoCollection.DeleteManyAsync(Builders<TSummary>.Filter.In(x => x.Id, deleteIds));
        }
    }

    /// <summary>
    /// 取得資料比對用的 Key（參考 BackstageScheduler 的 GetComparisonKey 模式）
    /// 注意：不包含 MemberId 和 MemberAccount，因為我們正是要合併不同會員的數據
    /// </summary>
    private (DateTimeOffset, string, string, string?, uint, AccountingBetCategoryEnum) GetComparisonKey(TSummary data)
    {
        return (data.PeriodStartAt, data.AgentPath, data.GameId, data.Timezone, data.CurrencySn, data.BetCategory);
    }

    /// <summary>
    /// 取得指定時間區間內的總下注數量。
    /// <para>(Shared)</para>
    /// </summary>
    public async Task<int> GetBetCountByMemberIdAsync(string memberId)
    {
        var filterBuilder = Builders<TSummary>.Filter;
        var filter = filterBuilder.Eq(x => x.MemberId, memberId);

        var result = await _mongoCollection.Aggregate()
            .Match(filter)
            .Group(x => 1, g => new { TotalBetCount = g.Sum(x => x.TotalBetCount) })
            .FirstOrDefaultAsync();

        return result?.TotalBetCount ?? 0;

    }

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
            .Where(x => x.PeriodStartAt >= startAt && x.PeriodStartAt < endAt);

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
                     && x.PeriodStartAt < endAt.UtcDateTime);

        if (string.IsNullOrEmpty(timezone))
        {
            timezone = TimeSpan.Zero.ToString();
            query = query.Where(x => x.Timezone == null || x.Timezone == timezone);
        }
        else
        {
            query = query.Where(x =>  x.Timezone == timezone);
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
/// 會員針對各遊戲的下注彙總表 介面。
/// </summary>
public interface ISummaryMemberGameRepository<TSummary> : IMongoRepository<TSummary>
                                                          where TSummary : SummaryMemberGameBase
{
    /// <summary>
    /// 合併並更新 MemberId - 如果更新後會產生重複的 key，則合併數據
    /// </summary>
    Task MergeAndSetMemberIdByOperatorIdAndMemberIdAsync(string operatorId, string keepMemberId, string keepMemberAccount, string deleteMemberId);

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