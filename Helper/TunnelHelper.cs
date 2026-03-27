using Renci.SshNet;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Text;

namespace Tool.Helper;

/// <summary>
/// SSH Tunnel 連線管理工具
/// </summary>
public class TunnelHelper
{
    private static ConcurrentDictionary<string, SshClient>? _clientMap;
    private static ConcurrentDictionary<string, uint>? _portMap;
    private static readonly object _lock = new object();

    /// <summary>
    /// 建立SSH Tunnel 連線
    /// </summary>
    /// <param name="tunnelHost">目標主機</param>
    /// <param name="tunnelPort">目標連接埠</param>
    /// <returns>本地綁定的連接埠</returns>
    public static uint CreateTunnel(string tunnelHost, int tunnelPort)
    {
        if (TunnelEnabled() == false)
            return (uint)tunnelPort;

        lock (_lock)
        {
            _clientMap ??= new ConcurrentDictionary<string, SshClient>();
            _portMap ??= new ConcurrentDictionary<string, uint>();

            // 建立唯一的 key 來識別這個 tunnel
            var tunnelKey = $"{tunnelHost}:{tunnelPort}";

            // 如果已經建立過相同的 tunnel，直接返回已綁定的 port
            if (_portMap.TryGetValue(tunnelKey, out var existingPort))
            {
                Console.WriteLine($"Reusing existing tunnel: {tunnelKey} -> localhost:{existingPort}");
                return existingPort;
            }

            var proxyHost = ConfigManager.DevelopmentSetting.TunnelSetting.ProxyHost;

            // 尋找可用的本地連接埠
            var boundPort = FindAvailablePort(tunnelPort);
            //Console.WriteLine($"Found available port: {boundPort} for {tunnelKey}");

            var client = _clientMap.GetOrAdd(proxyHost, host =>
            {
                try
                {
                    //Console.WriteLine($"Creating SSH client connection to {host}...");

                    // 載入橋接所需使用的PrivateKey File
                    var pemKeyPath = ConfigManager.DevelopmentSetting.TunnelSetting.PemKeyPath;

                    if (!File.Exists(pemKeyPath))
                    {
                        throw new FileNotFoundException($"PEM key file not found: {pemKeyPath}");
                    }

                    var pemKey = File.ReadAllText(pemKeyPath);
                    var privateKeyFile = new PrivateKeyFile(new MemoryStream(Encoding.UTF8.GetBytes(pemKey)));
                    var authMethod = new PrivateKeyAuthenticationMethod(
                        ConfigManager.DevelopmentSetting.TunnelSetting.ProxyUserName,
                        privateKeyFile);

                    var connectionInfo = new Renci.SshNet.ConnectionInfo(
                        ConfigManager.DevelopmentSetting.TunnelSetting.ProxyHost,
                        22,
                        ConfigManager.DevelopmentSetting.TunnelSetting.ProxyUserName,
                        authMethod);

                    var sshClient = new SshClient(connectionInfo);
                    sshClient.Connect();

                    //Console.WriteLine($"SSH client connected to {host}");
                    return sshClient;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create SSH client: {ex.Message}");
                    throw;
                }
            });

            // 建立 port forwarding
            if (client.IsConnected)
            {
                try
                {
                    var portForwarding = new ForwardedPortLocal("localhost", boundPort, tunnelHost, (uint)tunnelPort);

                    client.AddForwardedPort(portForwarding);
                    portForwarding.Start();

                    // 記錄映射關係
                    _portMap.TryAdd(tunnelKey, boundPort);

                    //Console.WriteLine($"✓ Tunnel created: {tunnelKey} -> localhost:{boundPort}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create port forwarding: {ex.Message}");
                    throw;
                }
            }
            else
            {
                throw new InvalidOperationException("SSH client is not connected");
            }

            return boundPort;
        }
    }

    /// <summary>
    /// 尋找可用的本地連接埠
    /// </summary>
    /// <param name="preferredPort">偏好的連接埠</param>
    /// <returns>可用的連接埠</returns>
    private static uint FindAvailablePort(int preferredPort)
    {
        // 先檢查偏好的 port 是否可用
        if (IsPortAvailable(preferredPort))
        {
            return (uint)preferredPort;
        }

        // 取得所有已使用的 port（包含系統和已建立的 tunnel）
        var usedPorts = GetUsedPorts().ToHashSet();

        // 從偏好的 port 開始往上找
        var startPort = preferredPort;
        var maxPort = 65535;

        for (int port = startPort; port <= maxPort; port++)
        {
            if (!usedPorts.Contains(port) && IsPortAvailable(port))
            {
                return (uint)port;
            }
        }

        throw new InvalidOperationException($"No available port found starting from {preferredPort}");
    }

    /// <summary>
    /// 檢查連接埠是否可用
    /// </summary>
    private static bool IsPortAvailable(int port)
    {
        try
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            // 檢查 TCP 連接埠
            var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();

            if (tcpListeners.Any(endpoint => endpoint.Port == port))
            {
                return false;
            }

            // 檢查 TCP 連線
            var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();

            if (tcpConnections.Any(conn => conn.LocalEndPoint.Port == port))
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 取得所有已使用的連接埠
    /// </summary>
    private static IEnumerable<int> GetUsedPorts()
    {
        var usedPorts = new List<int>();

        try
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            // 所有 TCP 監聽的 port
            usedPorts.AddRange(ipGlobalProperties.GetActiveTcpListeners().Select(e => e.Port));

            // 所有 TCP 連線的 port
            usedPorts.AddRange(ipGlobalProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint.Port));

            // 已建立的 tunnel 使用的 port
            if (_clientMap != null)
            {
                var tunnelPorts = _clientMap.Values
                                            .SelectMany(client => client.ForwardedPorts.Select(f => ((ForwardedPortLocal)f).BoundPort));
                usedPorts.AddRange(tunnelPorts.Select(p => (int)p));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting used ports: {ex.Message}");
        }

        return usedPorts.Distinct();
    }

    /// <summary>
    /// 是否開啟橋接模式
    /// </summary>
    /// <returns></returns>
    public static bool TunnelEnabled()
    {
        bool result = false;
        if (ConfigManager.DevelopmentSetting.TunnelSetting.EnableMongoForwarding)
        {
            if (string.IsNullOrEmpty(ConfigManager.DevelopmentSetting.TunnelSetting.ProxyHost) == false
                && string.IsNullOrEmpty(ConfigManager.DevelopmentSetting.TunnelSetting.ProxyUserName) == false
                && string.IsNullOrEmpty(ConfigManager.DevelopmentSetting.TunnelSetting.PemKeyPath) == false)
            {
                result = true;
            }
        }
        return result;
    }
}