using Tool.Helper;
using MongoDB.Driver;

namespace Tool.Extensions;

/// <summary>
/// 注入擴充
/// </summary>
public static class MongoCollectionExtension
{
    /// <summary>
    /// 注入Mongo服務
    /// </summary>
    public static IServiceCollection InjectionMongo(this IServiceCollection services)
    {
        var connStr = ConfigManager.ConnectionStrings?.AgentWarmMongoConnection;

        // 檢查連線字串是否為空
        if (string.IsNullOrEmpty(connStr))
        {
            Console.WriteLine("警告：MongoDB 連線字串未設定，MongoDB 相關功能將無法使用。");
            // 註冊 null 服務，避免依賴注入失敗
            services.AddScoped<IMongoDatabase>(s => null!);
            return services;
        }

        try
        {
            var mongoUrl = new MongoUrl(connStr);
            var settings = MongoClientSettings.FromUrl(mongoUrl);
            string databaseName = mongoUrl.DatabaseName;

            Console.WriteLine($"MongoDB 連線資訊：");
            Console.WriteLine($"  - Database: {databaseName}");
            Console.WriteLine($"  - Server: {mongoUrl.Server?.Host ?? "N/A"}:{mongoUrl.Server?.Port ?? 0}");
            Console.WriteLine($"  - Tunnel 啟用: {TunnelHelper.TunnelEnabled()}");

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
                Console.WriteLine($"  - 建立 SSH Tunnel: {firstServer.Host}:{firstServer.Port}");

                var tunnelPort = TunnelHelper.CreateTunnel(firstServer.Host, firstServer.Port);

                Console.WriteLine($"  - Tunnel Port: localhost:{tunnelPort}");

                // 因為 ReplicaSet 模式下其餘節點無法透過 localhost 連線
                // 所以只能在非 replicaSet 或 dev 環境中使用
                settings.Server = new MongoServerAddress("localhost", (int)tunnelPort);
                settings.ReplicaSetName = null; // 避免連線失敗
            }

            // 標準 MongoDB 設定
            settings.UseTls = false;
            settings.AllowInsecureTls = false;
            settings.RetryWrites = true;
            settings.ReadPreference = ReadPreference.SecondaryPreferred;
        }

            settings.MaxConnectionPoolSize = 500;
            settings.MinConnectionPoolSize = 50;
            settings.MaxConnecting = 10;

            var mongoClient = new MongoClient(settings);

            services.AddSingleton<IMongoClient>(mongoClient);
            services.AddScoped<IMongoDatabase>(s => s.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

            Console.WriteLine($"✓ MongoDB 服務註冊成功\n");
            return services;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"錯誤：無法初始化 MongoDB 連線 - {ex.Message}");
            Console.WriteLine("MongoDB 相關功能將無法使用。");
            // 註冊 null 服務，避免依賴注入失敗
            services.AddScoped<IMongoDatabase>(s => null!);
            return services;
        }
    }

    /// <summary>
    /// 判斷是否為 DocumentDB
    /// </summary>
    private static bool IsDocumentDb(string connStr)
    {
        return connStr.Contains("docdb-elastic.amazonaws.com", StringComparison.OrdinalIgnoreCase) ||
               connStr.Contains("docdb.amazonaws.com", StringComparison.OrdinalIgnoreCase);
    }
}