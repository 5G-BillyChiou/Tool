using MongoDB.Driver;
using Tool.Entity;
using Tool.Enum;
using Tool.Model.Entity.FiveGameTrans;

namespace Tool.Model.Repository.FiveGameTrans;

/// <summary>
/// 會員下注記錄 Repository
/// </summary>
public class AccountingRepository : BaseRepository<Accounting>, IAccountingRepository
{
    public AccountingRepository(FiveGameTransEntities context) : base(context)
    {
    }

    /// <summary>
    /// 從指定的 id 清單中，回傳在 accounting 中已存在的 id 集合
    /// </summary>
    public HashSet<string> GetExistingIds(List<string> ids)
    {
        return this.GetAll()
                   .Where(x => ids.Contains(x.Id))
                   .Select(x => x.Id)
                   .ToHashSet();
    }

    /// <summary>
    /// 取得指定時間區間的下注記錄總數
    /// </summary>
    public long GetTotalAccountingCount(DateTimeOffset startAt, DateTimeOffset endAt)
        => GetAll()
           .Where(a => a.FinishedAt >= startAt && a.FinishedAt < endAt)
           //.Where(a => _testOperatorIds == null || _testOperatorIds.Contains(a.OperatorId) == false)
           .Where(a => a.TestAccount == false)                                  // 排除測試帳號
           .Where(a => a.CampaignType == CampaignTypeEnum.CampaignType_Default) // 排除活動贈送的注單
           .Count();
}

/// <summary>
/// 會員下注記錄 Repository 介面
/// </summary>
public interface IAccountingRepository : IRepository<Accounting>
{
    /// <summary>
    /// 從指定的 id 清單中，回傳在 accounting 中已存在的 id 集合
    /// </summary>
    HashSet<string> GetExistingIds(List<string> ids);

    /// <summary>
    /// 取得指定時間區間的下注記錄總數
    /// </summary>
    long GetTotalAccountingCount(DateTimeOffset startAt, DateTimeOffset endAt);
}