using Tool.Model.Entity.Mongo;
using MongoDB.Driver;

namespace Tool.Model.Repository.Mongo;

/// <summary>
/// 玩家餘額 Repository
/// </summary>
public class MemberWalletRepository(IMongoDatabase mongoDbContext) : MongoRepository<MemberWallet>(mongoDbContext), IMemberWalletRepository
{
    /// <summary>
    /// 取得玩家所有餘額記錄 (非同步)
    /// </summary>
    public Task<List<MemberWallet>> GetListByMemberIdAsync(string memberId)
    {
        var filter = Builders<MemberWallet>.Filter.Eq(x => x.MemberId, memberId);
        var sort = Builders<MemberWallet>.Sort.Ascending(x => x.CreatedAt);

        return _mongoCollection.Find(filter)
                                .Sort(sort)
                                .ToListAsync();
    }

    /// <summary>
    /// 批次 Delete，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// </summary>
    public void UnorderedRetryDelete(List<string> memberIds, bool retry = true)
    {
        if (memberIds.Any() == false)
            return;

        try
        {
            var bulkOps = new List<WriteModel<MemberWallet>>()
                {
                    new DeleteManyModel<MemberWallet>(Builders<MemberWallet>.Filter.In("member_id", memberIds))
                };

            var bulkWriteOptions = new BulkWriteOptions() { IsOrdered = false }; // 設定為 `false` 讓 MongoDB 平行處理
            _mongoCollection.BulkWrite(bulkOps, bulkWriteOptions);

        }
        catch (MongoBulkWriteException<MemberWallet> ex)
        {
            if (retry == false)
                return;

            // 取得失敗的資料
            var errorDatas = ex.WriteErrors.Select(error => memberIds[error.Index]).ToList();

            // 重新再一次 (只會重試一次)
            UnorderedRetryDelete(errorDatas, false);
        }
    }

    /// <summary>
    /// 增加玩家身上的餘額（使用重試機制）
    /// </summary>
    /// <param name="memberId">會員 ID</param>
    /// <param name="value">要增加的金額</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> IncreaseBalanceWithRetry(string memberId, long value)
    {
        // 如果金額為 0 或負數，不執行操作
        if (value <= 0)
            return false;

        var filter = Builders<MemberWallet>.Filter.Eq(x => x.MemberId, memberId);
        var update = Builders<MemberWallet>.Update
            .Inc(x => x.Balance, value)
            .Set(x => x.UpdatedAt, DateTimeOffset.UtcNow);

        try
        {
            // 使用重試策略執行更新
            var result = await Task.Run(() => _retryPolicy.Execute(() =>
                _mongoCollection.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true })
            ));

            return result.ModifiedCount > 0 || result.UpsertedId != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"IncreaseBalanceWithRetry 失敗: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 批量取得存在錢包資料的會員 ID
    /// </summary>
    /// <param name="memberIds">會員 ID 清單</param>
    /// <returns>有錢包資料的會員 ID 清單</returns>
    public async Task<HashSet<string>> GetExistingMemberIdsAsync(List<string> memberIds)
    {
        if (memberIds == null || memberIds.Count == 0)
            return new HashSet<string>();

        var filter = Builders<MemberWallet>.Filter.In(x => x.MemberId, memberIds);
        var projection = Builders<MemberWallet>.Projection.Include(x => x.MemberId);

        var wallets = await _mongoCollection.Find(filter)
                                            .Project<MemberWallet>(projection)
                                            .ToListAsync();

        return wallets.Select(x => x.MemberId).ToHashSet();
    }
}

/// <summary>
/// 玩家餘額 Repository 介面
/// </summary>
public interface IMemberWalletRepository : IMongoRepository<MemberWallet>
{
    /// <summary>
    /// 取得玩家所有餘額記錄 (非同步)
    /// </summary>
    Task<List<MemberWallet>> GetListByMemberIdAsync(string memberId);

    /// <summary>
    /// 批次 Delete
    /// </summary>
    void UnorderedRetryDelete(List<string> memberIds, bool retry = true);

    /// <summary>
    /// 增加玩家身上的餘額（使用重試機制）
    /// </summary>
    Task<bool> IncreaseBalanceWithRetry(string memberId, long value);

    /// <summary>
    /// 批量取得存在錢包資料的會員 ID
    /// </summary>
    Task<HashSet<string>> GetExistingMemberIdsAsync(List<string> memberIds);
}