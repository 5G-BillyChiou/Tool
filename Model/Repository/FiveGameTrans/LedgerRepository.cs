using Microsoft.EntityFrameworkCore;
using Tool.Entity;
using Tool.Model.Entity.FiveGameTrans;

namespace Tool.Model.Repository.FiveGameTrans;


public class LedgerRepository : BaseRepository<Ledger>, ILedgerRepository
{
    public LedgerRepository(FiveGameTransEntities context) : base(context)
    {
    }

    /// <summary>
    /// 批次查詢指定 operator_id 下，哪些 reference_id 存在於 Ledger
    /// </summary>
    public HashSet<string> GetExistingReferenceIds(string operatorId, IEnumerable<string> referenceIds)
    {
        var idList = referenceIds.ToList();
        return GetAll()
            .Where(x => x.OperatorId == operatorId && idList.Contains(x.ReferenceId))
            .AsNoTracking()
            .Select(x => x.ReferenceId)
            .ToHashSet();
    }

    /// <summary>
    /// 依照時間範圍查詢 Ledger 紀錄
    /// </summary>
    public List<Ledger> GetListByTimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        var query = GetAll()
            .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
            .Where(x => x.Type == 4 || x.Type == 5)
            .AsNoTracking();

        return query.ToList();
    }
}

public interface ILedgerRepository : IRepository<Ledger>
{
    /// <summary>
    /// 批次查詢指定 operator_id 下，哪些 reference_id 存在於 Ledger
    /// </summary>
    HashSet<string> GetExistingReferenceIds(string operatorId, IEnumerable<string> referenceIds);

    /// <summary>
    /// 依照時間範圍查詢 Ledger 紀錄
    /// </summary>
    List<Ledger> GetListByTimeRange(DateTimeOffset start, DateTimeOffset end);
}