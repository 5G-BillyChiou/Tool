using Tool.Entity;
using Tool.Model.Entity.FiveGameTrans;

namespace Tool.Model.Repository.FiveGameTrans;

/// <summary>
/// 會員預下注記錄 Repository
/// </summary>
public class PreAccountingResultRepository : BaseRepository<PreAccountingResult>, IPreAccountingResultRepository
{
	public PreAccountingResultRepository(FiveGameTransEntities context) : base(context)
	{
	}


    /// <summary>
    /// 取得指定時間範圍內的會員預下注記錄列表
    /// </summary
    public List<PreAccountingResult> GetListByTimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        return this.GetAll()
                    .Where(x => x.CreatedAt >= start )
                    .Where(x => x.CreatedAt <= end)
                    .ToList();
    }

    /// <summary>
    /// 依單號（Id）列表批次查詢
    /// </summary>
    public List<PreAccountingResult> GetListByIds(List<string> ids)
    {
        return this.GetAll()
                    .Where(x => ids.Contains(x.Id))
                    .ToList();
    }

    /// <summary>
    /// 從指定的 id 清單中，回傳在 pre_accounting_result 中已存在的 id 集合
    /// </summary>
    public HashSet<string> GetExistingIds(List<string> ids)
    {
        return this.GetAll()
                   .Where(x => ids.Contains(x.Id))
                   .Select(x => x.Id)
                   .ToHashSet();
    }
}

/// <summary>
/// 會員預下注記錄 Repository 介面
/// </summary>
public interface IPreAccountingResultRepository : IRepository<PreAccountingResult>
{
    /// <summary>
    /// 取得指定時間範圍內的會員預下注記錄列表
    /// </summary
    List<PreAccountingResult> GetListByTimeRange(DateTimeOffset start, DateTimeOffset end);

    /// <summary>
    /// 依單號（Id）列表批次查詢
    /// </summary>
    List<PreAccountingResult> GetListByIds(List<string> ids);

    /// <summary>
    /// 從指定的 id 清單中，回傳在 pre_accounting_result 中已存在的 id 集合
    /// </summary>
    HashSet<string> GetExistingIds(List<string> ids);
}