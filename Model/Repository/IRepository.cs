using System.Linq.Expressions;

namespace Tool.Entity;

/// <summary>
/// 基礎MySQL Repository Interface
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IRepository<TEntity> : IDisposable where TEntity : class
{
    /// <summary>
    /// 建立Entity
    /// </summary>
    /// <param name="instance"></param>
    void Create(TEntity instance);

    /// <summary>
    /// 建立Entity並回傳
    /// </summary>
    /// <param name="instance"></param>
    TEntity CreateAndReturn(TEntity instance);

    /// <summary>
    /// 建立多筆Entity
    /// </summary>
    /// <param name="instance"></param>
    void Create(ICollection<TEntity> instance);

    /// <summary>
    /// 使用transaction機制建立Entity
    /// </summary>
    /// <param name="instance"></param>
    void CreateWithTransaction(TEntity instance);

    /// <summary>
    /// 使用transaction機制建立多筆Entity
    /// </summary>
    /// <param name="instance"></param>
    void CreateWithTransaction(ICollection<TEntity> instance);

    /// <summary>
    /// 更新Entity
    /// </summary>
    /// <param name="instance"></param>
    void Update(TEntity instance);

    /// <summary>
    /// 更新多筆Entity
    /// </summary>
    /// <param name="instance"></param>
    void Update(ICollection<TEntity> instance);

    /// <summary>
    /// 判斷Id欄位是否有值，決定執行更新或是新增
    /// </summary>
    /// <param name="instance"></param>
    void CreateOrUpdate(TEntity instance);

    /// <summary>
    /// 刪除Entity
    /// </summary>
    /// <param name="instance"></param>
    void Delete(TEntity instance);

    /// <summary>
    /// 刪除多筆Entity
    /// </summary>
    /// <param name="collection"></param>
    void Delete(ICollection<TEntity> collection);

    /// <summary>
    /// 使用Lambda表示式取得Entity
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    TEntity? Get(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 取得所有Entity
    /// </summary>
    /// <returns></returns>
    IQueryable<TEntity> GetAll();

    /// <summary>
    /// 執行Query
    /// </summary>
    void SaveChanges();

    /// <summary>
    /// 有無檢查
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    bool Any(Expression<Func<TEntity, bool>> predicate);
}