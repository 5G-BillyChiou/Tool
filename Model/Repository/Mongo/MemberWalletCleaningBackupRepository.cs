using Tool.Model.Entity.Mongo;
using MongoDB.Driver;

namespace Tool.Model.Repository.Mongo;


/// <summary>
/// 玩家餘額 Repository
/// </summary>
public class MemberWalletCleaningBackupRepository(IMongoDatabase mongoDbContext) : MongoRepository<MemberWalletCleaningBackup>(mongoDbContext), IMemberWalletCleaningBackupRepository
{
    
}

/// <summary>
/// 玩家餘額 Repository 介面
/// </summary>
public interface IMemberWalletCleaningBackupRepository : IMongoRepository<MemberWalletCleaningBackup>
{
   
}