using System.Collections.Concurrent;
using Tool.Helper;
using MongoDB.Driver;

namespace Tool.Helper;

/// <summary>
/// DBHelper
/// </summary>
public class DBHelper : IDBHelper
{
    private static readonly ConcurrentDictionary<string, MongoClient> _clientCache = new(); //Mongo會自動進行線程安全及資源管理，因此不需要重複建立實體
    private static readonly ConcurrentDictionary<string, string> _agentHostMap = new();

    /// <summary>
    /// 快速取得MongoDB資料庫
    /// </summary>
    public IMongoDatabase GetMongoDatabase(string connectionString)
    {
        var client = _clientCache.GetOrAdd(connectionString, connStr =>
        {
            var mongoUrl = new MongoUrl(connStr);
            var settings = MongoClientSettings.FromUrl(mongoUrl);

            if (IsDocumentDb(connStr))
            {
                if (TunnelHelper.TunnelEnabled())
                {
                    var tunnelPort = TunnelHelper.CreateTunnel(mongoUrl.Server.Host, mongoUrl.Server.Port);
                    settings.Server = new MongoServerAddress("localhost", (int)tunnelPort);
                }

                // DocumentDB 設定
                settings.UseTls = true;
                settings.AllowInsecureTls = true;
                settings.RetryWrites = false;
            }
            else
            {
                if (TunnelHelper.TunnelEnabled())
                {
                    var firstServer = mongoUrl.Servers.First(); // 取第一個節點建立 tunnel
                    var tunnelPort = TunnelHelper.CreateTunnel(firstServer.Host, firstServer.Port);

                    // 因為 ReplicaSet 模式下其餘節點無法透過 localhost 連線
                    // 所以只能在非 replicaSet 或 dev 環境中使用
                    settings.Server = new MongoServerAddress("localhost", (int)tunnelPort);
                    settings.ReplicaSetName = null; // 避免連線失敗
                }

                // 標準 MongoDB 設定
                settings.UseTls = false;
                settings.AllowInsecureTls = false;
                settings.RetryWrites = true;
                settings.ReadPreference = ReadPreference.PrimaryPreferred;
                settings.DirectConnection = true;
            }

            settings.MaxConnectionPoolSize = 200;
            settings.MinConnectionPoolSize = 20;
            settings.MaxConnecting = 5;

            return new MongoClient(settings);
        });

        return client.GetDatabase(new MongoUrl(connectionString).DatabaseName);
    }

    /// <summary>
    /// 建立 DocumentDB 連線字串
    /// </summary>
    private string BuildDocumentDbConnectionString(MongoUrl mongoUrl, string host)
    {
        return $"mongodb://{mongoUrl.Username}:{mongoUrl.Password}@{host}:{mongoUrl.Server.Port}/{mongoUrl.DatabaseName}";
    }

    /// <summary>
    /// 建立標準 MongoDB 連線字串
    /// </summary>
    private string BuildMongoConnectionString(MongoUrl mongoUrl, string host)
    {
        var builder = new MongoUrlBuilder
        {
            Server = new MongoServerAddress(host, mongoUrl.Server.Port),
            Username = mongoUrl.Username,
            Password = mongoUrl.Password,
            DatabaseName = mongoUrl.DatabaseName,
            AuthenticationSource = mongoUrl.AuthenticationSource ?? "admin"
        };

        return builder.ToString();
    }

    /// <summary>
    /// 判斷是否為 DocumentDB
    /// </summary>
    private bool IsDocumentDb(string connStr)
    {
        return connStr.Contains("docdb-elastic.amazonaws.com", StringComparison.OrdinalIgnoreCase) ||
               connStr.Contains("docdb.amazonaws.com", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// IDBHelper
/// </summary>
public interface IDBHelper
{
    /// <summary>
    /// 快速取得MongoDB資料庫
    /// </summary>
    IMongoDatabase GetMongoDatabase(string connectionString);
}