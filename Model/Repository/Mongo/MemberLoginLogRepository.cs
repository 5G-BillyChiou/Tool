using Tool.Model.Entity.Mongo;
using MongoDB.Driver;

namespace Tool.Model.Repository.Mongo;

/// <summary>
/// 會員登入日誌 Repository
/// </summary>
public class MemberLoginLogRepository(IMongoDatabase mongoDbContext) : MongoRepository<MemberLoginLog>(mongoDbContext), IMemberLoginLogRepository
{
    /// <summary>
    /// 將指定的 deleteMemberId 會員登入日誌更新為 keepMemberId
    /// </summary>
    public async Task SetMemberIdByMemberIdAsync(string keepMemberId, string deleteMemberId)
    {
        var filter = Builders<MemberLoginLog>.Filter.Eq(x => x.MemberId, deleteMemberId);
        var update = Builders<MemberLoginLog>.Update
            .Set(x => x.MemberId, keepMemberId);

        await _mongoCollection.UpdateManyAsync(filter, update);
    }

    /// <summary>
    /// 取得指定會員的登入日誌筆數
    /// </summary>
    public Task<long> GetCountByOperatorIdAndMemberIdAsync(string operatorId, string memberId)
    {
        var filter = Builders<MemberLoginLog>.Filter.And(
             Builders<MemberLoginLog>.Filter.Eq(x => x.OperatorId, operatorId),
             Builders<MemberLoginLog>.Filter.Eq(x => x.MemberId, memberId)
         );
        return _mongoCollection.CountDocumentsAsync(filter);
    }
}

/// <summary>
/// 會員登入日誌 Repository
/// </summary>
public interface IMemberLoginLogRepository : IMongoRepository<MemberLoginLog>
{
    /// <summary>
    /// 將指定的 deleteMemberId 會員登入日誌更新為 keepMemberId
    /// </summary>
    Task SetMemberIdByMemberIdAsync(string keepMemberId, string deleteMemberId);

    /// <summary>
    /// 取得指定會員的登入日誌筆數
    /// </summary>
    Task<long> GetCountByOperatorIdAndMemberIdAsync(string operatorId, string memberId);
}