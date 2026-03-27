using Tool.Entity;
using Tool.Model.Entity.FiveGame;
using Tool.Model.Entity.MySQL;

namespace Tool.Model.Repository.FiveGame;

/// <summary>
/// Member Repository 實作
/// </summary>
public class MemberCleaningBackupRepository(FiveGameEntities context) : FiveGameRepository<MemberCleaningBackup>(context), IMemberCleaningBackupRepository
{
    
}


/// <summary>
/// Member Repository 介面
/// </summary>
public interface IMemberCleaningBackupRepository : IRepository<MemberCleaningBackup>
{
}