using Tool.Entity;
using Tool.Model.Entity.FiveGame;
using Tool.Model.Entity.MySQL;

namespace Tool.Model.Repository.FiveGame;

/// <summary>
/// 玩家連線資訊 Repository
/// </summary>
public class MemberSessionRepository(FiveGameEntities context) : FiveGameRepository<MemberSession>(context), IMemberSessionRepository
{
    /// <summary>
    /// 判斷資料存在
    /// </summary>
    public bool Exists(List<string> memberIds)
    {
        var exists = GetAll()
            .Where(x => memberIds.Contains(x.MemberId))
            .Any();
        return exists;
    }
}

/// <summary>
/// 玩家連線資訊 Repository Interface
/// </summary>
public interface IMemberSessionRepository : IRepository<MemberSession>
{
    /// <summary>
    /// 判斷資料存在
    /// </summary>
    bool Exists(List<string> memberIds);
}