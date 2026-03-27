using Tool.Entity;
using Tool.Enum;
using Tool.Model.Entity.MySQL;


namespace Tool.Model.Repository.FiveGame;

/// <summary>
/// 營運商 實作
/// </summary>
public class OperatorRepository(FiveGameEntities context) : FiveGameRepository<Operator>(context), IOperatorRepository
{
    /// <summary>
    /// 取得存在的資料數量
    /// </summary>
    public int GetExistCount(List<string> operatorIds)
    {
        return this.GetAll()
               .Where(x => operatorIds.Contains(x.Id))
               .Count();
    }

    /// <summary>
    /// 取得錢包類型的營運商ID
    /// </summary>
    public List<string> GetIdsByWalletType(WalletTypeEnum walletType)
    {
        return this.GetAll()
               .Where(g => g.WalletType == walletType)
               .Select(x =>x.Id)
               .ToList();
    }

    /// <summary>
    /// 取得所有營運商清單
    /// </summary>
    public List<Operator> GetList()
    {
        return this.GetAll()
                   .Where(x => x.Deleted == false)
                   .ToList();
    }

    /// <summary>
    /// 取得所有營運商資訊（包含 WalletType）
    /// </summary>
    public List<Operator> GetAllOperators()
    {
        return this.GetAll().ToList();
    }
}

/// <summary>
/// 營運商 interface
/// </summary>
public interface IOperatorRepository : IRepository<Operator>
{
    /// <summary>
    /// 取得存在的資料數量
    /// </summary>
    int GetExistCount(List<string> operatorIds);

    /// <summary>
    /// 取得錢包類型的營運商ID
    /// </summary>
    List<string> GetIdsByWalletType(WalletTypeEnum walletType);

    /// <summary>
    /// 取得所有營運商清單
    /// </summary>
    List<Operator> GetList();

    /// <summary>
    /// 取得所有營運商資訊（包含 WalletType）
    /// </summary>
    List<Operator> GetAllOperators();
}