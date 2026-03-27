using Microsoft.EntityFrameworkCore;
using Tool.Entity;
using System.Linq.Expressions;

namespace Tool.Model;

/// <summary>
/// 基礎MySQL Repository
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Context物件
    /// </summary>
    protected DbContext? _context { get; set; }

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="context"></param>
    public BaseRepository(DbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates the specified instance.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <exception cref="ArgumentNullException">instance</exception>
    public void Create(TEntity instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        _context.Set<TEntity>().Add(instance);
        SaveChanges();
    }

    /// <summary>
    /// 建立Entity並回傳
    /// </summary>
    /// <param name="instance"></param>
    public TEntity CreateAndReturn(TEntity instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        _context.Set<TEntity>().Add(instance);
        SaveChanges();
        return instance;
    }

    /// <summary>
    /// 批次建立物件
    /// </summary>
    /// <param name="instances"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void Create(ICollection<TEntity> instances)
    {
        if (instances == null)
        {
            throw new ArgumentNullException(nameof(instances));
        }

        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        if (instances.Any())
        {
            _context.Set<TEntity>().AddRange(instances);
            _context.ChangeTracker.DetectChanges();
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// 附帶Transaction的物件建立
    /// </summary>
    /// <param name="instance"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void CreateWithTransaction(TEntity instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                Reset();
                _context.Set<TEntity>().Add(instance);
                _context.ChangeTracker.DetectChanges();
                _context.SaveChanges();

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    /// <summary>
    /// 附帶Transaction效果的批次建立
    /// </summary>
    /// <param name="instances"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void CreateWithTransaction(ICollection<TEntity> instances)
    {
        if (instances == null)
        {
            throw new ArgumentNullException(nameof(instances));
        }

        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                Reset();
                _context.Set<TEntity>().AddRange(instances);
                _context.ChangeTracker.DetectChanges();
                _context.SaveChanges();

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    private void Reset()
    {
        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        var entries = _context.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToArray();
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                    entry.State = EntityState.Unchanged;
                    break;

                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;

                case EntityState.Deleted:
                    entry.Reload();
                    break;
            }
        }
    }

    /// <summary>
    /// Updates the specified instance.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <exception cref="ArgumentNullException">instance</exception>
    public void Update(TEntity instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        _context.Entry(instance).State = EntityState.Modified;
        SaveChanges();
    }

    /// <summary>
    /// Updates the multi instance.
    /// </summary>
    /// <param name="instances">The instances.</param>
    /// <exception cref="ArgumentNullException">instances</exception>
    public void Update(ICollection<TEntity> instances)
    {
        if (instances == null)
        {
            throw new ArgumentNullException(nameof(instances));
        }

        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        _context.UpdateRange(instances);
        SaveChanges();
    }

    /// <summary>
    /// create or update instance
    /// </summary>
    /// <param name="instance"></param>
    /// <exception cref="NullReferenceException"></exception>
    public void CreateOrUpdate(TEntity instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var properties = instance.GetType().GetProperties();
        var id = properties.Single(x => x.Name == "Id").GetValue(instance, null);

        if (id == null)
            throw new NullReferenceException("property");

        int value = Convert.ToInt32(id);
        if (value == 0)
            Create(instance);
        else
            Update(instance);
    }

    /// <summary>
    /// Deletes the specified instance.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <exception cref="ArgumentNullException">instance</exception>
    public void Delete(TEntity instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        _context.Entry(instance).State = EntityState.Deleted;
        SaveChanges();
    }

    /// <summary>
    /// 刪除物件
    /// </summary>
    /// <param name="collection"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void Delete(ICollection<TEntity> collection)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        foreach (var item in collection)
        {
            _context.Entry(item).State = EntityState.Deleted;
        }
        SaveChanges();
    }

    /// <summary>
    /// Gets the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <returns></returns>
    public TEntity? Get(Expression<Func<TEntity, bool>> predicate)
    {
        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        return _context.Set<TEntity>().FirstOrDefault(predicate);
    }

    /// <summary>
    /// 有無檢查
    /// </summary>
    public bool Any(Expression<Func<TEntity, bool>> predicate)
    {
        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        return _context.Set<TEntity>().Any(predicate);
    }

    /// <summary>
    /// Gets all.
    /// </summary>
    /// <returns></returns>
    public IQueryable<TEntity> GetAll()
    {
        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        return _context.Set<TEntity>().AsQueryable();
    }

    /// <summary>
    /// 儲存變更
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void SaveChanges()
    {
        if (_context == null)
        {
            throw new InvalidOperationException("DbContext is not initialized.");
        }

        _context.SaveChanges();
    }

    /// <summary>
    /// 資源釋放
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 資源釋放
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context?.Dispose();
            _context = null;
        }
    }
}