using Tool.Entity;
using Tool.Model.Entity.MySQL;
using Tool.ViewModel;

namespace Tool.Model.Repository.FiveGame;

/// <summary>
/// Member Repository 實作
/// </summary>
public class MemberRepository(FiveGameEntities context) : FiveGameRepository<Member>(context), IMemberRepository
{
    private readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger(typeof(MemberRepository));

    /// <summary>
    /// 查詢相同 operator_id 但有重複 Account 的會員資料 ( 包含 operator_id、account 和重複數量的列表 )
    /// </summary>
    public List<ResDuplicateAccount> GetDuplicateAccountsByOperatorIds(List<string>? operatorIds = null)
    {
        var query = this.GetAll();

        // 如果指定了 operatorId，則只查詢該營運商
        if (operatorIds != null && operatorIds.Count == 1)
        {
            query = query.Where(x => x.OperatorId == operatorIds.First());
        }
        else
        {
            if (operatorIds != null && operatorIds.Count > 1)
            {
                query = query.Where(x => operatorIds.Contains(x.OperatorId));
            }
        }

        var duplicateAccounts = query.GroupBy(x => new { x.OperatorId, x.Account })
                                        .Where(g => g.Count() > 1)
                                        .Select(g => new ResDuplicateAccount
                                        {
                                            OperatorId = g.Key.OperatorId,
                                            Account = g.Key.Account,
                                            Count = g.Count()
                                        })
                                        .OrderBy(x => x.OperatorId)
                                        .ThenBy(x => x.Account)
                                        .ToList();

        var totalDuplicates = duplicateAccounts.Sum(x => x.Count);
        _logger.LogInformation($"找到 {duplicateAccounts.Count} 組重複的帳號，共 {totalDuplicates} 筆資料");

        return duplicateAccounts;
    }

    /// <summary>
    /// 查詢營運商帳號重複的會員資料
    /// </summary>
    public List<Member> GetListByOperatorAndAccount(string operatorId, string account)
    {
        return this.GetAll()
                   .Where(x => x.OperatorId == operatorId)
                   .Where(x => x.Account == account)
                   .ToList();
    }

    /// <summary>
    /// 取得指定營運商的所有會員 ID
    /// </summary>
    public List<string> GetIdsByOperatorId(string operatorId)
    {
        return this.GetAll()
                   .Where(x => x.OperatorId == operatorId)
                   .Select(x => x.Id)
                   .ToList();
    }
}


/// <summary>
/// Member Repository 介面
/// </summary>
public interface IMemberRepository : IRepository<Member>
{
    /// <summary>
    /// 查詢相同 operator_id 但有重複 Account 的會員資料 ( 包含 operator_id、account 和重複數量的列表 )
    /// </summary>
    List<ResDuplicateAccount> GetDuplicateAccountsByOperatorIds(List<string>? operatorIds = null);

    /// <summary>
    /// 查詢營運商帳號重複的會員資料
    /// </summary>
    List<Member> GetListByOperatorAndAccount(string operatorId, string account);

    /// <summary>
    /// 取得指定營運商的所有會員 ID
    /// </summary>
    List<string> GetIdsByOperatorId(string operatorId);
}