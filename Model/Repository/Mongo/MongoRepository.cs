using Tool.Helper;
using Tool.Model.Entity.Mongo;
using MongoDB.Driver;
using Polly;
using Polly.Retry;
using System.Reflection;

namespace Tool.Model.Repository.Mongo;

public class MongoRepository<TDocument> : IMongoRepository<TDocument> where TDocument : BaseDocument
{
    private static readonly ILogger<MongoRepository<TDocument>> _logger = LoggerFactory.Create(builder => builder.AddConsole())
                                                                                    .CreateLogger<MongoRepository<TDocument>>();
    protected IMongoCollection<TDocument> _mongoCollection;
    protected readonly RetryPolicy _retryPolicy;

    public MongoRepository(IMongoDatabase database)
    {
        string collectionName = GetCollectionName(typeof(TDocument));
        this._mongoCollection = database.GetCollection<TDocument>(collectionName);

        const int maxRetryCount = 30;
        // 設定 Polly 重試策略
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                retryCount: maxRetryCount, //重試次數
                sleepDurationProvider: retryAttempt => GetRetryDelay(retryAttempt, _logger),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        $"MongoDB operation failed. Retry {retryCount} of {maxRetryCount}. " +
                        $"Waiting {timeSpan.TotalMilliseconds}ms before next retry."
                    );
                }
            );
    }

    private static string GetCollectionName(Type type)
    {
        var attribute = type.GetCustomAttribute<CollectionNameAttribute>()
            ?? throw new NullReferenceException();

        return attribute.Name;
    }

    /// <summary>
    /// Get All Documents
    /// </summary>
    /// <returns></returns>
    public IQueryable<TDocument> GetAll()
    {
        return _mongoCollection.AsQueryable();
    }

    /// <summary>
    /// Create Document
    /// </summary>
    /// <param name="document"></param>
    public void Create(TDocument document)
    {
        _mongoCollection.InsertOne(document);
    }

    public async Task<bool> CreateAsync(TDocument document)
    {
        if (document == null)
        {
            return false;
        }

        try
        {
            await _mongoCollection.InsertOneAsync(document);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    /// <summary>
    /// Create Documents
    /// </summary>
    /// <param name="documents"></param>
    public void Create(ICollection<TDocument> documents)
    {
        if (documents.Any())
            _mongoCollection.InsertMany(documents);
    }

    /// <summary>
    /// 包含重試機制的建立方式
    /// </summary>
    /// <param name="document"></param>
    public void CreateWithRetry(TDocument document)
    {
        _retryPolicy.Execute(() => _mongoCollection.InsertOne(document));
    }

    /// <summary>
    /// 包含重試機制的建立方式
    /// </summary>
    /// <param name="document"></param>
    public void CreateWithRetry(ICollection<TDocument> documents)
    {
        _retryPolicy.Execute(() => _mongoCollection.InsertMany(documents));
    }

    /// <summary>
    /// 批次 Create，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// <para>PS：失敗後只會重試一次</para>
    /// </summary>
    public void UnorderedRetryCreate(List<TDocument> datas, bool retry = true, Action<double>? onProgress = null)
    {
        // 一次一萬筆
        int batch = 10000;
        var count = 0;
        var progress = 0.0d;

        for (var i = 0; i < datas.Count; i += batch)
        {
            count = Math.Min(batch, datas.Count - i);

            ExecuteUnorderedRetryCreate(datas.GetRange(i, count), retry);

            progress = Math.Round((double)(i + count) / datas.Count, 2);
            onProgress?.Invoke(progress);
        }
    }

    /// <summary>
    /// Update Document
    /// </summary>
    /// <param name="document"></param>
    public void Update(TDocument document)
    {
        _mongoCollection.ReplaceOne(Builders<TDocument>.Filter.Eq(x => x.Id, document.Id), document);
    }

    public async Task UpdateAsync(TDocument document)
    {
        await _mongoCollection.ReplaceOneAsync(Builders<TDocument>.Filter.Eq(x => x.Id, document.Id), document);
    }

    /// <summary>
    /// Update Multiple Documents
    /// </summary>
    /// <param name="documents"></param>
    public void Update(ICollection<TDocument> documents)
    {
        var requests = documents.Select(x =>
            new ReplaceOneModel<TDocument>(Builders<TDocument>.Filter.Eq(p => p.Id, x.Id), x)
            { IsUpsert = true }
        );
        _mongoCollection.BulkWrite(requests);
    }

    public async Task UpdateAsync(ICollection<TDocument> documents)
    {
        var requests =
            documents.Select(x => new ReplaceOneModel<TDocument>(Builders<TDocument>.Filter.Eq(p => p.Id, x.Id), x)
            { IsUpsert = true }
            );
        await _mongoCollection.BulkWriteAsync(requests);
    }

    /// <summary>
    /// 包含重試機制的更新
    /// </summary>
    /// <param name="document"></param>
    public void UpdateWithRetry(TDocument document)
    {
        _retryPolicy.Execute(() => _mongoCollection.ReplaceOne(Builders<TDocument>.Filter.Eq(x => x.Id, document.Id), document));
    }

    /// <summary>
    /// Delete Document
    /// </summary>
    /// <param name="document"></param>
    public void Delete(TDocument document)
    {
        _mongoCollection.DeleteOne(Builders<TDocument>.Filter.Eq(x => x.Id, document.Id));
    }
    public async Task DeleteAsync(TDocument document)
    {
        await _mongoCollection.DeleteOneAsync(Builders<TDocument>.Filter.Eq(x => x.Id, document.Id));
    }

    /// <summary>
    /// Delete Documents
    /// </summary>
    /// <param name="documents"></param>
    public void Delete(ICollection<TDocument> documents)
    {
        if (documents.Count > 0)
            _mongoCollection.DeleteMany(Builders<TDocument>.Filter.In(x => x.Id, documents.Select(x => x.Id)));
    }

    public async Task DeleteAsync(ICollection<TDocument> documents)
    {
        if (documents.Any())
            await _mongoCollection.DeleteManyAsync(Builders<TDocument>.Filter.In(x => x.Id, documents.Select(x => x.Id)));
    }

    /// <summary>
    /// 包含重試機制的刪除
    /// </summary>
    /// <param name="documents"></param>
    public void DeleteWithRetry(ICollection<TDocument> documents)
    {
        if (documents.Count > 0)
        {
            _retryPolicy.Execute(() =>
                _mongoCollection.DeleteMany(Builders<TDocument>.Filter.In(x => x.Id, documents.Select(x => x.Id)))
            );
        }
    }

    /// <summary>
    /// 批次 Delete，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// </summary>
    public void UnorderedRetryDelete(List<TDocument> datas, bool retry = true, Action<double>? onProgress = null)
    {
        // 一次一萬筆
        int batch = 10000;
        var count = 0;
        var progress = 0.0d;

        for (var i = 0; i < datas.Count; i += batch)
        {
            count = Math.Min(batch, datas.Count - i);

            ExecuteUnorderedRetryDelete(datas.GetRange(i, count), retry);

            progress = Math.Round((double)(i + count) / datas.Count, 2);
            onProgress?.Invoke(progress);
        }
    }

    /// <summary>
    /// Reset Database Collection
    /// </summary>
    /// <param name="database"></param>
    public void ResetDatabase(IMongoDatabase database)
    {
        string collectionName = GetCollectionName(typeof(TDocument));
        this._mongoCollection = database.GetCollection<TDocument>(collectionName);
    }

    /// <summary>
    /// Find and update single document
    /// </summary>
    public TDocument FindOneAndUpdate(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update,
        FindOneAndUpdateOptions<TDocument>? options = null)
    {
        return _mongoCollection.FindOneAndUpdate(filter, update, options);
    }

    public Task<TDocument> FindOneAndUpdateAsync(FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument>? options = null)
    {
        return _mongoCollection.FindOneAndUpdateAsync(filter, update, options);
    }

    /// <summary>
    /// 批次 Upsert，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// </summary>
    /// <param name="operations"> 要執行的 WriteModel 操作列表 </param>
    /// <param name="retry"> 是否啟用重試機制，預設為 true </param>
    /// <param name="onProgress"> 進度回調函數，參數為完成進度（0.0 - 1.0） </param>
    public void UnorderedRetryUpsert(List<WriteModel<TDocument>> operations, bool retry = true, Action<double>? onProgress = null)
    {
        int batch = 10000;
        var progress = 0.0d;

        for (var i = 0; i < operations.Count; i += batch)
        {
            var count = Math.Min(batch, operations.Count - i);
            var batchOperations = operations.GetRange(i, count);
            ExecuteUnorderedRetryUpsert(batchOperations, retry);
            progress = Math.Round((double)(i + count) / operations.Count, 2);
            onProgress?.Invoke(progress);
        }
    }

    // 判斷是否為暫時性錯誤的幫助方法
    private bool IsTransientError(Exception ex)
    {
        if (ex is MongoCommandException mex)
        {
            // 常見的 AWS DocumentDB 暫時性錯誤碼，包含碎片容量升級時可能出現的錯誤碼
            if (mex.Code == 11600 || // 連接異常
                   mex.Code == 13436 || // 網絡錯誤
                   mex.Code == 6 ||     // 主機不可用
                   mex.Code == 7 ||     // 主機不是主節點
                   mex.Code == 89 ||    // 超時
                   mex.Code == 91 ||    // 故障轉移進行中 
                   mex.Code == 24 ||    // 主機正在進行維護
                   mex.Code == 42)      // 主機正在進行維護
            {
                return true;
            }
        }

        // 檢查錯誤訊息是否包含暫時性錯誤的關鍵字
        var errorMessage = ex.Message.ToLower();
        if (errorMessage.Contains("unable to process request") ||
            errorMessage.Contains("shard is unreachable") ||
            errorMessage.Contains("connection timeout") ||
            errorMessage.Contains("network error") ||
            errorMessage.Contains("temporary failure") ||
            errorMessage.Contains("maintenance") ||
            errorMessage.Contains("timeout") ||
            errorMessage.Contains("connection") ||
            errorMessage.Contains("socket io") ||
            errorMessage.Contains("authenticate") ||
            errorMessage.Contains("updating") ||
            errorMessage.Contains("write operation resulted in an error"))
        {
            return true;
        }

        _logger.LogError($"Mongo Retry Exception : {errorMessage}");

        return false;
    }

    private static TimeSpan GetRetryDelay(int retryAttempt, ILogger logger)
    {
        // 基礎延遲時間
        var baseDelay = TimeSpan.FromMilliseconds(100);

        if (retryAttempt <= 1)
            return baseDelay;

        // 指數退避
        var exponentialDelay = TimeSpan.FromMilliseconds(Math.Pow(1.2, retryAttempt - 2) * 1000);

        // 加入隨機抖動
        var jitter = RandomHelper.RandFunction(0, 50);

        var finalDelay = baseDelay + exponentialDelay + TimeSpan.FromMilliseconds(jitter);

        // 設定最大延遲時間
        var maxDelay = TimeSpan.FromSeconds(10);

        return TimeSpan.FromMilliseconds(Math.Min(finalDelay.TotalMilliseconds, maxDelay.TotalMilliseconds));
    }

    /// <summary>
    /// 批次 Create，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// <para>PS：失敗後只會重試一次</para>
    /// </summary>
    private void ExecuteUnorderedRetryCreate(ICollection<TDocument> datas, bool retry = true)
    {
        if (datas.Any() == false)
            return;

        try
        {
            var insertOptions = new InsertManyOptions() { IsOrdered = false }; // 設定為 `false` 讓 MongoDB 平行處理
            _mongoCollection.InsertMany(datas, insertOptions);

        }
        catch (MongoBulkWriteException<TDocument> ex)
        {
            _logger.LogError($"ExecuteUnorderedRetryCreate Retry：{retry}、Error：{ex.Message}");

            if (retry == false)
                throw;

            // 取得失敗的資料
            var errorDatas = ex.WriteErrors.Select(error => datas.ElementAt(error.Index))
                                           .ToArray();

            // 重新再一次 (只會重試一次)
            ExecuteUnorderedRetryCreate(errorDatas, false);
        }
        catch (Exception ex)
        {
            _logger.LogError($"ExecuteUnorderedRetryCreate Error：{ex.Message}");
            throw;
        }
    }


    /// <summary>
    /// 批次 Delete，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// </summary>
    private void ExecuteUnorderedRetryDelete(ICollection<TDocument> datas, bool retry = true)
    {
        if (datas.Any() == false)
            return;

        try
        {
            var bulkOps = new List<WriteModel<TDocument>>()
                {
                    new DeleteManyModel<TDocument>(Builders<TDocument>.Filter.In(x => x.Id, datas.Select(x => x.Id))),
                };

            var bulkWriteOptions = new BulkWriteOptions() { IsOrdered = false }; // 設定為 `false` 讓 MongoDB 平行處理
            _mongoCollection.BulkWrite(bulkOps, bulkWriteOptions);

        }
        catch (MongoBulkWriteException<TDocument> ex)
        {
            _logger.LogError($"ExecuteUnorderedRetryDelete Retry：{retry}、Error：{ex.Message}");

            if (retry == false)
                return;

            // 取得失敗的資料
            var errorDatas = ex.WriteErrors.Select(error => datas.ElementAt(error.Index))
                                           .ToArray();

            // 重新再一次 (只會重試一次)
            ExecuteUnorderedRetryDelete(errorDatas, false);
        }
        catch (Exception ex)
        {
            _logger.LogError($"ExecuteUnorderedRetryDelete Error：{ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 批次 Upsert，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// </summary>
    /// <param name="operations"></param>
    /// <param name="retry"></param>
    private void ExecuteUnorderedRetryUpsert(ICollection<WriteModel<TDocument>> operations, bool retry = true)
    {
        if (!operations.Any())
            return;

        try
        {
            var bulkOptions = new BulkWriteOptions()
            {
                IsOrdered = false,
            };
            _mongoCollection.BulkWrite(operations, bulkOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"ExecuteUnorderedRetryUpsert Error：{ex.Message}");
            throw;
        }
    }
}

public interface IMongoRepository<TDocument> where TDocument : class
{
    /// <summary>
    /// Get All Documents
    /// </summary>
    /// <returns></returns>
    IQueryable<TDocument> GetAll();

    /// <summary>
    /// Create Document
    /// </summary>
    /// <param name="document"></param>
    void Create(TDocument document);

    Task<bool> CreateAsync(TDocument document);

    /// <summary>
    /// Create Documents
    /// </summary>
    /// <param name="documents"></param>
    void Create(ICollection<TDocument> documents);

    /// <summary>
    /// 包含重試機制的建立方式
    /// </summary>
    /// <param name="document"></param>
    void CreateWithRetry(TDocument document);

    /// <summary>
    /// 包含重試機制的建立方式
    /// </summary>
    /// <param name="document"></param>
    void CreateWithRetry(ICollection<TDocument> documents);

    /// <summary>
    /// 批次 Create，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// </summary>
    void UnorderedRetryCreate(List<TDocument> datas, bool retry = true, Action<double>? onProgress = null);

    /// <summary>
    /// Update Document
    /// </summary>
    /// <param name="document"></param>
    void Update(TDocument document);

    /// <summary>
    /// Update Multiple Documents
    /// </summary>
    /// <param name="documents"></param>
    void Update(ICollection<TDocument> documents);

    /// <summary>
    /// 包含重試機制的更新
    /// </summary>
    /// <param name="document"></param>
    void UpdateWithRetry(TDocument document);

    /// <summary>
    /// Delete Document
    /// </summary>
    /// <param name="document"></param>
    void Delete(TDocument document);

    /// <summary>
    /// Delete Documents
    /// </summary>
    /// <param name="documents"></param>
    void Delete(ICollection<TDocument> documents);

    /// <summary>
    /// 包含重試機制的刪除
    /// </summary>
    /// <param name="documents"></param>
    void DeleteWithRetry(ICollection<TDocument> documents);

    /// <summary>
    /// 批次 Delete，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// </summary>
    void UnorderedRetryDelete(List<TDocument> datas, bool retry = true, Action<double>? onProgress = null);

    /// <summary>
    /// Reset Database Collection
    /// </summary>
    /// <param name="database"></param>
    void ResetDatabase(IMongoDatabase database);

    /// <summary>
    /// FindOneAndUpdate
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="update"></param>
    /// <param name="options"></param>
    TDocument FindOneAndUpdate(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update,
        FindOneAndUpdateOptions<TDocument>? options = null);

    /// <summary>
    /// FindOneAndUpdateAsync
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="update"></param>
    /// <param name="options"></param>
    Task<TDocument> FindOneAndUpdateAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update,
        FindOneAndUpdateOptions<TDocument>? options = null);

    /// <summary>
    /// 批次 Upsert，這邊會取消 Order 以加快速度，並且擁有失敗重試機制。
    /// </summary>
    /// <param name="operations"></param>
    /// <param name="retry"></param>
    /// <param name="onProgress"></param>
    void UnorderedRetryUpsert(List<WriteModel<TDocument>> operations, bool retry = true, Action<double>? onProgress = null);
}