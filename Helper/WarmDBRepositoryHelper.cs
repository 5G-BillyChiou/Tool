using Tool.Model.Repository.Mongo;
using Tool.ViewModel;
using Tool.ViewModel.Options;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Tool.Helper;

/// <summary>
/// 協助處理 Warm DB Repository 工具
/// </summary>
public class WarmDBRepositoryHelper : IWarmDBRepositoryHelper
{
    private readonly IDBHelper _dbHelper;
    private readonly ConcurrentDictionary<(string, Type), object> _mongoHostToRepositoryMap = new();
    private readonly string _warmDbHost;

    /// <summary>
    /// 建構式
    /// </summary>
    public WarmDBRepositoryHelper(IOptions<WarmDbHost> options, IDBHelper dbHelper)
    {
        _dbHelper = dbHelper;
        _warmDbHost = options.Value.Host;

        // 檢查連線字串是否為空
        if (string.IsNullOrEmpty(_warmDbHost))
        {
            Console.WriteLine("警告：MongoDB 連線字串未設定，某些功能可能無法使用。");
        }
    }

    /// <summary>
    /// 取得指定的 Repository。
    /// </summary>
    public TRepository? GetRepository<TRepository, TDocument>() where TRepository : IMongoRepository<TDocument>
                                                                where TDocument : class
    {
        // 檢查連線字串
        if (string.IsNullOrEmpty(_warmDbHost))
        {
            Console.WriteLine("錯誤：MongoDB 連線字串未設定，無法建立 Repository。");
            return default;
        }

        var type = typeof(TRepository);
        var hostKey = (_warmDbHost, type);

        if (_mongoHostToRepositoryMap.TryGetValue(hostKey, out var repository))
            return (TRepository)repository;

        try
        {
            var mongoDb = _dbHelper.GetMongoDatabase(_warmDbHost);
            repository = Activator.CreateInstance(type, mongoDb)!;
            _mongoHostToRepositoryMap.TryAdd(hostKey, repository);

            return (TRepository)repository;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"錯誤：無法建立 MongoDB Repository - {ex.Message}");
            return default;
        }
    }
}

/// <summary>
/// 協助處理 Warm DB Repository 介面
/// </summary>
public interface IWarmDBRepositoryHelper
{
    /// <summary>
    /// 取得指定的 Repository。
    /// </summary>
    TRepository? GetRepository<TRepository, TDocument>() where TRepository : IMongoRepository<TDocument>
                                                        where TDocument : class;
}