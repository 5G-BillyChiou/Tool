using Tool.Entity;
using Tool.Model.Entity.FiveGameTrans;

namespace Tool.Model.Repository.FiveGameTrans;

/// <summary>
/// 會員錢包 Repository 實作
/// </summary>
public class MemberWalletRepository : BaseRepository<MemberWallet>, IMemberWalletRepository
{
    public MemberWalletRepository(FiveGameTransEntities context) : base(context)
    {
    }

    /// <summary>
    /// 從指定的 member_id 清單中，回傳在 member_wallet 中存在的 member_id 集合
    /// </summary>
    public HashSet<string> GetExistingMemberIds(List<string> memberIds)
    {
        return this.GetAll()
                   .Where(x => memberIds.Contains(x.MemberId))
                   .Select(x => x.MemberId)
                   .ToHashSet();
    }
}

/// <summary>
/// 會員錢包 Repository 介面
/// </summary>
public interface IMemberWalletRepository : IRepository<MemberWallet>
{
    /// <summary>
    /// 從指定的 member_id 清單中，回傳在 member_wallet 中存在的 member_id 集合
    /// </summary>
    HashSet<string> GetExistingMemberIds(List<string> memberIds);
}