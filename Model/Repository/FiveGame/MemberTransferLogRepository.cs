using Tool.Entity;
using Tool.Model.Entity.FiveGame;
using Tool.Model.Entity.MySQL;
using Microsoft.EntityFrameworkCore;

namespace Tool.Model.Repository.FiveGame;

/// <summary>
/// MemberTransferLog Repository 實作
/// </summary>
public class MemberTransferLogRepository(FiveGameEntities context) : FiveGameRepository<MemberTransferLog>(context), IMemberTransferLogRepository
{
    /// <summary>
    /// 取得指定時間區間內，特定營運商及會員的轉帳紀錄數量 (非同步)
    /// </summary>
    public Task<int> GetCountByOperatorIdAndMemberIdAsync(string operatorId, string memberId)
    {
        return GetAll()
               .Where(x => x.OperatorId == operatorId)
               .Where(x => x.MemberId == memberId)
               .CountAsync();
    }

    /// <summary>
    /// 將指定的 deleteMemberId 會員轉帳紀錄更新為 keepMemberId
    /// </summary>
    public async Task SetMemberIdByMemberIdAsync(string keepMemberId, string deleteMemberId)
    {
        await this.GetAll()
            .Where(d => d.MemberId == deleteMemberId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.MemberId, keepMemberId));
    }
}


/// <summary>
/// MemberTransferLog Repository 介面
/// </summary>
public interface IMemberTransferLogRepository : IRepository<MemberTransferLog>
{
    /// <summary>
    /// 取得指定時間區間內，特定營運商及會員的轉帳紀錄數量 (非同步)
    /// </summary>
    Task<int> GetCountByOperatorIdAndMemberIdAsync(string operatorId, string memberId);

    /// <summary>
    /// 將指定的 deleteMemberId 會員轉帳紀錄更新為 keepMemberId
    /// </summary>
    Task SetMemberIdByMemberIdAsync(string keepMemberId, string deleteMemberId);
}