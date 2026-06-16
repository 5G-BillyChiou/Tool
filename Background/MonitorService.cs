using System.Diagnostics;
using Tool.Enum;
using Tool.Service;

namespace Tool.Background;

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
                        var v1Service = scope.ServiceProvider.GetRequiredService<ISummaryBigQueryCheckService>();
                        v1Service.CheckSummaryBigQueryMinute();
                    }
                    break;
                case "12":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var v1Service = scope.ServiceProvider.GetRequiredService<ISummaryBigQueryCheckService>();
                        v1Service.CheckSummaryBigQueryHourly();
                    }
                    break;
                case "13":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var v1Service = scope.ServiceProvider.GetRequiredService<ISummaryBigQueryCheckService>();
                        v1Service.CheckSummaryBigQueryDaily();
                    }
                    break;
                case "14":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var v1Service = scope.ServiceProvider.GetRequiredService<ISummaryBigQueryCheckService>();
                        v1Service.CheckSummaryBigQueryMonthly();
                    }
                    break;
                //case "21":
                //    using (var scope = _serviceProvider.CreateScope())
                //    {
                //        var newMemberService = scope.ServiceProvider.GetRequiredService<ISummaryNewMemberCheckService>();
                //        newMemberService.CheckHourly();
                //    }
                //    break;
                //case "22":
                //    using (var scope = _serviceProvider.CreateScope())
                //    {
                //        var newMemberService = scope.ServiceProvider.GetRequiredService<ISummaryNewMemberCheckService>();
                //        newMemberService.CheckDaily();
                //    }
                //    break;
                //case "23":
                //    using (var scope = _serviceProvider.CreateScope())
                //    {
                //        var newMemberService = scope.ServiceProvider.GetRequiredService<ISummaryNewMemberCheckService>();
                //        newMemberService.CheckMonthly();
                //    }
                //    break;
                //case "30":
                //    using (var scope = _serviceProvider.CreateScope())
                //    {
                //        var ledgerService = scope.ServiceProvider.GetRequiredService<ILedgerService>();
                //        ledgerService.GetHasLederButNotLog();
                //    }
                //    break;
                //case "31":
                //    using (var scope = _serviceProvider.CreateScope())
                //    {
                //        var ledgerService = scope.ServiceProvider.GetRequiredService<ILedgerService>();
                //        ledgerService.CheckLog();
                //    }
                //    break;
                //case "32":
                //    using (var scope = _serviceProvider.CreateScope())
                //    {
                //        var ledgerService = scope.ServiceProvider.GetRequiredService<ILedgerService>();
                //        ledgerService.GetLog();
                //    }
                //    break;
                //case "20":
                //    using (var scope = _serviceProvider.CreateScope())
                //    {
                //        var memberTransferLogService = scope.ServiceProvider.GetRequiredService<IMemberTransferLogService>();
                //        memberTransferLogService.ExportTransferError();
                //    }
                //    break;
                //case "30":
                //    using (var scope = _serviceProvider.CreateScope())
                //    {
                //        var preAccountingService = scope.ServiceProvider.GetRequiredService<IPreAccountingService>();
                //        preAccountingService.CreateAccountingInsertSql();
                //    }
                //    break;
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
        //Console.WriteLine("------------------------------ 新增會員 比對 ------------------------------------");
        //Console.WriteLine("21. 比對小時新增會員  (Member.FristAccountAt vs SummaryOperatorNewMemberHourly)");
        //Console.WriteLine("22. 比對每日新增會員  (Member.FristAccountAt vs SummaryOperatorNewMemberDaily)  [+0/+8/-4]");
        //Console.WriteLine("23. 比對每月新增會員  (Member.FristAccountAt vs SummaryOperatorNewMemberMonthly) [+0/+8/-4]");
        //Console.WriteLine("==================================================================================");
        //Console.WriteLine("30. Ledger 與 TransferLog 比對");
        //Console.WriteLine("31. CheckLog（查詢 OpenObserve，需設定 appsettings OpenObserveSetting）");
        //Console.WriteLine("31. CheckLog（查詢 OpenObserve，需設定 appsettings OpenObserveSetting）");
        //Console.WriteLine("32. GetLog（查詢 OpenObserve，需設定 appsettings OpenObserveSetting）");
        Console.Write("\nPlease select an operation (1-32): ");
    }
}
