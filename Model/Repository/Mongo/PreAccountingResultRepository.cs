using Tool.Model.Entity.Mongo;
using MongoDB.Driver;

namespace Tool.Model.Repository.Mongo;

/// <summary>
/// 會員預下注記錄
/// </summary>
public class PreAccountingResultRepository(IMongoDatabase mongoDbContext) : MongoRepository<PreAccountingResult>(mongoDbContext), IPreAccountingResultRepository
{
    /// <summary>
    /// 取得指定營運商及會員的預下注記錄數量 (非同步)
    /// </summary>
    public long GetCountByOperatorIdAndMemberIds(string operatorId, List<string> memberIds)
    {
        var filter = Builders<PreAccountingResult>.Filter.And(
            Builders<PreAccountingResult>.Filter.Eq(x => x.OperatorId, operatorId),
            Builders<PreAccountingResult>.Filter.In(x => x.MemberId, memberIds)
        );

        return _mongoCollection.CountDocuments(filter);
    }
}


/// <summary>
/// 會員預下注記錄 Repository 介面
/// </summary>
public interface IPreAccountingResultRepository : IMongoRepository<PreAccountingResult>
{
    /// <summary>
    /// 取得指定營運商及會員的預下注記錄數量 (非同步)
    /// </summary>
    long GetCountByOperatorIdAndMemberIds(string operatorId, List<string> memberIds);
}