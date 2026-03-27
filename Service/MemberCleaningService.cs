using MongoDB.Bson;
using MongoDB.Driver;
using Tool.Enum;
using Tool.Helper;
using Tool.Model.Entity.FiveGame;
using Tool.Model.Entity.Mongo;
using Tool.Model.Entity.MySQL;
using Tool.Model.Repository.FiveGame;
using Tool.Model.Repository.Mongo;

namespace Tool.Service;


/// <summary>
/// 會員資料清理服務
/// </summary>
public class MemberCleaningService( IServiceProvider _serviceProvider,
                                    IDBHelper _dbBHelper,
                                    IMemberRepository _memberRepository,
                                    IMemberCleaningBackupRepository _memberCleaningBackupRepository,
                                    IOperatorRepository _operatorRepository) : IMemberCleaningService
{
    /// <summary>
    /// 查詢重複帳號
    /// </summary>
    public async Task GetAllDuplicateAccounts(WalletTypeEnum? walletType = null, string? operatorId = null)
    {
        var walletTypeText = walletType switch
        {
            WalletTypeEnum.WalletType_Single => "獨立錢包",
            WalletTypeEnum.WalletType_Shared => "共用錢包",
            _ => ""
        };

        var title = (walletType, string.IsNullOrEmpty(operatorId)) switch
        {
            (null, true) => "查詢所有營運商的重複帳號會員列表",
            (null, false) => $"查詢營運商 {operatorId} 的重複帳號會員列表",
            (not null, true) => $"查詢所有 {walletTypeText} 營運商的重複帳號會員列表",
            (not null, false) => $"查詢 {walletTypeText} 營運商 {operatorId} 的重複帳號會員列表"
        };
        Console.WriteLine($"\n===== {title} =====");
        Console.WriteLine("開始查詢...\n");

        // 查詢指定錢包 && 營運商的重複帳號會員資料
        var queryOperatorIds = new List<string>();
        if (walletType != null)
            queryOperatorIds = _operatorRepository.GetIdsByWalletType(walletType.Value);
        if (!string.IsNullOrEmpty(operatorId))
            queryOperatorIds.Add(operatorId);
        var duplicateMembers = _memberRepository.GetDuplicateAccountsByOperatorIds(queryOperatorIds);

        if (duplicateMembers.Count == 0)
        {
            Console.WriteLine("沒有找到重複的帳號。");
        }
        else
        {
            Console.WriteLine($"找到重複的會員資料：\n");
            Console.WriteLine($"{"營運商 ID",-40} {"帳號",-30} {"重複數量",10}");
            Console.WriteLine(new string('-', 105));

            foreach (var member in duplicateMembers)
            {
                Console.WriteLine($"{member.OperatorId,-40} {member.Account,-30} {member.Count,10}");
            }

            var groupCount = duplicateMembers.GroupBy(x => new { x.OperatorId, x.Account }).Count();
            Console.WriteLine(new string('-', 105));
            Console.WriteLine($"總計：{groupCount} 組重複帳號，共 {duplicateMembers.Count} 筆會員資料");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 查詢重複帳號詳細資料
    /// </summary>
    public async Task GetOperatorDuplicateAccounts(WalletTypeEnum? walletType = null, string? operatorId = null)
    {
        var walletTypeText = walletType switch
        {
            WalletTypeEnum.WalletType_Single => "獨立錢包",
            WalletTypeEnum.WalletType_Shared => "共用錢包",
            _ => ""
        };

        var title = (walletType, string.IsNullOrEmpty(operatorId)) switch
        {
            (null, true) => "查詢所有營運商的重複帳號會員列表",
            (null, false) => $"查詢營運商 {operatorId} 的重複帳號會員列表",
            (not null, true) => $"查詢所有 {walletTypeText} 營運商的重複帳號會員列表",
            (not null, false) => $"查詢 {walletTypeText} 營運商 {operatorId} 的重複帳號會員列表"
        };

        Console.WriteLine($"\n===== {title} =====");
        Console.WriteLine("開始查詢...\n");

        // 查詢指定錢包 && 營運商的重複帳號會員資料
        var queryOperatorIds = new List<string>();
        if (walletType != null)
            queryOperatorIds = _operatorRepository.GetIdsByWalletType(walletType.Value);
        if (!string.IsNullOrEmpty(operatorId))
            queryOperatorIds.Add(operatorId);

        var duplicateMembers = _memberRepository.GetDuplicateAccountsByOperatorIds(queryOperatorIds);

        if (duplicateMembers.Count == 0)
        {
            Console.WriteLine("沒有找到重複的帳號。");
            await Task.CompletedTask;
            return;
        }

        var groupCount = duplicateMembers.GroupBy(x => new { x.OperatorId, x.Account }).Count();
        Console.WriteLine($"找到 {groupCount} 組重複帳號");
        Console.WriteLine(new string('=', 150));

        // Warm DB
        var agentMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentWarmMongoConnection);
        var accountingRepository = new AccountingRepository(agentMongoDBContext);
        var memberWalletRepository = new MemberWalletRepository(agentMongoDBContext);

        // step1 查詢出Member資料並印出詳細資訊
        int processedGroups = 0;
        int totalMemberCount = 0;              // 總會員數
        int membersWithData = 0;               // 有資料的會員數
        int membersWithoutData = 0;            // 沒有資料的會員數
        long grandTotalAccounting = 0;          // 總 Accounting 筆數
        int grandTotalTransferLog = 0;         // 總 TransferLog 筆數
        int grandTotalMemberWallet = 0;        // 總 MemberWallet 筆數
        int duplicateAccountsWithBalance = 0;  // 有多個會員且錢包都有餘額的帳號組數

        var groupedMembers = duplicateMembers.GroupBy(x => new { x.OperatorId, x.Account });

        foreach (var group in groupedMembers)
        {
            processedGroups++;

            var membersList = _memberRepository.GetListByOperatorAndAccount(group.Key.OperatorId, group.Key.Account);

            //Console.WriteLine($"\n 營運商: {group.Key.OperatorId} | 帳號: {group.Key.Account} | 會員數: {membersList.Count}");
            Console.WriteLine($"[{processedGroups}/{groupCount}]{"會員 ID",-40} {"Accounting 筆數",20} {"TransferLog 筆數",20} {"MemberWallet Balance",20}");
            Console.WriteLine(new string('-', 150));

            long totalAccounting = 0;
            int totalTransferLog = 0;
            int totalMemberWallet = 0;
            var memberWallets = new List<long?>();

            // 使用平行查詢提升效能
            var tasks = membersList.Select(async member =>
            {
                // 為每個平行任務創建新的 scope，避免 DbContext 併發問題
                using var scope = _serviceProvider.CreateScope();
                var scopedTransferLogRepository = scope.ServiceProvider.GetRequiredService<IMemberTransferLogRepository>();

                // 平行執行三個查詢
                var accountingTask = accountingRepository.GetCountByOperatorIdAndMemberIdAsync(member.OperatorId, member.Id);
                var transferLogTask = scopedTransferLogRepository.GetCountByOperatorIdAndMemberIdAsync(member.OperatorId, member.Id);
                var memberWalletTask = memberWalletRepository.GetListByMemberIdAsync(member.Id);

                await Task.WhenAll(accountingTask, transferLogTask, memberWalletTask);

                return new
                {
                    Member = member,
                    AccountingCount = accountingTask.Result,
                    TransferLogCount = transferLogTask.Result,
                    MemberWallet = memberWalletTask.Result
                };
            }).ToList();

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                totalMemberCount++;

                var accountingCount = result.AccountingCount;
                var transferLogCount = result.TransferLogCount;
                var memberWallet = result.MemberWallet;
                var memberWalletCount = memberWallet != null ? 1 : 0;

                // 收集錢包餘額
                var walletBalance = memberWallet?.Sum(x => x.Balance);
                memberWallets.Add(walletBalance);

                Console.WriteLine($"{result.Member.Id,-40} {accountingCount,20} {transferLogCount,20} {walletBalance,20}");

                // 統計有資料/沒有資料的會員數
                if (accountingCount > 0 || transferLogCount > 0 || walletBalance > 0)
                    membersWithData++;
                else
                    membersWithoutData++;

                totalAccounting += accountingCount;
                totalTransferLog += transferLogCount;
                totalMemberWallet += memberWalletCount;
                grandTotalAccounting += accountingCount;
                grandTotalTransferLog += transferLogCount;
                grandTotalMemberWallet += memberWalletCount;
            }

            // 檢查：該組是否有多個會員且所有錢包都有餘額
            if (membersList.Count > 1 && memberWallets.All(balance => balance.HasValue && balance.Value > 0))
            {
                duplicateAccountsWithBalance++;
            }

            Console.WriteLine(new string('-', 150));
        }

        Console.WriteLine(new string('=', 150));
        Console.WriteLine($"\n處理完成！");
        Console.WriteLine($"  ● 共查詢 {processedGroups} 組重複帳號");
        Console.WriteLine($"  ● 有多個會員且錢包都有餘額的帳號組：{duplicateAccountsWithBalance} 組");
        Console.WriteLine($"  ● 總會員數：{totalMemberCount} 筆");
        Console.WriteLine($"  ● 有資料的會員：{membersWithData} 筆 (Accounting、TransferLog 至少有一筆資料 或 MemberWallet.Balance > 0 )");
        Console.WriteLine($"  ● 沒有資料的會員：{membersWithoutData} 筆 (Accounting、TransferLog 和 MemberWallet 都是 0)");
        Console.WriteLine($"  ● 總 Accounting 筆數：{grandTotalAccounting:N0}");
        Console.WriteLine($"  ● 總 TransferLog 筆數：{grandTotalTransferLog:N0}");
        Console.WriteLine($"  ● 總 MemberWallet 筆數：{grandTotalMemberWallet:N0}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 處理重複帳號 - 合併會員資料並刪除多餘的記錄
    /// </summary>
    public async Task ProcessOperatorDuplicateAccounts(bool mergeAccounting, WalletTypeEnum? walletType = null, string? operatorId = null)
    {
        // 創建日誌文件
        var logFileName = $"ProcessOperatorDuplicateAccounts_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", logFileName);

        // 確保 Logs 目錄存在
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        using var logWriter = new StreamWriter(logFilePath, false, System.Text.Encoding.UTF8);

        // 本地方法：同時寫入控制台和日誌文件
        void Log(string message)
        {
            Console.WriteLine(message);
            logWriter.WriteLine(message);
            logWriter.Flush(); // 確保即時寫入
        }

        var walletTypeText = walletType switch
        {
            WalletTypeEnum.WalletType_Single => "獨立錢包",
            WalletTypeEnum.WalletType_Shared => "共用錢包",
            _ => ""
        };

        var title = (walletType, string.IsNullOrEmpty(operatorId)) switch
        {
            (null, true) => "處理所有營運商的重複帳號會員",
            (null, false) => $"處理營運商 {operatorId} 的重複帳號會員",
            (not null, true) => $"處理所有 {walletTypeText} 營運商的重複帳號會員",
            (not null, false) => $"處理 {walletTypeText} 營運商 {operatorId} 的重複帳號會員"
        };

        Log($"\n===== {title} =====");
        Log($"開始處理時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"日誌文件: {logFilePath}");
        Log("開始處理...\n");

        // 查詢指定錢包 && 營運商的重複帳號會員資料
        var queryOperatorIds = new List<string>();
        if (walletType != null)
            queryOperatorIds = _operatorRepository.GetIdsByWalletType(walletType.Value);
        if (!string.IsNullOrEmpty(operatorId))
            queryOperatorIds.Add(operatorId);

        var duplicateMembers = _memberRepository.GetDuplicateAccountsByOperatorIds(queryOperatorIds);

        if (duplicateMembers.Count == 0)
        {
            Log("沒有找到重複的帳號。");
            await Task.CompletedTask;
            return;
        }

        var groupCount = duplicateMembers.GroupBy(x => new { x.OperatorId, x.Account }).Count();
        Log($"找到 {groupCount} 組重複帳號");
        Log(new string('=', 150));

        // 更新及備分說明：
        // - 更新：
        // -- 注單、轉帳紀錄、登入記錄、會員彙總 -> Warm DB
        // -- 錢包 -> Hot DB
        // - 備分：
        // --  會員資料 -> MySQL
        // --  錢包 -> Hot DB

        // Agent Warm DB -> New Change To Hot DB
        IMongoDatabase agentMongoDBContext;
        AccountingRepository accountingRepository;
        PreAccountingResultRepository preAccountingResultRepository;
        MemberLoginLogRepository memberLoginLogRepository;
        IMongoDatabase agentHotMongoDBContext;
        MemberWalletRepository memberWalletRepository;
        IMongoDatabase adminMongoDBContext;
        SummaryMemberGameRepository<SummaryMemberGameMinute> summaryMemberGameMinuteRepository;
        SummaryMemberGameRepository<SummaryMemberGameHourly> summaryMemberGameHourlyRepository;
        SummaryMemberGameRepository<SummaryMemberGameDaily> summaryMemberGameDailyRepository;
        SummaryMemberGameRepository<SummaryMemberGameMonthly> summaryMemberGameMonthlyRepository;

        try
        {
            agentMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentWarmMongoConnection);
            accountingRepository = new AccountingRepository(agentMongoDBContext);
            memberLoginLogRepository = new MemberLoginLogRepository(agentMongoDBContext);

            // Agent Hot DB
            agentHotMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentMongoConnection);
            memberWalletRepository = new MemberWalletRepository(agentHotMongoDBContext);
            preAccountingResultRepository = new PreAccountingResultRepository(agentHotMongoDBContext);

            // Admin MongoDB
            adminMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
            summaryMemberGameMinuteRepository = new SummaryMemberGameRepository<SummaryMemberGameMinute>(adminMongoDBContext);
            summaryMemberGameHourlyRepository = new SummaryMemberGameRepository<SummaryMemberGameHourly>(adminMongoDBContext);
            summaryMemberGameDailyRepository = new SummaryMemberGameRepository<SummaryMemberGameDaily>(adminMongoDBContext);
            summaryMemberGameMonthlyRepository = new SummaryMemberGameRepository<SummaryMemberGameMonthly>(adminMongoDBContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 初始化資料庫連接時發生錯誤: {ex.Message}");
            return;
        }

        int processedGroups = 0;
        int totalDeletedMembers = 0;
        int totalDeletedWallets = 0;
        int skippedGroups = 0;
        int onlineGroups = 0;
        int preAccountingGroups = 0;

        // 收集跳過的會員資料
        var skippedMemberGroups = new List<(string OperatorId, string Account, List<(Member Member, long AccountingCount, int TransferLogCount, long LoginLogCount, List<MemberWallet>? Wallet)> Members)>();

        // 收集有多筆 MemberWallet 的會員資料
        var membersWithMultipleWallets = new List<(string MemberId, List<MemberWallet> Wallets)>();

        foreach (var group in duplicateMembers)
        {
            processedGroups++;

            try
            {
                var membersList = _memberRepository.GetListByOperatorAndAccount(group.OperatorId, group.Account);
                var memberIds = membersList.Select(m => m.Id).ToList();

                // 檢查這個組中是否有任何會員在線上
                using (var scope = _serviceProvider.CreateScope())
                {
                    var memberSessionRepository = scope.ServiceProvider.GetRequiredService<IMemberSessionRepository>();
                    var hasOnlineMember = memberSessionRepository.Exists(memberIds);

                    if (hasOnlineMember)
                    {
                        Log($"[{processedGroups}/{groupCount}] ⚠ 警告：此組有會員在線上，跳過不處理！(營運商: {group.OperatorId}, 帳號: {group.Account})");
                        Log(new string('-', 100));
                        onlineGroups++;
                        continue;
                    }
                }

                // 檢查是否有未完成的注單
                var preAccountingCount = preAccountingResultRepository.GetCountByOperatorIdAndMemberIds(group.OperatorId, memberIds);
                if (preAccountingCount > 0)
                {
                    Log($"[{processedGroups}/{groupCount}] ⚠ 警告：此組有會員有未完成注單，跳過不處理！(營運商: {group.OperatorId}, 帳號: {group.Account})");
                    Log(new string('-', 100));
                    preAccountingGroups++;
                    continue;
                }

                Log($"[{processedGroups}/{groupCount}]{"會員 ID",-35} {"TotalBet",15} {"TransferLog",15} {"LoginLog",15} {"Balance",15} {"狀態",10}");
                Log(new string('-', 120));

                // 查詢每個會員的資料使用情況
                var tasks = membersList.Select(async member =>
                {
                    try
                    {
                        // 為每個平行任務創建新的 scope，避免 DbContext 併發問題
                        using var scope = _serviceProvider.CreateScope();
                        var scopedTransferLogRepository = scope.ServiceProvider.GetRequiredService<IMemberTransferLogRepository>();

                        // 平行執行四個查詢
                        var totalBetCountTask = summaryMemberGameMinuteRepository.GetBetCountByMemberIdAsync(member.Id);
                        var transferLogTask = scopedTransferLogRepository.GetCountByOperatorIdAndMemberIdAsync(member.OperatorId, member.Id);
                        var loginLogTask = memberLoginLogRepository.GetCountByOperatorIdAndMemberIdAsync(member.OperatorId, member.Id);
                        var memberWalletListTask = memberWalletRepository.GetListByMemberIdAsync(member.Id);

                        await Task.WhenAll(totalBetCountTask, transferLogTask, loginLogTask, memberWalletListTask);

                        return new MemberQueryResult
                        {
                            Member = member,
                            AccountingCount = totalBetCountTask.Result,
                            TransferLogCount = transferLogTask.Result,
                            LoginLogCount = loginLogTask.Result,
                            MemberWalletList = memberWalletListTask.Result
                        };
                    }
                    catch (Exception ex)
                    {
                        Log($"  ❌ 查詢會員 {member.Id} 資料時發生錯誤: {ex.Message}");
                        throw;
                    }
                }).ToList();

                var results = Array.Empty<MemberQueryResult>();

                try
                {
                    results = await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    // 任一失敗就會到這裡
                    Log("查詢過程中失敗");
                    throw;
                }

                // 收集會員資料狀態
                var memberDataList = new List<(Member Member, long AccountingCount, int TransferLogCount, long LoginLogCount, List<MemberWallet>? Wallet)>();

                foreach (var result in results)
                {
                    var accountingCount = result.AccountingCount;
                    var transferLogCount = result.TransferLogCount;
                    var loginLogCount = result.LoginLogCount;
                    var walletList = result.MemberWalletList;

                    memberDataList.Add((result.Member, accountingCount, transferLogCount, loginLogCount, walletList));

                    var walletBalance = walletList.Sum(x => x.Balance);
                    var hasData = accountingCount > 0 || transferLogCount > 0 || walletBalance > 0;
                    var walletCountInfo = walletList.Count > 1 ? $" ({walletList.Count}筆)" : "";
                    var status = hasData ? "有資料" : "無資料";

                    Log($"{result.Member.Id,-40} {accountingCount,15} {transferLogCount,15} {loginLogCount,15} {walletBalance,15}{walletCountInfo,-10} {status,10}");
                }

                // 檢查是否有多個會員都有 Accounting
                var membersWithAccounting = memberDataList.Where(x => x.AccountingCount > 0).ToList();
                bool shouldSkip = false;

                // 是否有注單資料 並且不允許合併 Accounting
                if (membersWithAccounting.Count > 1)
                {
                    if(mergeAccounting == false)
                    {
                        // 多個會員都有 Accounting，跳過不處理
                        Log($"⚠ 警告：有 {membersWithAccounting.Count} 個會員都有 Accounting 資料，跳過此組不處理！");
                        foreach (var member in membersWithAccounting)
                        {
                            Log($"  • 會員 {member.Member.Id}: 有 Accounting 資料");
                        }
                        shouldSkip = true;
                        skippedGroups++;

                        // 收集跳過的會員資料
                        skippedMemberGroups.Add((group.OperatorId, group.Account, Members: memberDataList));
                    }
                }

                if (shouldSkip)
                {
                    Log(new string('-', 100));
                    continue;
                }

                // 決定保留哪個會員：保留 CreatedAt 最早的
                var memberToKeep = memberDataList.OrderBy(x => x.Member.CreatedAt).First().Member;

                // 取得保留會員的保留錢包
                var memberWalletToKeep = memberDataList.Where(x => x.Member.Id == memberToKeep.Id)
                                                       .SelectMany(x => x.Wallet ?? new List<MemberWallet>())
                                                       .OrderBy(w => w.CreatedAt)
                                                       .FirstOrDefault();

                // 收集要刪除的會員
                var membersToDelete = memberDataList.Where(x => x.Member.Id != memberToKeep.Id).ToList();

                // 處理要刪除的會員：合併注單、轉帳記錄、錢包、刪除會員記錄
                if (membersToDelete.Any())
                {
                    int deletedMemberCount = 0;
                    int deletedWalletCount = 0;

                    // 步驟1: 合併 MemberLoginLog
                    Log($"\n→ 開始合併 MemberLoginLog 資料到保留的會員: {memberToKeep.Id}");
                    try
                    {
                        var loginLogMergeTasks = membersToDelete.Select(async memberToDelete =>
                        {
                            await memberLoginLogRepository.SetMemberIdByMemberIdAsync(memberToKeep.Id, memberToDelete.Member.Id);
                        }).ToList();

                        await Task.WhenAll(loginLogMergeTasks);
                        Log($"  ✓ MemberLoginLog 合併完成！\n");
                    }
                    catch (Exception ex)
                    {
                        Log($"  ❌ 合併 MemberLoginLog 時發生錯誤: {ex.Message}");
                        throw;
                    }

                    // 檢查是否需要合併注單和轉帳記錄
                    bool needMergeAccounting = mergeAccounting && membersToDelete.Any(x => x.AccountingCount > 0 || x.TransferLogCount > 0);

                    // 步驟2: 合併 Accounting 和 TransferLog（根據條件執行）
                    if (needMergeAccounting)
                    {
                        Log($"→ 開始合併 Accounting 和 TransferLog 資料到保留的會員: {memberToKeep.Id}");

                        try
                        {
                            // 建立所有合併任務並並行執行
                            var mergeTasks = membersToDelete.Select(async memberToDelete =>
                            {
                                try
                                {
                                    // 為每個查詢創建新的 scope 避免 DbContext 併發問題
                                    using var scope = _serviceProvider.CreateScope();
                                    var memberTransferLogRepository = scope.ServiceProvider.GetRequiredService<IMemberTransferLogRepository>();

                                    // 合併 Accounting 資料
                                    if (memberToDelete.AccountingCount > 0)
                                    {
                                        Log($"    → 合併會員 {memberToDelete.Member.Id} 的 Accounting 資料 (共 {memberToDelete.AccountingCount} 筆)");
                                        await accountingRepository.SetMemberIdByOperatorIdAndMemberIdAsync(group.OperatorId, memberToKeep.Id, memberToDelete.Member.Id);
                                        Log($"    ✓ 會員 {memberToDelete.Member.Id} 的 Accounting 合併成功");

                                        // 使用合併方法處理 Summary 資料（會檢查重複 key 並合併）
                                        Log($"    → 合併會員 {memberToDelete.Member.Id} 的 MemberGameMinute 資料");
                                        await summaryMemberGameMinuteRepository.MergeAndSetMemberIdByOperatorIdAndMemberIdAsync(group.OperatorId, memberToKeep.Id, memberToKeep.Account, memberToDelete.Member.Id);
                                        Log($"    ✓ 會員 {memberToDelete.Member.Id} 的 MemberGameMinute 合併成功");

                                        Log($"    → 合併會員 {memberToDelete.Member.Id} 的 MemberGameHourly 資料");
                                        await summaryMemberGameHourlyRepository.MergeAndSetMemberIdByOperatorIdAndMemberIdAsync(group.OperatorId, memberToKeep.Id, memberToKeep.Account, memberToDelete.Member.Id);
                                        Log($"    ✓ 會員 {memberToDelete.Member.Id} 的 MemberGameHourly 合併成功");

                                        Log($"    → 合併會員 {memberToDelete.Member.Id} 的 MemberGameDaily 資料");
                                        await summaryMemberGameDailyRepository.MergeAndSetMemberIdByOperatorIdAndMemberIdAsync(group.OperatorId, memberToKeep.Id, memberToKeep.Account, memberToDelete.Member.Id);
                                        Log($"    ✓ 會員 {memberToDelete.Member.Id} 的 MemberGameDaily 合併成功");

                                        Log($"    → 合併會員 {memberToDelete.Member.Id} 的 MemberGameMonthly 資料");
                                        await summaryMemberGameMonthlyRepository.MergeAndSetMemberIdByOperatorIdAndMemberIdAsync(group.OperatorId, memberToKeep.Id, memberToKeep.Account, memberToDelete.Member.Id);
                                        Log($"    ✓ 會員 {memberToDelete.Member.Id} 的 MemberGameMonthly 合併成功");
                                    }

                                    // 合併 TransferLog 資料
                                    if (memberToDelete.TransferLogCount > 0)
                                    {
                                        Log($"    → 合併會員 {memberToDelete.Member.Id} 的 TransferLog 資料 (共 {memberToDelete.TransferLogCount} 筆)");
                                        await memberTransferLogRepository.SetMemberIdByMemberIdAsync(memberToKeep.Id, memberToDelete.Member.Id);
                                        Log($"    ✓ 會員 {memberToDelete.Member.Id} 的 TransferLog 合併成功");
                                    }

                                    Log($"  ✓ 會員 {memberToDelete.Member.Id} 合併完成");
                                }
                                catch (Exception ex)
                                {
                                    Log($"  ❌ 會員 {memberToDelete.Member.Id} 合併失敗: {ex.Message}");
                                    throw;
                                }
                            }).ToList();

                            // 等待所有合併任務完成
                            await Task.WhenAll(mergeTasks);

                            Log($"  ✓ Accounting 和 TransferLog 合併完成！\n");
                        }
                        catch (Exception ex)
                        {
                            Log($"  ❌ 合併 Accounting 和 TransferLog 時發生錯誤: {ex.Message}");
                            throw;
                        }
                    }

                    // 步驟2: 備份原始會員資料
                    Log("\n→ 備份原始會員及錢包資料");
                    try
                    {
                        var allWallets = memberDataList.SelectMany(x => x.Wallet ?? new List<MemberWallet>()).ToList();
                        BackupMemberData(membersList, allWallets, agentHotMongoDBContext);
                    }
                    catch (Exception ex)
                    {
                        Log($"  ❌ 備份資料時發生錯誤: {ex.Message}");
                        throw;
                    }

                    // 步驟3: 收集所有要刪除的錢包（要刪除會員的錢包 + 保留會員的多餘錢包）
                    var memberWalletsEntitiesToDelete = new List<MemberWallet>();

                    // 步驟3-1: 收集要刪除會員的所有錢包
                    var deleteWallets = membersToDelete.SelectMany(x => x.Wallet ?? new List<MemberWallet>()).ToList();
                    memberWalletsEntitiesToDelete.AddRange(deleteWallets);

                    // 步驟3-2: 收集保留會員的多餘錢包（排除 memberWalletToKeep）
                    if (memberWalletToKeep != null)
                    {
                        var keepMemberOtherWallets = memberDataList
                            .Where(x => x.Member.Id == memberToKeep.Id)
                            .SelectMany(x => x.Wallet ?? new List<MemberWallet>())
                            .Where(w => w.Id != memberWalletToKeep.Id)
                            .ToList();
                        memberWalletsEntitiesToDelete.AddRange(keepMemberOtherWallets);
                    }

                    // 步驟3-3: 合併錢包餘額到保留會員的錢包
                    if (memberWalletsEntitiesToDelete.Any())
                    {
                        Log($"  → 共有 {memberWalletsEntitiesToDelete.Count} 筆錢包需要合併");

                        try
                        {
                            // 把要刪除的錢包餘額加到保留會員的錢包
                            var deletedWalletBalances = memberWalletsEntitiesToDelete.Sum(w => w.Balance);
                            if (deletedWalletBalances != 0)
                            {
                                var success = await memberWalletRepository.IncreaseBalanceWithRetry(memberToKeep.Id, deletedWalletBalances);
                                if (!success)
                                {
                                    Log($"  ❌ 增加錢包餘額失敗: MemberId={memberToKeep.Id}, Amount={deletedWalletBalances}");
                                    throw new Exception($"增加錢包餘額失敗: MemberId={memberToKeep.Id}, Amount={deletedWalletBalances}");
                                }
                            }

                            deletedWalletCount = memberWalletsEntitiesToDelete.Count;
                            totalDeletedWallets += deletedWalletCount;
                            await memberWalletRepository.DeleteAsync(memberWalletsEntitiesToDelete);
                        }
                        catch (Exception ex)
                        {
                            Log($"  ❌ 合併錢包或刪除錢包時發生錯誤: {ex.Message}");
                            throw;
                        }
                    }

                    // 步驟4: 準備要刪除的 Member
                    var memberEntitiesToDelete = membersToDelete.Select(x => x.Member).ToList();

                    // 刪除 MySQL Member 記錄
                    if (memberEntitiesToDelete.Any())
                    {
                        try
                        {
                            _memberRepository.Delete(memberEntitiesToDelete);
                            deletedMemberCount = memberEntitiesToDelete.Count;
                            totalDeletedMembers += deletedMemberCount;
                        }
                        catch (Exception ex)
                        {
                            Log($"  ❌ 刪除 Member 記錄時發生錯誤: {ex.Message}");
                            throw;
                        }
                    }

                    // 輸出刪除結果
                    Log($"  ✓ 刪除 Member:{deletedMemberCount} / Wallet:{deletedWalletCount}");

                    Log($"\n  刪除會員摘要:");
                    foreach (var item in membersToDelete)
                    {
                        var totalBalance = item.Wallet?.Sum(w => w.Balance) ?? 0;
                        Log($"  ✓ 會員: {item.Member.Id} (Accounting: {item.AccountingCount}, TransferLog: {item.TransferLogCount}, BalanceSum: {totalBalance})");
                    }
                }

                Log(new string('-', 100));
            }
            catch (Exception ex)
            {
                Log($"❌ 處理會員組時發生錯誤 (營運商: {group.OperatorId}, 帳號: {group.Account}): {ex.Message}");
                Log(new string('-', 100));
                throw;
            }
        }

        Log(new string('=', 150));
        Log($"\n處理完成時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"處理完成！");
        Log($"  ● 共處理 {processedGroups} 組重複帳號");
        Log($"  ● 跳過處理：{onlineGroups} 組 (在線中)");
        Log($"  ● 跳過處理：{preAccountingGroups} 組 (有未完成注單)");
        Log($"  ● 跳過處理：{skippedGroups} 組 (多個會員都有 AccountingCount)");
        Log($"  ● 成功處理：{processedGroups - skippedGroups - onlineGroups} 組");
        Log($"  ● 總刪除會員數：{totalDeletedMembers} 筆");
        Log($"  ● 總刪除錢包數：{totalDeletedWallets} 筆");

        // 輸出所有跳過的會員詳細資訊
        if (skippedMemberGroups.Any())
        {
            Log($"\n跳過處理的會員詳細資訊 (多個會員都有 AccountingCount):");
            Log(new string('=', 150));

            int skipIndex = 1;
            foreach (var skippedGroup in skippedMemberGroups)
            {
                Log($"\n[{skipIndex}/{skippedMemberGroups.Count}] 營運商: {skippedGroup.OperatorId}, 帳號: {skippedGroup.Account}");
                Log($"{"會員 ID",-40} {"TotalBet",15} {"TransferLog",15} {"Balance",15}");
                Log(new string('-', 100));

                foreach (var member in skippedGroup.Members)
                {
                    var totalBalance = member.Wallet?.Sum(w => w.Balance) ?? 0;
                    Log($"{member.Member.Id,-40} {member.AccountingCount,15} {member.TransferLogCount,15} {totalBalance,15}");
                }

                skipIndex++;
            }

            Log(new string('=', 150));
        }

        // 輸出有多筆 MemberWallet 的會員詳細資訊
        if (membersWithMultipleWallets.Any())
        {
            Log($"\n有多筆 MemberWallet 的會員詳細資訊:");
            Log(new string('=', 150));
            Log($"  ● 共有 {membersWithMultipleWallets.Count} 個會員有多筆錢包記錄\n");

            int walletIndex = 1;
            foreach (var memberWallet in membersWithMultipleWallets)
            {
                Log($"[{walletIndex}/{membersWithMultipleWallets.Count}] 會員 ID: {memberWallet.MemberId}");
                Log($"  → 錢包記錄數: {memberWallet.Wallets.Count} 筆");
                Log($"{"  錢包 ID",-45} {"Balance",15} {"UpdatedAt",25}");
                Log(new string('-', 100));

                foreach (var wallet in memberWallet.Wallets)
                {
                    Log($"  {wallet.Id,-45} {wallet.Balance,15} {wallet.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                }

                Log("");
                walletIndex++;
            }

            Log(new string('=', 150));
        }

        Log($"\n日誌文件已儲存至: {logFilePath}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 處理重複帳號 - 只處理完全沒有使用過的會員合併（無注單、無轉帳紀錄、無餘額）
    /// </summary>
    public async Task ProcessUnusedDuplicateAccounts(WalletTypeEnum? walletType = null, string? operatorId = null)
    {
        // 創建日誌文件
        var logFileName = $"ProcessUnusedDuplicateAccounts_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", logFileName);

        // 確保 Logs 目錄存在
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        using var logWriter = new StreamWriter(logFilePath, false, System.Text.Encoding.UTF8);

        // 本地方法：同時寫入控制台和日誌文件
        void Log(string message)
        {
            Console.WriteLine(message);
            logWriter.WriteLine(message);
            logWriter.Flush(); // 確保即時寫入
        }

        var walletTypeText = walletType switch
        {
            WalletTypeEnum.WalletType_Single => "獨立錢包",
            WalletTypeEnum.WalletType_Shared => "共用錢包",
            _ => ""
        };

        var title = (walletType, string.IsNullOrEmpty(operatorId)) switch
        {
            (null, true) => "處理所有營運商的未使用重複帳號會員",
            (null, false) => $"處理營運商 {operatorId} 的未使用重複帳號會員",
            (not null, true) => $"處理所有 {walletTypeText} 營運商的未使用重複帳號會員",
            (not null, false) => $"處理 {walletTypeText} 營運商 {operatorId} 的未使用重複帳號會員"
        };

        Log($"\n===== {title} =====");
        Log($"開始處理時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"日誌文件: {logFilePath}");
        Log("注意：此方法只處理完全沒有使用過的會員（無注單、無轉帳紀錄、無餘額）");
        Log("開始處理...\n");

        // 查詢指定錢包 && 營運商的重複帳號會員資料
        var queryOperatorIds = new List<string>();
        if (walletType != null)
            queryOperatorIds = _operatorRepository.GetIdsByWalletType(walletType.Value);
        if (!string.IsNullOrEmpty(operatorId))
            queryOperatorIds.Add(operatorId);

        var duplicateMembers = _memberRepository.GetDuplicateAccountsByOperatorIds(queryOperatorIds);

        if (duplicateMembers.Count == 0)
        {
            Log("沒有找到重複的帳號。");
            await Task.CompletedTask;
            return;
        }

        var groupCount = duplicateMembers.GroupBy(x => new { x.OperatorId, x.Account }).Count();
        Log($"找到 {groupCount} 組重複帳號");
        Log(new string('=', 150));

        IMongoDatabase agentMongoDBContext;
        IMongoDatabase adminMongoDBContext;
        SummaryMemberGameRepository<SummaryMemberGameMinute> summaryMemberGameMinuteRepository;
        PreAccountingResultRepository preAccountingResultRepository;
        MemberLoginLogRepository memberLoginLogRepository;
        IMongoDatabase agentHotMongoDBContext;
        MemberWalletRepository memberWalletRepository;

        try
        {
            // Agent Warm DB
            agentMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentWarmMongoConnection);
            memberLoginLogRepository = new MemberLoginLogRepository(agentMongoDBContext);

            // Admin MongoDB
            adminMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
            summaryMemberGameMinuteRepository = new SummaryMemberGameRepository<SummaryMemberGameMinute>(adminMongoDBContext);

            // Agent Hot DB
            agentHotMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentMongoConnection);
            memberWalletRepository = new MemberWalletRepository(agentHotMongoDBContext);
            preAccountingResultRepository = new PreAccountingResultRepository(agentHotMongoDBContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 初始化資料庫連接時發生錯誤: {ex.Message}");
            return;
        }

        int processedGroups = 0;
        int totalDeletedMembers = 0;
        int totalDeletedWallets = 0;
        int onlineGroups = 0;
        int preAccountingGroups = 0;
        int hasDataGroups = 0;  // 有任何使用資料的組數

        // 收集跳過的會員資料（有使用資料的組）
        var skippedMemberGroups = new List<(string OperatorId, string Account, List<(Member Member, long AccountingCount, int TransferLogCount, long LoginLogCount, List<MemberWallet>? Wallet)> Members)>();

        foreach (var group in duplicateMembers)
        {
            processedGroups++;

            try
            {
                var membersList = _memberRepository.GetListByOperatorAndAccount(group.OperatorId, group.Account);
                var memberIds = membersList.Select(m => m.Id).ToList();

                // 檢查這個組中是否有任何會員在線上
                using (var scope = _serviceProvider.CreateScope())
                {
                    var memberSessionRepository = scope.ServiceProvider.GetRequiredService<IMemberSessionRepository>();
                    var hasOnlineMember = memberSessionRepository.Exists(memberIds);

                    if (hasOnlineMember)
                    {
                        Log($"[{processedGroups}/{groupCount}] ⚠ 警告：此組有會員在線上，跳過不處理！(營運商: {group.OperatorId}, 帳號: {group.Account})");
                        Log(new string('-', 100));
                        onlineGroups++;
                        continue;
                    }
                }

                // 檢查是否有未完成的注單
                var preAccountingCount = preAccountingResultRepository.GetCountByOperatorIdAndMemberIds(group.OperatorId, memberIds);
                if (preAccountingCount > 0)
                {
                    Log($"[{processedGroups}/{groupCount}] ⚠ 警告：此組有會員有未完成注單，跳過不處理！(營運商: {group.OperatorId}, 帳號: {group.Account})");
                    Log(new string('-', 100));
                    preAccountingGroups++;
                    continue;
                }

                Log($"[{processedGroups}/{groupCount}]{"會員 ID",-35} {"TotalBet",15} {"TransferLog",15} {"LoginLog",15} {"Balance",15} {"狀態",10}");
                Log(new string('-', 120));

                // 查詢每個會員的資料使用情況
                var tasks = membersList.Select(async member =>
                {
                    try
                    {
                        // 為每個平行任務創建新的 scope，避免 DbContext 併發問題
                        using var scope = _serviceProvider.CreateScope();
                        var scopedTransferLogRepository = scope.ServiceProvider.GetRequiredService<IMemberTransferLogRepository>();

                        // 平行執行四個查詢
                        var totalBetCountTask = summaryMemberGameMinuteRepository.GetBetCountByMemberIdAsync(member.Id);
                        var transferLogTask = scopedTransferLogRepository.GetCountByOperatorIdAndMemberIdAsync(member.OperatorId, member.Id);
                        var loginLogTask = memberLoginLogRepository.GetCountByOperatorIdAndMemberIdAsync(member.OperatorId, member.Id);
                        var memberWalletListTask = memberWalletRepository.GetListByMemberIdAsync(member.Id);

                        await Task.WhenAll(totalBetCountTask, transferLogTask, loginLogTask, memberWalletListTask);

                        return new MemberQueryResult
                        {
                            Member = member,
                            AccountingCount = totalBetCountTask.Result,
                            TransferLogCount = transferLogTask.Result,
                            LoginLogCount = loginLogTask.Result,
                            MemberWalletList = memberWalletListTask.Result
                        };
                    }
                    catch (Exception ex)
                    {
                        Log($"  ❌ 查詢會員 {member.Id} 資料時發生錯誤: {ex.Message}");
                        throw;
                    }
                }).ToList();

                var results = Array.Empty<MemberQueryResult>();

                try
                {
                    results = await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    Log($"查詢過程中失敗: {ex.Message}");
                    throw;
                }

                // 收集會員資料狀態
                var memberDataList = new List<(Member Member, long AccountingCount, int TransferLogCount, long LoginLogCount, List<MemberWallet>? Wallet)>();

                foreach (var result in results)
                {
                    var accountingCount = result.AccountingCount;
                    var transferLogCount = result.TransferLogCount;
                    var loginLogCount = result.LoginLogCount;
                    var walletList = result.MemberWalletList;

                    memberDataList.Add((result.Member, accountingCount, transferLogCount, loginLogCount, walletList));

                    var walletBalance = walletList.Sum(x => x.Balance);
                    var hasData = accountingCount > 0 || transferLogCount > 0 || walletBalance > 0;
                    var walletCountInfo = walletList.Count > 1 ? $" ({walletList.Count}筆)" : "";
                    var status = hasData ? "有資料" : "無資料";

                    Log($"{result.Member.Id,-40} {accountingCount,15} {transferLogCount,15} {loginLogCount,15} {walletBalance,15}{walletCountInfo,-10} {status,10}");
                }

                // 檢查是否有任何會員有使用資料（Accounting、TransferLog、或餘額 > 0）
                var membersWithData = memberDataList.Where(x => x.AccountingCount > 0 || x.TransferLogCount > 0 || (x.Wallet != null && x.Wallet.Any(x => x.Balance > 0))).ToList();

                if (membersWithData.Count > 0)
                {
                    // 有任何會員有使用資料，跳過不處理
                    Log($"⚠ 跳過：此組有 {membersWithData.Count} 個會員有使用資料，不符合處理條件");
                    foreach (var member in membersWithData)
                    {
                        var balance = member.Wallet?.Sum(w => w.Balance) ?? 0;
                        Log($"  • 會員 {member.Member.Id}: Accounting={member.AccountingCount}, TransferLog={member.TransferLogCount}, Balance={balance}");
                    }
                    hasDataGroups++;

                    // 收集跳過的會員資料
                    skippedMemberGroups.Add((group.OperatorId, group.Account, Members: memberDataList));

                    Log(new string('-', 100));
                    continue;
                }

                // 所有會員都沒有使用資料，可以進行合併
                Log($"✓ 此組所有會員都沒有使用資料，開始合併處理");

                // 決定保留哪個會員：保留 CreatedAt 最早的
                var memberToKeep = memberDataList.OrderBy(x => x.Member.CreatedAt)
                                                 .ThenBy(x => x.Member.LastLoginAt)
                                                 .First()
                                                 .Member;

                // 取得保留會員的保留錢包
                var memberWalletToKeep = memberDataList.Where(x => x.Member.Id == memberToKeep.Id)
                                                       .SelectMany(x => x.Wallet ?? new List<MemberWallet>())
                                                       .OrderBy(w => w.CreatedAt)
                                                       .FirstOrDefault();

                // 收集要刪除的會員
                var membersToDelete = memberDataList.Where(x => x.Member.Id != memberToKeep.Id).ToList();

                // 處理要刪除的會員
                if (membersToDelete.Any())
                {
                    int deletedMemberCount = 0;
                    int deletedWalletCount = 0;

                    // 步驟1: 合併 MemberLoginLog
                    Log($"\n→ 開始合併 MemberLoginLog 資料到保留的會員: {memberToKeep.Id}");
                    try
                    {
                        var loginLogMergeTasks = membersToDelete.Select(async memberToDelete =>
                        {
                            await memberLoginLogRepository.SetMemberIdByMemberIdAsync(memberToKeep.Id, memberToDelete.Member.Id);
                        }).ToList();

                        await Task.WhenAll(loginLogMergeTasks);
                        Log($"  ✓ MemberLoginLog 合併完成！\n");
                    }
                    catch (Exception ex)
                    {
                        Log($"  ❌ 合併 MemberLoginLog 時發生錯誤: {ex.Message}");
                        throw;
                    }

                    // 步驟2: 備份原始會員資料
                    Log("\n→ 備份原始會員及錢包資料");
                    try
                    {
                        var allWallets = memberDataList.SelectMany(x => x.Wallet ?? new List<MemberWallet>()).ToList();
                        BackupMemberData(membersList, allWallets, agentHotMongoDBContext);
                    }
                    catch (Exception ex)
                    {
                        Log($"  ❌ 備份資料時發生錯誤: {ex.Message}");
                        throw;
                    }

                    // 步驟3: 收集所有要刪除的錢包（要刪除會員的錢包 + 保留會員的多餘錢包）
                    var memberWalletsEntitiesToDelete = new List<MemberWallet>();

                    // 步驟3-1: 收集要刪除會員的所有錢包
                    var deleteWallets = membersToDelete.SelectMany(x => x.Wallet ?? new List<MemberWallet>()).ToList();
                    memberWalletsEntitiesToDelete.AddRange(deleteWallets);

                    // 步驟3-2: 收集保留會員的多餘錢包（排除 memberWalletToKeep）
                    if (memberWalletToKeep != null)
                    {
                        var keepMemberOtherWallets = memberDataList
                            .Where(x => x.Member.Id == memberToKeep.Id)
                            .SelectMany(x => x.Wallet ?? new List<MemberWallet>())
                            .Where(w => w.Id != memberWalletToKeep.Id)
                            .ToList();
                        memberWalletsEntitiesToDelete.AddRange(keepMemberOtherWallets);
                    }

                    // 步驟3-3: 刪除錢包（因為都是無餘額的，不需要合併餘額）
                    if (memberWalletsEntitiesToDelete.Any())
                    {
                        Log($"  → 共有 {memberWalletsEntitiesToDelete.Count} 筆錢包需要刪除");

                        try
                        {
                            deletedWalletCount = memberWalletsEntitiesToDelete.Count;
                            totalDeletedWallets += deletedWalletCount;
                            await memberWalletRepository.DeleteAsync(memberWalletsEntitiesToDelete);
                        }
                        catch (Exception ex)
                        {
                            Log($"  ❌ 刪除錢包時發生錯誤: {ex.Message}");
                            throw;
                        }
                    }

                    // 步驟4: 準備要刪除的 Member
                    var memberEntitiesToDelete = membersToDelete.Select(x => x.Member).ToList();

                    // 刪除 MySQL Member 記錄
                    if (memberEntitiesToDelete.Any())
                    {
                        try
                        {
                            _memberRepository.Delete(memberEntitiesToDelete);
                            deletedMemberCount = memberEntitiesToDelete.Count;
                            totalDeletedMembers += deletedMemberCount;
                        }
                        catch (Exception ex)
                        {
                            Log($"  ❌ 刪除 Member 記錄時發生錯誤: {ex.Message}");
                            throw;
                        }
                    }

                    // 輸出刪除結果
                    Log($"  ✓ 刪除 Member:{deletedMemberCount} / Wallet:{deletedWalletCount}");

                    Log($"\n  刪除會員摘要:");
                    foreach (var item in membersToDelete)
                    {
                        Log($"  ✓ 會員: {item.Member.Id} (已刪除)");
                    }
                }

                Log(new string('-', 100));
            }
            catch (Exception ex)
            {
                Log($"❌ 處理會員組時發生錯誤 (營運商: {group.OperatorId}, 帳號: {group.Account}): {ex.Message}");
                Log(new string('-', 100));
                throw;
            }
        }

        Log(new string('=', 150));
        Log($"\n處理完成時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"處理完成！");
        Log($"  ● 共處理 {processedGroups} 組重複帳號");
        Log($"  ● 跳過處理：{onlineGroups} 組 (在線中)");
        Log($"  ● 跳過處理：{preAccountingGroups} 組 (有未完成注單)");
        Log($"  ● 跳過處理：{hasDataGroups} 組 (有會員有使用資料)");
        Log($"  ● 成功處理：{processedGroups - onlineGroups - preAccountingGroups - hasDataGroups} 組");
        Log($"  ● 總刪除會員數：{totalDeletedMembers} 筆");
        Log($"  ● 總刪除錢包數：{totalDeletedWallets} 筆");

        // 輸出所有跳過的會員詳細資訊（有使用資料的組）
        if (skippedMemberGroups.Any())
        {
            Log($"\n跳過處理的會員詳細資訊 (有會員有使用資料):");
            Log(new string('=', 150));

            int skipIndex = 1;
            foreach (var skippedGroup in skippedMemberGroups)
            {
                Log($"\n[{skipIndex}/{skippedMemberGroups.Count}] 營運商: {skippedGroup.OperatorId}, 帳號: {skippedGroup.Account}");
                Log($"{"會員 ID",-40} {"TotalBet",15} {"TransferLog",15} {"Wallent ID",20} {"Balance",15}");
                Log(new string('-', 100));

                foreach (var member in skippedGroup.Members)
                {
                    if(member.Wallet?.Count > 1)
                    {
                        foreach (var wallet in member.Wallet)
                        {
                            Log($"{member.Member.Id,-40} {member.AccountingCount,15} {member.TransferLogCount,15} {wallet.Id,20} {wallet.Balance,15}");
                        }
                    }
                    else
                    {
                        var wallet = member.Wallet?.FirstOrDefault();
                        Log($"{member.Member.Id,-40} {member.AccountingCount,15} {member.TransferLogCount,15} {wallet?.Id,20} {wallet?.Balance,15}");
                    }
                }

                skipIndex++;
            }

            Log(new string('=', 150));
        }

        Log($"\n日誌文件已儲存至: {logFilePath}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 產生處理重複帳號的 SQL/MongoDB 語法 - 只產生語法不執行（無注單、無轉帳紀錄、無餘額）
    /// </summary>
    public async Task GenerateUnusedDuplicateAccountsScript(WalletTypeEnum? walletType = null, string? operatorId = null)
    {
        // 創建日誌文件
        var logFileName = $"GenerateUnusedDuplicateAccountsScript_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", logFileName);

        // 確保 Logs 目錄存在
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        using var logWriter = new StreamWriter(logFilePath, false, System.Text.Encoding.UTF8);

        // 本地方法：同時寫入控制台和日誌文件
        void Log(string message)
        {
            Console.WriteLine(message);
            logWriter.WriteLine(message);
            logWriter.Flush(); // 確保即時寫入
        }

        var walletTypeText = walletType switch
        {
            WalletTypeEnum.WalletType_Single => "獨立錢包",
            WalletTypeEnum.WalletType_Shared => "共用錢包",
            _ => ""
        };

        var title = (walletType, string.IsNullOrEmpty(operatorId)) switch
        {
            (null, true) => "產生所有營運商的未使用重複帳號會員處理語法",
            (null, false) => $"產生營運商 {operatorId} 的未使用重複帳號會員處理語法",
            (not null, true) => $"產生所有 {walletTypeText} 營運商的未使用重複帳號會員處理語法",
            (not null, false) => $"產生 {walletTypeText} 營運商 {operatorId} 的未使用重複帳號會員處理語法"
        };

        Log($"\n===== {title} =====");
        Log($"開始處理時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"日誌文件: {logFilePath}");
        Log("注意：此方法只產生語法，不會執行任何資料異動");
        Log("注意：此方法只處理完全沒有使用過的會員（無注單、無轉帳紀錄、無餘額）");
        Log("開始處理...\n");

        // 查詢指定錢包 && 營運商的重複帳號會員資料
        var queryOperatorIds = new List<string>();
        if (walletType != null)
            queryOperatorIds = _operatorRepository.GetIdsByWalletType(walletType.Value);
        if (!string.IsNullOrEmpty(operatorId))
            queryOperatorIds.Add(operatorId);

        var duplicateMembers = _memberRepository.GetDuplicateAccountsByOperatorIds(queryOperatorIds);

        if (duplicateMembers.Count == 0)
        {
            Log("沒有找到重複的帳號。");
            await Task.CompletedTask;
            return;
        }

        var groupCount = duplicateMembers.GroupBy(x => new { x.OperatorId, x.Account }).Count();
        Log($"找到 {groupCount} 組重複帳號");
        Log(new string('=', 150));

        IMongoDatabase agentMongoDBContext;
        IMongoDatabase adminMongoDBContext;
        SummaryMemberGameRepository<SummaryMemberGameMinute> summaryMemberGameMinuteRepository;
        PreAccountingResultRepository preAccountingResultRepository;
        MemberLoginLogRepository memberLoginLogRepository;
        IMongoDatabase agentHotMongoDBContext;
        MemberWalletRepository memberWalletRepository;

        try
        {
            // Agent Warm DB
            agentMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentWarmMongoConnection);
            memberLoginLogRepository = new MemberLoginLogRepository(agentMongoDBContext);

            // Admin MongoDB
            adminMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
            summaryMemberGameMinuteRepository = new SummaryMemberGameRepository<SummaryMemberGameMinute>(adminMongoDBContext);

            // Agent Hot DB
            //agentHotMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentMongoConnection);
            agentHotMongoDBContext = agentMongoDBContext;
            memberWalletRepository = new MemberWalletRepository(agentHotMongoDBContext);
            preAccountingResultRepository = new PreAccountingResultRepository(agentHotMongoDBContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 初始化資料庫連接時發生錯誤: {ex.Message}");
            return;
        }

        int processedGroups = 0;
        int onlineGroups = 0;
        int preAccountingGroups = 0;
        int hasDataGroups = 0;  // 有任何使用資料的組數
        int canProcessGroups = 0; // 可以處理的組數
        int totalMemberCount = 0;
        int totalMemberWalletCount = 0;

        // 收集所有要刪除的 ID（合併成單一 IN 語法）
        var allMemberIdsToDelete = new List<string>();
        var allMemberIds = new List<string>();
        var allWalletIdsToDelete = new List<ObjectId>();
        var allLoginLogUpdateMappings = new List<(string OldMemberId, string NewMemberId)>();

        // 收集跳過的會員資料（有使用資料的組）
        var skippedMemberGroups = new List<(string OperatorId, string Account, List<(Member Member, long AccountingCount, int TransferLogCount, long LoginLogCount, List<MemberWallet>? Wallet)> Members)>();

        foreach (var group in duplicateMembers)
        {
            processedGroups++;

            try
            {
                var membersList = _memberRepository.GetListByOperatorAndAccount(group.OperatorId, group.Account);
                var memberIds = membersList.Select(m => m.Id).ToList();

                allMemberIds.AddRange(memberIds);

                // 檢查這個組中是否有任何會員在線上
                using (var scope = _serviceProvider.CreateScope())
                {
                    var memberSessionRepository = scope.ServiceProvider.GetRequiredService<IMemberSessionRepository>();
                    var hasOnlineMember = memberSessionRepository.Exists(memberIds);

                    if (hasOnlineMember)
                    {
                        Log($"[{processedGroups}/{groupCount}] ⚠ 警告：此組有會員在線上，跳過不處理！(營運商: {group.OperatorId}, 帳號: {group.Account})");
                        Log(new string('-', 100));
                        onlineGroups++;
                        continue;
                    }
                }

                // 檢查是否有未完成的注單
                var preAccountingCount = preAccountingResultRepository.GetCountByOperatorIdAndMemberIds(group.OperatorId, memberIds);
                if (preAccountingCount > 0)
                {
                    Log($"[{processedGroups}/{groupCount}] ⚠ 警告：此組有會員有未完成注單，跳過不處理！(營運商: {group.OperatorId}, 帳號: {group.Account})");
                    Log(new string('-', 100));
                    preAccountingGroups++;
                    continue;
                }

                Log($"[{processedGroups}/{groupCount}]{"會員 ID",-35} {"TotalBet",15} {"TransferLog",15} {"LoginLog",15} {"Balance",15} {"狀態",10}");
                Log(new string('-', 120));

                // 查詢每個會員的資料使用情況
                var tasks = membersList.Select(async member =>
                {
                    try
                    {
                        // 為每個平行任務創建新的 scope，避免 DbContext 併發問題
                        using var scope = _serviceProvider.CreateScope();
                        var scopedTransferLogRepository = scope.ServiceProvider.GetRequiredService<IMemberTransferLogRepository>();

                        // 平行執行四個查詢
                        var totalBetCountTask = summaryMemberGameMinuteRepository.GetBetCountByMemberIdAsync(member.Id);
                        var transferLogTask = scopedTransferLogRepository.GetCountByOperatorIdAndMemberIdAsync(member.OperatorId, member.Id);
                        var loginLogTask = memberLoginLogRepository.GetCountByOperatorIdAndMemberIdAsync(member.OperatorId, member.Id);
                        var memberWalletListTask = memberWalletRepository.GetListByMemberIdAsync(member.Id);

                        await Task.WhenAll(totalBetCountTask, transferLogTask, loginLogTask, memberWalletListTask);

                        return new MemberQueryResult
                        {
                            Member = member,
                            AccountingCount = totalBetCountTask.Result,
                            TransferLogCount = transferLogTask.Result,
                            LoginLogCount = loginLogTask.Result,
                            MemberWalletList = memberWalletListTask.Result
                        };
                    }
                    catch (Exception ex)
                    {
                        Log($"  ❌ 查詢會員 {member.Id} 資料時發生錯誤: {ex.Message}");
                        throw;
                    }
                }).ToList();

                var results = Array.Empty<MemberQueryResult>();

                try
                {
                    results = await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    Log($"查詢過程中失敗: {ex.Message}");
                    throw;
                }

                // 收集會員資料狀態
                var memberDataList = new List<(Member Member, long AccountingCount, int TransferLogCount, long LoginLogCount, List<MemberWallet>? Wallet)>();

                foreach (var result in results)
                {
                    var accountingCount = result.AccountingCount;
                    var transferLogCount = result.TransferLogCount;
                    var loginLogCount = result.LoginLogCount;
                    var walletList = result.MemberWalletList;

                    memberDataList.Add((result.Member, accountingCount, transferLogCount, loginLogCount, walletList));

                    var walletBalance = walletList.Sum(x => x.Balance);
                    var hasData = accountingCount > 0 || transferLogCount > 0 || walletBalance > 0;
                    var walletCountInfo = walletList.Count > 1 ? $" ({walletList.Count}筆)" : "";
                    var status = hasData ? "有資料" : "無資料";

                    Log($"{result.Member.Id,-40} {accountingCount,15} {transferLogCount,15} {loginLogCount,15} {walletBalance,15}{walletCountInfo,-10} {status,10}");
                }

                totalMemberCount += memberDataList.Count;
                totalMemberWalletCount += memberDataList.Sum(x => x.Wallet?.Count ?? 0);

                // 檢查是否有任何會員有使用資料（Accounting、TransferLog、或餘額 > 0）
                var membersWithData = memberDataList.Where(x => x.AccountingCount > 0 || x.TransferLogCount > 0 || (x.Wallet != null && x.Wallet.Any(x => x.Balance > 0))).ToList();

                if (membersWithData.Count > 0)
                {
                    // 有任何會員有使用資料，跳過不處理
                    Log($"⚠ 跳過：此組有 {membersWithData.Count} 個會員有使用資料，不符合處理條件");
                    foreach (var member in membersWithData)
                    {
                        var balance = member.Wallet?.Sum(w => w.Balance) ?? 0;
                        Log($"  • 會員 {member.Member.Id}: Accounting={member.AccountingCount}, TransferLog={member.TransferLogCount}, Balance={balance}");
                    }
                    hasDataGroups++;

                    // 收集跳過的會員資料
                    skippedMemberGroups.Add((group.OperatorId, group.Account, Members: memberDataList));

                    Log(new string('-', 100));
                    continue;
                }

                // 所有會員都沒有使用資料，可以進行合併
                Log($"✓ 此組所有會員都沒有使用資料，產生處理語法");
                canProcessGroups++;

                // 決定保留哪個會員：保留 CreatedAt 最早的
                var memberToKeep = memberDataList.OrderBy(x => x.Member.CreatedAt)
                                                 .ThenBy(x => x.Member.LastLoginAt)
                                                 .First()
                                                 .Member;

                // 取得保留會員的保留錢包
                var memberWalletToKeep = memberDataList.Where(x => x.Member.Id == memberToKeep.Id)
                                                       .SelectMany(x => x.Wallet ?? new List<MemberWallet>())
                                                       .OrderBy(w => w.CreatedAt)
                                                       .FirstOrDefault();

                // 收集要刪除的會員
                var membersToDelete = memberDataList.Where(x => x.Member.Id != memberToKeep.Id).ToList();

                // 處理要刪除的會員
                if (membersToDelete.Any())
                {
                    Log($"\n→ 保留會員: {memberToKeep.Id}");

                    // 收集 MemberLoginLog 更新映射
                    foreach (var memberToDelete in membersToDelete)
                    {
                        allLoginLogUpdateMappings.Add((memberToDelete.Member.Id, memberToKeep.Id));
                    }

                    // 收集所有要刪除的錢包
                    var memberWalletsEntitiesToDelete = new List<MemberWallet>();

                    // 收集要刪除會員的所有錢包
                    var deleteWallets = membersToDelete.SelectMany(x => x.Wallet ?? new List<MemberWallet>()).ToList();
                    memberWalletsEntitiesToDelete.AddRange(deleteWallets);

                    // 收集保留會員的多餘錢包（排除 memberWalletToKeep）
                    if (memberWalletToKeep != null)
                    {
                        var keepMemberOtherWallets = memberDataList
                            .Where(x => x.Member.Id == memberToKeep.Id)
                            .SelectMany(x => x.Wallet ?? new List<MemberWallet>())
                            .Where(w => w.Id != memberWalletToKeep.Id)
                            .ToList();
                        memberWalletsEntitiesToDelete.AddRange(keepMemberOtherWallets);
                    }

                    // 收集錢包 ID
                    if (memberWalletsEntitiesToDelete.Any())
                    {
                        allWalletIdsToDelete.AddRange(memberWalletsEntitiesToDelete.Select(w => w.Id));
                        Log($"  → 待刪除錢包: {memberWalletsEntitiesToDelete.Count} 筆");
                    }

                    // 收集 Member ID
                    var memberEntitiesToDelete = membersToDelete.Select(x => x.Member).ToList();
                    if (memberEntitiesToDelete.Any())
                    {
                        allMemberIdsToDelete.AddRange(memberEntitiesToDelete.Select(m => m.Id));
                    }

                    Log($"\n  刪除會員摘要:");
                    foreach (var item in membersToDelete)
                    {
                        Log($"  → 會員: {item.Member.Id} (待刪除)");
                    }
                }

                Log(new string('-', 100));
            }
            catch (Exception ex)
            {
                Log($"❌ 處理會員組時發生錯誤 (營運商: {group.OperatorId}, 帳號: {group.Account}): {ex.Message}");
                Log(new string('-', 100));
                throw;
            }
        }

        Log(new string('=', 150));
        Log($"\n處理完成時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"處理完成！");
        Log($"  ● 共處理 {processedGroups} 組重複帳號( 會員：{totalMemberCount} 錢包{totalMemberWalletCount} )");
        Log($"  ● 跳過處理：{onlineGroups} 組 (在線中)");
        Log($"  ● 跳過處理：{preAccountingGroups} 組 (有未完成注單)");
        Log($"  ● 跳過處理：{hasDataGroups} 組 (有會員有使用資料)");
        Log($"  ● 可處理組數：{canProcessGroups} 組");

        // 輸出所有跳過的會員詳細資訊（有使用資料的組）
        if (skippedMemberGroups.Any())
        {
            Log($"\n跳過處理的會員詳細資訊 (有會員有使用資料):");
            Log(new string('=', 150));

            int skipIndex = 1;
            foreach (var skippedGroup in skippedMemberGroups)
            {
                Log($"\n[{skipIndex}/{skippedMemberGroups.Count}] 營運商: {skippedGroup.OperatorId}, 帳號: {skippedGroup.Account}");
                Log($"{"會員 ID",-40} {"TotalBet",15} {"TransferLog",15} {"Wallent ID",20} {"Balance",15}");
                Log(new string('-', 100));

                foreach (var member in skippedGroup.Members)
                {
                    if(member.Wallet?.Count > 1)
                    {
                        foreach (var wallet in member.Wallet)
                        {
                            Log($"{member.Member.Id,-40} {member.AccountingCount,15} {member.TransferLogCount,15} {wallet.Id,20} {wallet.Balance,15}");
                        }
                    }
                    else
                    {
                        var wallet = member.Wallet?.FirstOrDefault();
                        Log($"{member.Member.Id,-40} {member.AccountingCount,15} {member.TransferLogCount,15} {wallet?.Id,20} {wallet?.Balance,15}");
                    }
                }

                skipIndex++;
            }

            Log(new string('=', 150));
        }

        // 輸出彙整的統計
        Log($"\n彙整統計:");
        Log($"  ● 待刪除 Member: {allMemberIdsToDelete.Count} 筆");
        Log($"  ● 待刪除 MemberWallet: {allWalletIdsToDelete.Count} 筆");
        Log($"  ● 待更新 MemberLoginLog: {allLoginLogUpdateMappings.Count} 筆");

        // 建立獨立的 SQL 腳本檔案
        if (allMemberIdsToDelete.Any() || allWalletIdsToDelete.Any() || allLoginLogUpdateMappings.Any() || allMemberIds.Any())
        {
            var scriptFileName = $"GenerateUnusedDuplicateAccountsScript_{DateTime.Now:yyyyMMdd_HHmmss}_SQL.txt";
            var scriptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", scriptFileName);

            using var scriptWriter = new StreamWriter(scriptFilePath, false, System.Text.Encoding.UTF8);

            scriptWriter.WriteLine("========================================");
            scriptWriter.WriteLine("  自動產生的 SQL/MongoDB 執行語法");
            scriptWriter.WriteLine($"  產生時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            scriptWriter.WriteLine("========================================");
            scriptWriter.WriteLine();

            // MongoDB - MemberLoginLog 更新語法 (Agent Warm DB)
            if (allLoginLogUpdateMappings.Any())
            {
                scriptWriter.WriteLine("// ========== MongoDB 語法 (Agent Warm DB - MemberLoginLog) ==========");
                scriptWriter.WriteLine("// 請在 MongoDB Shell 或 Compass 中執行以下語法:");
                scriptWriter.WriteLine();
                foreach (var mapping in allLoginLogUpdateMappings)
                {
                    scriptWriter.WriteLine($"db.member_login_log.updateMany({{ \"member_id\": \"{mapping.OldMemberId}\" }}, {{ $set: {{ \"member_id\": \"{mapping.NewMemberId}\" }} }});");
                }
                scriptWriter.WriteLine();
            }

            // MongoDB - MemberWallet 刪除語法 (Agent Hot DB)
            if (allWalletIdsToDelete.Any())
            {
                scriptWriter.WriteLine("// ========== MongoDB 語法 (Agent Hot DB - MemberWallet) ==========");
                scriptWriter.WriteLine("// 請在 MongoDB Shell 或 Compass 中執行以下語法:");
                scriptWriter.WriteLine($"// 共 {allWalletIdsToDelete.Count} 筆待刪除");
                scriptWriter.WriteLine();
                var walletIdsFormatted = string.Join(", ", allWalletIdsToDelete.Select(id => $"ObjectId(\"{id}\")"));
                scriptWriter.WriteLine($"db.member_wallet.deleteMany({{ \"updated_at\": null, \"balance\": 0, \"_id\": {{ $in: [{walletIdsFormatted}] }} }});");
                scriptWriter.WriteLine();
            }

            // MySQL - Member 刪除語法
            if (allMemberIdsToDelete.Any())
            {
                var cutoffTimestamp = DateTimeOffset.UtcNow;

                scriptWriter.WriteLine("-- ========== MySQL 語法 (Member) ==========");
                scriptWriter.WriteLine("-- 請在 MySQL 中執行以下語法:");
                scriptWriter.WriteLine($"-- 共 {allMemberIdsToDelete.Count} 筆待刪除");
                scriptWriter.WriteLine();
                var memberIdsFormatted = string.Join(", ", allMemberIdsToDelete.Select(id => $"'{id}'"));
                scriptWriter.WriteLine($"DELETE FROM `member` WHERE `last_login_at` < {cutoffTimestamp} AND `frist_account_at` IS NULL AND `id` IN ({memberIdsFormatted});");
                scriptWriter.WriteLine();
            }

            // MySQL - MemberIds
            if (allMemberIds.Any())
            {
                var cutoffTimestamp = DateTimeOffset.UtcNow;

                scriptWriter.WriteLine("-- ========== MemberIds ==========");
                scriptWriter.WriteLine($"-- 共 {allMemberIds.Count} 筆");
                scriptWriter.WriteLine();
                var memberIdsFormatted = string.Join(", ", allMemberIds.Select(id => $"'{id}'"));
                scriptWriter.WriteLine($"{memberIdsFormatted}");
                scriptWriter.WriteLine();
            }

            scriptWriter.Flush();
            Log($"\n語法檔案已儲存至: {scriptFilePath}");
        }

        Log($"\n日誌文件已儲存至: {logFilePath}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 產生處理重複帳號的更新語法 - 只產生語法不執行（全部合併不跳過任何情況）
    /// </summary>
    public async Task GenerateProcessUnusedDuplicateAccountsUpdateScript(WalletTypeEnum? walletType = null, string? operatorId = null)
    {
        // 創建日誌文件
        var logFileName = $"GenerateProcessUnusedDuplicateAccountsUpdateScript_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", logFileName);

        // 確保 Logs 目錄存在
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        using var logWriter = new StreamWriter(logFilePath, false, System.Text.Encoding.UTF8);

        // 本地方法：同時寫入控制台和日誌文件
        void Log(string message)
        {
            Console.WriteLine(message);
            logWriter.WriteLine(message);
            logWriter.Flush(); // 確保即時寫入
        }

        var walletTypeText = walletType switch
        {
            WalletTypeEnum.WalletType_Single => "獨立錢包",
            WalletTypeEnum.WalletType_Shared => "共用錢包",
            _ => ""
        };

        var title = (walletType, string.IsNullOrEmpty(operatorId)) switch
        {
            (null, true) => "產生所有營運商的重複帳號會員更新語法",
            (null, false) => $"產生營運商 {operatorId} 的重複帳號會員更新語法",
            (not null, true) => $"產生所有 {walletTypeText} 營運商的重複帳號會員更新語法",
            (not null, false) => $"產生 {walletTypeText} 營運商 {operatorId} 的重複帳號會員更新語法"
        };

        Log($"\n===== {title} =====");
        Log($"開始處理時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"日誌文件: {logFilePath}");
        Log("注意：此方法只產生語法，不會執行任何資料異動");
        Log("注意：此方法會對所有重複帳號產生合併語法（不跳過任何情況）");
        Log("開始處理...\n");

        // 查詢指定錢包 && 營運商的重複帳號會員資料
        var queryOperatorIds = new List<string>();
        if (walletType != null)
            queryOperatorIds = _operatorRepository.GetIdsByWalletType(walletType.Value);
        if (!string.IsNullOrEmpty(operatorId))
            queryOperatorIds.Add(operatorId);

        var duplicateMembers = _memberRepository.GetDuplicateAccountsByOperatorIds(queryOperatorIds);

        if (duplicateMembers.Count == 0)
        {
            Log("沒有找到重複的帳號。");
            await Task.CompletedTask;
            return;
        }

        var groupCount = duplicateMembers.GroupBy(x => new { x.OperatorId, x.Account }).Count();
        Log($"找到 {groupCount} 組重複帳號");
        Log(new string('=', 150));

        IMongoDatabase agentMongoDBContext;
        IMongoDatabase adminMongoDBContext;
        SummaryMemberGameRepository<SummaryMemberGameMinute> summaryMemberGameMinuteRepository;
        MemberLoginLogRepository memberLoginLogRepository;
        IMongoDatabase agentHotMongoDBContext;
        MemberWalletRepository memberWalletRepository;

        try
        {
            // Agent Warm DB
            agentMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentWarmMongoConnection);
            memberLoginLogRepository = new MemberLoginLogRepository(agentMongoDBContext);

            // Admin MongoDB
            adminMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AdminMongoConnection);
            summaryMemberGameMinuteRepository = new SummaryMemberGameRepository<SummaryMemberGameMinute>(adminMongoDBContext);

            // Agent Hot DB
            agentHotMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentMongoConnection);
            memberWalletRepository = new MemberWalletRepository(agentHotMongoDBContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 初始化資料庫連接時發生錯誤: {ex.Message}");
            return;
        }

        int processedGroups = 0;

        // 收集所有要更新/刪除的資料
        var allMemberIdsToDelete = new List<string>();
        var allWalletIdsToDelete = new List<ObjectId>();
        var allLoginLogUpdateMappings = new List<(string OldMemberId, string NewMemberId)>();

        // 收集注單和匯總資料更新映射
        var allAccountingUpdateMappings = new List<(string OperatorId, string OldMemberId, string NewMemberId)>();
        var allTransferLogUpdateMappings = new List<(string OldMemberId, string NewMemberId)>();

        foreach (var group in duplicateMembers)
        {
            processedGroups++;

            try
            {
                var membersList = _memberRepository.GetListByOperatorAndAccount(group.OperatorId, group.Account);
                var memberIds = membersList.Select(m => m.Id).ToList();

                Log($"[{processedGroups}/{groupCount}]{"會員 ID",-35} {"TotalBet",15} {"TransferLog",15} {"LoginLog",15} {"Balance",15} {"狀態",10}");
                Log(new string('-', 120));

                // 查詢每個會員的資料使用情況
                var tasks = membersList.Select(async member =>
                {
                    try
                    {
                        // 為每個平行任務創建新的 scope，避免 DbContext 併發問題
                        using var scope = _serviceProvider.CreateScope();
                        var scopedTransferLogRepository = scope.ServiceProvider.GetRequiredService<IMemberTransferLogRepository>();

                        // 平行執行四個查詢
                        var totalBetCountTask = summaryMemberGameMinuteRepository.GetBetCountByMemberIdAsync(member.Id);
                        var transferLogTask = scopedTransferLogRepository.GetCountByOperatorIdAndMemberIdAsync(member.OperatorId, member.Id);
                        var loginLogTask = memberLoginLogRepository.GetCountByOperatorIdAndMemberIdAsync(member.OperatorId, member.Id);
                        var memberWalletListTask = memberWalletRepository.GetListByMemberIdAsync(member.Id);

                        await Task.WhenAll(totalBetCountTask, transferLogTask, loginLogTask, memberWalletListTask);

                        return new MemberQueryResult
                        {
                            Member = member,
                            AccountingCount = totalBetCountTask.Result,
                            TransferLogCount = transferLogTask.Result,
                            LoginLogCount = loginLogTask.Result,
                            MemberWalletList = memberWalletListTask.Result
                        };
                    }
                    catch (Exception ex)
                    {
                        Log($"  ❌ 查詢會員 {member.Id} 資料時發生錯誤: {ex.Message}");
                        throw;
                    }
                }).ToList();

                var results = Array.Empty<MemberQueryResult>();

                try
                {
                    results = await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    Log($"查詢過程中失敗: {ex.Message}");
                    throw;
                }

                // 收集會員資料狀態
                var memberDataList = new List<(Member Member, long AccountingCount, int TransferLogCount, long LoginLogCount, List<MemberWallet>? Wallet)>();

                foreach (var result in results)
                {
                    var accountingCount = result.AccountingCount;
                    var transferLogCount = result.TransferLogCount;
                    var loginLogCount = result.LoginLogCount;
                    var walletList = result.MemberWalletList;

                    memberDataList.Add((result.Member, accountingCount, transferLogCount, loginLogCount, walletList));

                    var walletBalance = walletList.Sum(x => x.Balance);
                    var hasData = accountingCount > 0 || transferLogCount > 0 || walletBalance > 0;
                    var walletCountInfo = walletList.Count > 1 ? $" ({walletList.Count}筆)" : "";
                    var status = hasData ? "有資料" : "無資料";

                    Log($"{result.Member.Id,-40} {accountingCount,15} {transferLogCount,15} {loginLogCount,15} {walletBalance,15}{walletCountInfo,-10} {status,10}");
                }

                // 產生合併語法（不跳過任何情況）
                Log($"✓ 產生此組的合併處理語法");

                // 決定保留哪個會員：保留 CreatedAt 最早的
                var memberToKeep = memberDataList.OrderBy(x => x.Member.CreatedAt)
                                                 .ThenBy(x => x.Member.LastLoginAt)
                                                 .First()
                                                 .Member;

                // 取得保留會員的保留錢包
                var memberWalletToKeep = memberDataList.Where(x => x.Member.Id == memberToKeep.Id)
                                                       .SelectMany(x => x.Wallet ?? new List<MemberWallet>())
                                                       .OrderBy(w => w.CreatedAt)
                                                       .FirstOrDefault();

                // 收集要刪除的會員
                var membersToDelete = memberDataList.Where(x => x.Member.Id != memberToKeep.Id).ToList();

                // 處理要刪除的會員
                if (membersToDelete.Any())
                {
                    Log($"\n→ 保留會員: {memberToKeep.Id}");

                    // 收集各種更新映射
                    foreach (var memberToDelete in membersToDelete)
                    {
                        // 收集 MemberLoginLog 更新映射
                        allLoginLogUpdateMappings.Add((memberToDelete.Member.Id, memberToKeep.Id));

                        // 收集 Accounting 和匯總資料更新映射（有注單資料的會員）
                        if (memberToDelete.AccountingCount > 0)
                        {
                            allAccountingUpdateMappings.Add((group.OperatorId, memberToDelete.Member.Id, memberToKeep.Id));
                        }

                        // 收集 TransferLog 更新映射（有轉帳記錄的會員）
                        if (memberToDelete.TransferLogCount > 0)
                        {
                            allTransferLogUpdateMappings.Add((memberToDelete.Member.Id, memberToKeep.Id));
                        }
                    }

                    // 收集所有要刪除的錢包
                    var memberWalletsEntitiesToDelete = new List<MemberWallet>();

                    // 收集要刪除會員的所有錢包
                    var deleteWallets = membersToDelete.SelectMany(x => x.Wallet ?? new List<MemberWallet>()).ToList();
                    memberWalletsEntitiesToDelete.AddRange(deleteWallets);

                    // 收集保留會員的多餘錢包（排除 memberWalletToKeep）
                    if (memberWalletToKeep != null)
                    {
                        var keepMemberOtherWallets = memberDataList
                            .Where(x => x.Member.Id == memberToKeep.Id)
                            .SelectMany(x => x.Wallet ?? new List<MemberWallet>())
                            .Where(w => w.Id != memberWalletToKeep.Id)
                            .ToList();
                        memberWalletsEntitiesToDelete.AddRange(keepMemberOtherWallets);
                    }

                    // 收集錢包 ID
                    if (memberWalletsEntitiesToDelete.Any())
                    {
                        allWalletIdsToDelete.AddRange(memberWalletsEntitiesToDelete.Select(w => w.Id));
                        Log($"  → 待刪除錢包: {memberWalletsEntitiesToDelete.Count} 筆");
                    }

                    // 收集 Member ID
                    var memberEntitiesToDelete = membersToDelete.Select(x => x.Member).ToList();
                    if (memberEntitiesToDelete.Any())
                    {
                        allMemberIdsToDelete.AddRange(memberEntitiesToDelete.Select(m => m.Id));
                    }

                    Log($"\n  刪除會員摘要:");
                    foreach (var item in membersToDelete)
                    {
                        Log($"  → 會員: {item.Member.Id} (待刪除)");
                    }
                }

                Log(new string('-', 100));
            }
            catch (Exception ex)
            {
                Log($"❌ 處理會員組時發生錯誤 (營運商: {group.OperatorId}, 帳號: {group.Account}): {ex.Message}");
                Log(new string('-', 100));
                throw;
            }
        }

        Log(new string('=', 150));
        Log($"\n處理完成時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"處理完成！");
        Log($"  ● 共處理 {processedGroups} 組重複帳號");

        // 輸出彙整的統計
        Log($"\n彙整統計:");
        Log($"  ● 待刪除 Member: {allMemberIdsToDelete.Count} 筆");
        Log($"  ● 待刪除 MemberWallet: {allWalletIdsToDelete.Count} 筆");
        Log($"  ● 待更新 MemberLoginLog: {allLoginLogUpdateMappings.Count} 筆");
        Log($"  ● 待更新 Accounting/Summary: {allAccountingUpdateMappings.Count} 筆");
        Log($"  ● 待更新 TransferLog: {allTransferLogUpdateMappings.Count} 筆");

        // 建立獨立的 SQL 腳本檔案
        if (allMemberIdsToDelete.Any() || allWalletIdsToDelete.Any() || allLoginLogUpdateMappings.Any() || allAccountingUpdateMappings.Any() || allTransferLogUpdateMappings.Any())
        {
            var scriptFileName = $"GenerateProcessUnusedDuplicateAccountsUpdateScript_{DateTime.Now:yyyyMMdd_HHmmss}_SQL.txt";
            var scriptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", scriptFileName);

            using var scriptWriter = new StreamWriter(scriptFilePath, false, System.Text.Encoding.UTF8);

            scriptWriter.WriteLine("========================================");
            scriptWriter.WriteLine("  自動產生的更新語法 (全部合併不跳過)");
            scriptWriter.WriteLine($"  產生時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            scriptWriter.WriteLine("========================================");
            scriptWriter.WriteLine();

            // MongoDB - Accounting 更新語法 (Agent Warm DB)
            if (allAccountingUpdateMappings.Any())
            {
                scriptWriter.WriteLine("// ========== MongoDB 語法 (Agent Warm DB - Accounting) ==========");
                scriptWriter.WriteLine("// 合併 Accounting 注單資料到保留的會員");
                scriptWriter.WriteLine($"// 共 {allAccountingUpdateMappings.Count} 筆待更新");
                scriptWriter.WriteLine();
                foreach (var mapping in allAccountingUpdateMappings)
                {
                    scriptWriter.WriteLine($"db.accounting.updateMany({{ \"operator_id\": \"{mapping.OperatorId}\", \"member_id\": \"{mapping.OldMemberId}\" }}, {{ $set: {{ \"member_id\": \"{mapping.NewMemberId}\" }} }});");
                }
                scriptWriter.WriteLine();
            }

            // MongoDB - SummaryMemberGame 更新語法 (Admin MongoDB)
            if (allAccountingUpdateMappings.Any())
            {
                scriptWriter.WriteLine("// ========== MongoDB 語法 (Admin MongoDB - SummaryMemberGame) ==========");
                scriptWriter.WriteLine("// 合併會員彙總資料到保留的會員");
                scriptWriter.WriteLine($"// 共 {allAccountingUpdateMappings.Count} 筆待更新 (每筆包含 Minute/Hourly/Daily/Monthly)");
                scriptWriter.WriteLine();

                // SummaryMemberGameMinute
                scriptWriter.WriteLine("// --- SummaryMemberGameMinute ---");
                foreach (var mapping in allAccountingUpdateMappings)
                {
                    scriptWriter.WriteLine($"db.summary_member_game_minute.updateMany({{ \"operator_id\": \"{mapping.OperatorId}\", \"member_id\": \"{mapping.OldMemberId}\" }}, {{ $set: {{ \"member_id\": \"{mapping.NewMemberId}\" }} }});");
                }
                scriptWriter.WriteLine();

                // SummaryMemberGameHourly
                scriptWriter.WriteLine("// --- SummaryMemberGameHourly ---");
                foreach (var mapping in allAccountingUpdateMappings)
                {
                    scriptWriter.WriteLine($"db.summary_member_game_hourly.updateMany({{ \"operator_id\": \"{mapping.OperatorId}\", \"member_id\": \"{mapping.OldMemberId}\" }}, {{ $set: {{ \"member_id\": \"{mapping.NewMemberId}\" }} }});");
                }
                scriptWriter.WriteLine();

                // SummaryMemberGameDaily
                scriptWriter.WriteLine("// --- SummaryMemberGameDaily ---");
                foreach (var mapping in allAccountingUpdateMappings)
                {
                    scriptWriter.WriteLine($"db.summary_member_game_daily.updateMany({{ \"operator_id\": \"{mapping.OperatorId}\", \"member_id\": \"{mapping.OldMemberId}\" }}, {{ $set: {{ \"member_id\": \"{mapping.NewMemberId}\" }} }});");
                }
                scriptWriter.WriteLine();

                // SummaryMemberGameMonthly
                scriptWriter.WriteLine("// --- SummaryMemberGameMonthly ---");
                foreach (var mapping in allAccountingUpdateMappings)
                {
                    scriptWriter.WriteLine($"db.summary_member_game_monthly.updateMany({{ \"operator_id\": \"{mapping.OperatorId}\", \"member_id\": \"{mapping.OldMemberId}\" }}, {{ $set: {{ \"member_id\": \"{mapping.NewMemberId}\" }} }});");
                }
                scriptWriter.WriteLine();
            }

            //// MySQL - TransferLog 更新語法
            //if (allTransferLogUpdateMappings.Any())
            //{
            //    scriptWriter.WriteLine("-- ========== MySQL 語法 (TransferLog) ==========");
            //    scriptWriter.WriteLine("-- 合併 TransferLog 轉帳記錄到保留的會員");
            //    scriptWriter.WriteLine($"-- 共 {allTransferLogUpdateMappings.Count} 筆待更新");
            //    scriptWriter.WriteLine();
            //    foreach (var mapping in allTransferLogUpdateMappings)
            //    {
            //        scriptWriter.WriteLine($"UPDATE `member_transfer_log` SET `member_id` = '{mapping.NewMemberId}' WHERE `member_id` = '{mapping.OldMemberId}';");
            //    }
            //    scriptWriter.WriteLine();
            //}

            //// MongoDB - MemberLoginLog 更新語法 (Agent Warm DB)
            //if (allLoginLogUpdateMappings.Any())
            //{
            //    scriptWriter.WriteLine("// ========== MongoDB 語法 (Agent Warm DB - MemberLoginLog) ==========");
            //    scriptWriter.WriteLine("// 合併 MemberLoginLog 資料到保留的會員");
            //    scriptWriter.WriteLine($"// 共 {allLoginLogUpdateMappings.Count} 筆待更新");
            //    scriptWriter.WriteLine();
            //    foreach (var mapping in allLoginLogUpdateMappings)
            //    {
            //        scriptWriter.WriteLine($"db.member_login_log.updateMany({{ \"member_id\": \"{mapping.OldMemberId}\" }}, {{ $set: {{ \"member_id\": \"{mapping.NewMemberId}\" }} }});");
            //    }
            //    scriptWriter.WriteLine();
            //}

            // MongoDB - MemberWallet 刪除語法 (Agent Hot DB)
            if (allWalletIdsToDelete.Any())
            {
                scriptWriter.WriteLine("// ========== MongoDB 語法 (Agent Hot DB - MemberWallet) ==========");
                scriptWriter.WriteLine("// 刪除重複會員的錢包");
                scriptWriter.WriteLine($"// 共 {allWalletIdsToDelete.Count} 筆待刪除");
                scriptWriter.WriteLine();
                var walletIdsFormatted = string.Join(", ", allWalletIdsToDelete.Select(id => $"ObjectId(\"{id}\")"));
                scriptWriter.WriteLine($"db.member_wallet.deleteMany({{ \"_id\": {{ $in: [{walletIdsFormatted}] }} }});");
                scriptWriter.WriteLine();
            }

            // MySQL - Member 刪除語法
            if (allMemberIdsToDelete.Any())
            {
                scriptWriter.WriteLine("-- ========== MySQL 語法 (Member) ==========");
                scriptWriter.WriteLine("-- 刪除重複的會員記錄");
                scriptWriter.WriteLine($"-- 共 {allMemberIdsToDelete.Count} 筆待刪除");
                scriptWriter.WriteLine();
                var memberIdsFormatted = string.Join(", ", allMemberIdsToDelete.Select(id => $"'{id}'"));
                scriptWriter.WriteLine($"DELETE FROM `member` WHERE `id` IN ({memberIdsFormatted});");
                scriptWriter.WriteLine();
            }

            scriptWriter.Flush();
            Log($"\n語法檔案已儲存至: {scriptFilePath}");
        }

        Log($"\n日誌文件已儲存至: {logFilePath}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 備份會員原始資料
    /// </summary>
    private void BackupMemberData( List<Member> memberList, List<MemberWallet>? memberWallets, IMongoDatabase agentHotMongoDBContext)
    {
        int backupMemberCount = 0;
        int backupWalletCount = 0;

        // 備份所有會員資料到 MemberCleaningBackup
        if (memberList.Any())
        {
            var backupMembers = memberList.Select(x => new MemberCleaningBackup
            {
                Id = x.Id,
                OperatorId = x.OperatorId,
                Account = x.Account,
                Password = x.Password,
                Status = x.Status,
                DefaultRateIdx = x.DefaultRateIdx,
                FristAccountAt = x.FristAccountAt,
                LastLoginAt = x.LastLoginAt,
                CreatedAt = x.CreatedAt,
            }).ToList();

            _memberCleaningBackupRepository.Create(backupMembers);
            backupMemberCount = backupMembers.Count;
            Console.WriteLine($"  ✓ 已備份 {backupMemberCount} 筆 Member 資料");
        }

        // 備份所有 MemberWallet 資料到 MemberWalletCleaningBackup
        if (memberWallets != null && memberWallets.Any())
        {
            var memberWalletCleaningBackupRepository = new MemberWalletCleaningBackupRepository(agentHotMongoDBContext);
            var backupWallets = memberWallets.Select(x => new MemberWalletCleaningBackup
            {
                Id = x.Id,
                MemberId = x.MemberId,
                Balance = x.Balance,
                UpdatedAt = x.UpdatedAt
            }).ToList();

            memberWalletCleaningBackupRepository.UnorderedRetryCreate(backupWallets);
            backupWalletCount = backupWallets.Count;
            Console.WriteLine($"  ✓ 已備份 {backupWalletCount} 筆 MemberWallet 資料");
        };
    }

    public class MemberQueryResult
    {
        public Member Member { get; set; }
        public long AccountingCount { get; set; }
        public int TransferLogCount { get; set; }
        public long LoginLogCount { get; set; }
        public List<MemberWallet> MemberWalletList { get; set; }
    }

    /// <summary>
    /// 查詢會員錢包資料統計 - 依照 WalletType 分組，統計每個營運商有錢包/無錢包的會員數
    /// </summary>
    public async Task GetMemberNotWallet(WalletTypeEnum? walletType = null, string? operatorId = null)
    {
        Console.WriteLine("\n===== 查詢會員錢包資料統計 =====");
        Console.WriteLine($"開始時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

        // Agent Hot DB (member_wallet 在 Hot DB)
        var agentHotMongoDBContext = _dbBHelper.GetMongoDatabase(ConfigManager.ConnectionStrings.AgentMongoConnection);
        var memberWalletRepository = new MemberWalletRepository(agentHotMongoDBContext);

        // 取得所有營運商（包含 WalletType 資訊）
        var allOperators = _operatorRepository.GetAllOperators();

        // 根據參數過濾營運商
        if (walletType != null)
            allOperators = allOperators.Where(x => x.WalletType == walletType.Value).ToList();
        if (!string.IsNullOrEmpty(operatorId))
            allOperators = allOperators.Where(x => x.Id == operatorId).ToList();

        // 依照 WalletType 分組
        var operatorGroups = allOperators
            .GroupBy(x => x.WalletType)
            .OrderBy(g => g.Key);

        // 總計
        int grandTotalMembers = 0;
        int grandTotalWithWallet = 0;
        int grandTotalWithoutWallet = 0;

        foreach (var walletTypeGroup in operatorGroups)
        {
            var walletTypeText = walletTypeGroup.Key switch
            {
                WalletTypeEnum.WalletType_Shared => "共用錢包",
                WalletTypeEnum.WalletType_Single => "獨立錢包",
                _ => "未知錢包類型"
            };

            Console.WriteLine(new string('=', 100));
            Console.WriteLine($"【{walletTypeText}】營運商數量: {walletTypeGroup.Count()}");
            Console.WriteLine(new string('=', 100));
            Console.WriteLine($"{"營運商 ID",-40} {"營運商名稱",-20} {"會員總數",12} {"有錢包",10} {"無錢包",10}");
            Console.WriteLine(new string('-', 100));

            int walletTypeTotalMembers = 0;
            int walletTypeTotalWithWallet = 0;
            int walletTypeTotalWithoutWallet = 0;

            foreach (var op in walletTypeGroup.OrderBy(x => x.Id))
            {
                // 取得該營運商的所有會員 ID
                var memberIds = _memberRepository.GetIdsByOperatorId(op.Id);
                var totalMembers = memberIds.Count;

                if (totalMembers == 0)
                {
                    Console.WriteLine($"{op.Id,-40} {op.OperatorName,-20} {0,12} {0,10} {0,10}");
                    continue;
                }

                // 批量查詢有錢包的會員 ID
                var existingWalletMemberIds = await memberWalletRepository.GetExistingMemberIdsAsync(memberIds);
                var withWalletCount = existingWalletMemberIds.Count;
                var withoutWalletCount = totalMembers - withWalletCount;

                Console.WriteLine($"{op.Id,-40} {op.OperatorName,-20} {totalMembers,12} {withWalletCount,10} {withoutWalletCount,10}");

                // 累計此錢包類型的統計
                walletTypeTotalMembers += totalMembers;
                walletTypeTotalWithWallet += withWalletCount;
                walletTypeTotalWithoutWallet += withoutWalletCount;
            }

            Console.WriteLine(new string('-', 100));
            Console.WriteLine($"{"小計",-40} {"",-20} {walletTypeTotalMembers,12} {walletTypeTotalWithWallet,10} {walletTypeTotalWithoutWallet,10}");
            Console.WriteLine();

            // 累計總計
            grandTotalMembers += walletTypeTotalMembers;
            grandTotalWithWallet += walletTypeTotalWithWallet;
            grandTotalWithoutWallet += walletTypeTotalWithoutWallet;
        }

        // 輸出總計
        Console.WriteLine(new string('=', 100));
        Console.WriteLine("【總計】");
        Console.WriteLine($"  ● 會員總數: {grandTotalMembers:N0}");
        Console.WriteLine($"  ● 有錢包會員: {grandTotalWithWallet:N0}");
        Console.WriteLine($"  ● 無錢包會員: {grandTotalWithoutWallet:N0}");
        Console.WriteLine(new string('=', 100));
        Console.WriteLine($"\n完成時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        await Task.CompletedTask;
    }
}

public interface IMemberCleaningService : IServiceBase
{
    /// <summary>
    /// 查詢所有重複帳號
    /// </summary>
    Task GetAllDuplicateAccounts(WalletTypeEnum? walletType = null, string? operatorId = null);

    /// <summary>
    /// 查詢指定營運商的會員資料
    /// </summary>
    Task GetOperatorDuplicateAccounts(WalletTypeEnum? walletType = null, string? operatorId = null);

    /// <summary>
    /// 處理重複帳號 - 合併會員資料並刪除多餘的記錄
    /// </summary>
    Task ProcessOperatorDuplicateAccounts(bool mergeAccounting, WalletTypeEnum? walletType = null, string? operatorId = null);

    /// <summary>
    /// 處理重複帳號 - 只處理完全沒有使用過的會員合併（無注單、無轉帳紀錄、無餘額）
    /// </summary>
    Task ProcessUnusedDuplicateAccounts(WalletTypeEnum? walletType = null, string? operatorId = null);

    /// <summary>
    /// 產生處理重複帳號的 SQL/MongoDB 語法 - 只產生語法不執行（無注單、無轉帳紀錄、無餘額）
    /// </summary>
    Task GenerateUnusedDuplicateAccountsScript(WalletTypeEnum? walletType = null, string? operatorId = null);

    /// <summary>
    /// 產生處理重複帳號的更新語法 - 只產生語法不執行（全部合併不跳過任何情況）
    /// </summary>
    Task GenerateProcessUnusedDuplicateAccountsUpdateScript(WalletTypeEnum? walletType = null, string? operatorId = null);

    /// <summary>
    /// 查詢會員錢包資料統計 - 依照 WalletType 分組，統計每個營運商有錢包/無錢包的會員數
    /// </summary>
    Task GetMemberNotWallet(WalletTypeEnum? walletType = null, string? operatorId = null);
}