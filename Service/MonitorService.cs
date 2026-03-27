using System.Diagnostics;
using Tool.Enum;

namespace Tool.Service;

/// <summary>
/// 背景服務 - 監控
/// </summary>
public class MonitorService(IServiceProvider _serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        bool continueRunning = true;

        WalletTypeEnum? walletType = null;
        if(!string.IsNullOrEmpty(ConfigManager.Request.WalletType))
            walletType = (WalletTypeEnum)int.Parse(ConfigManager.Request.WalletType);

        while (continueRunning && !stoppingToken.IsCancellationRequested)
        {
            DisplayMenu();

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var summaryService = scope.ServiceProvider.GetRequiredService<ISummaryService>();
                        summaryService.CheckSummaryMinute();
                    }
                    break;
                case "2":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var summaryService = scope.ServiceProvider.GetRequiredService<ISummaryService>();
                        summaryService.CheckSummaryHourly();
                    }
                    break;
                case "3":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var summaryService = scope.ServiceProvider.GetRequiredService<ISummaryService>();
                        summaryService.CheckSummaryDaily();
                    }
                    break;
                case "4":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var summaryService = scope.ServiceProvider.GetRequiredService<ISummaryService>();
                        summaryService.CheckSummaryMonthly();
                    }
                    break;
                case "11":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var v1Service = scope.ServiceProvider.GetRequiredService<ISummaryCheckV1Service>();
                        v1Service.CheckSummaryMinuteV1();
                    }
                    break;
                case "12":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var v1Service = scope.ServiceProvider.GetRequiredService<ISummaryCheckV1Service>();
                        v1Service.CheckSummaryHourlyV1();
                    }
                    break;
                case "13":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var v1Service = scope.ServiceProvider.GetRequiredService<ISummaryCheckV1Service>();
                        v1Service.CheckSummaryDailyV1();
                    }
                    break;
                case "14":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var v1Service = scope.ServiceProvider.GetRequiredService<ISummaryCheckV1Service>();
                        v1Service.CheckSummaryMonthlyV1();
                    }
                    break;
                default:
                    Console.WriteLine("Invalid option or not yet implemented.");
                    break;
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadLine();
            Console.Clear();

            Console.WriteLine("Do you want to continue with other functions? (Y/N)");
            continueRunning = Console.ReadLine()?.ToUpper() == "Y";
        }

        await Task.CompletedTask;

        stopwatch.Stop();

        Console.ReadLine();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Menu
    /// </summary>
    private void DisplayMenu()
    {
        Console.Clear();
        Console.WriteLine("============================== Summary Check System ==============================");
        Console.WriteLine("1. 比對分鐘彙總     (Accounting vs SummaryMemberGameMinute vs SummaryOperatorMinute)");
        Console.WriteLine("2. 比對小時彙總     (Accounting vs SummaryMemberGameHourly vs SummaryOperatorHourly)");
        Console.WriteLine("3. 比對每日彙總     (Accounting vs SummaryMemberGameDaily  vs SummaryOperatorDaily)");
        Console.WriteLine("4. 比對每月彙總     (SummaryMemberGameDaily vs SummaryMemberGameMonthly | SummaryOperatorDaily vs SummaryOperatorMonthly)");
        Console.WriteLine("------------------------------ V1 比對 ------------------------------------------");
        Console.WriteLine("11. 比對分鐘彙總 V1  (SummaryMemberGameMinute  vs SummaryMemberGameMinute_v1  | SummaryOperatorMinute  vs SummaryOperatorMinute_v1)");
        Console.WriteLine("12. 比對小時彙總 V1  (SummaryMemberGameHourly  vs SummaryMemberGameHourly_v1  | SummaryOperatorHourly  vs SummaryOperatorHourly_v1)");
        Console.WriteLine("13. 比對每日彙總 V1  (SummaryMemberGameDaily   vs SummaryMemberGameDaily_v1   | SummaryOperatorDaily   vs SummaryOperatorDaily_v1)   [+0/+8/-4]");
        Console.WriteLine("14. 比對每月彙總 V1  (SummaryMemberGameMonthly vs SummaryMemberGameMonthly_v1 | SummaryOperatorMonthly vs SummaryOperatorMonthly_v1) [+0/+8/-4]");
        Console.WriteLine("==================================================================================");
        Console.Write("\nPlease select an operation (1-14): ");
    }
}
