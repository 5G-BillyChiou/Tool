using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;
using Tool.Model.Entity.FiveGameTrans;
using Tool.Model.Repository.FiveGame;
using Tool.Model.Repository.FiveGameTrans;

namespace Tool.Service;

/// <summary>
/// Ledger 相關服務
/// </summary>
public class LedgerService(IMemberTransferLogRepository _memberTransferLogRepository) : ILedgerService
{
    /// <summary>
    /// 取得有 Ledger 但沒有 Log 的紀錄
    /// </summary>
    public void GetHasLederButNotLog()
    {
        var start1 = new DateTimeOffset(2026, 4, 28, 16, 55, 00, TimeSpan.Zero);
        var endAt1 = new DateTimeOffset(2026, 4, 28, 17, 00, 00, TimeSpan.Zero);

        var start2 = new DateTimeOffset(2026, 4, 29, 01, 30, 00, TimeSpan.Zero);
        var endAt2 = new DateTimeOffset(2026, 4, 29, 02, 00, 00, TimeSpan.Zero);

        // 建立 FiveGameTrans DbContext（非常駐連線，直接 new）
        var options = new DbContextOptionsBuilder<FiveGameTransEntities>()
            .UseMySql(ConfigManager.ConnectionStrings.FiveGameTransConnection,
                      new MySqlServerVersion(new Version(8, 0, 32)),
                      o => o.UseMicrosoftJson()
                            .CommandTimeout(60))
            .Options;

        using var dbContext = new FiveGameTransEntities(options);
        dbContext.Database.ExecuteSqlRaw("SET SESSION ob_query_timeout = 120000000"); // OceanBase: 120s (us)
        var ledgerRepository = new LedgerRepository(dbContext);

        var lederList1 = ledgerRepository.GetListByTimeRange(start1, endAt1);
        var lederList2 = ledgerRepository.GetListByTimeRange(start2, endAt2);
        var lederList = lederList1.Concat(lederList2).ToList();

        var transferList1 = _memberTransferLogRepository.GetListByTimeRange(start1, endAt1);
        var transferList2 = _memberTransferLogRepository.GetListByTimeRange(start2, endAt2);
        var transferList = transferList1.Concat(transferList2).ToList();

        // ledger.reference_id 沒有對應 member_transfer_log.txn_id 的清單
        var transferTxnIdSet = transferList.Select(t => t.TxnId).ToHashSet();
        var missingLogList = lederList
            .Where(l => !transferTxnIdSet.Contains(l.ReferenceId))
            .ToList();

        // ledger.amount_cent = 0（實際未動帳）
        var noChangeList = lederList
            .Where(l => l.AmountCent == 0)
            .ToList();

        var outputPath = Path.Combine(AppContext.BaseDirectory, $"ledger_check_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");
        using var writer = new StreamWriter(outputPath, append: false, encoding: System.Text.Encoding.UTF8);

        void WriteLine(string line)
        {
            Console.WriteLine(line);
            writer.WriteLine(line);
        }

        WriteLine($"=== 有 Ledger 但無對應 TransferLog（共 {missingLogList.Count} 筆）===");
        WriteLine($"{"ledger.id",-28} | {"reference_id",-36} | {"member_id",-28} | {"amount_cent",14} | {"created_at",22}");
        WriteLine(new string('-', 140));
        foreach (var l in missingLogList)
            WriteLine($"{l.Id,-28} | {l.ReferenceId,-36} | {l.MemberId,-28} | {l.AmountCent,14} | {l.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}");

        WriteLine(string.Empty);
        WriteLine($"=== INSERT SQL（共 {missingLogList.Count} 筆，before_cent / after_cent 請自行填入）===");
        foreach (var l in missingLogList)
        {
            var type = l.AmountCent >= 0 ? 1 : 2; // 1=存入 2=提出
            var transferCent = Math.Abs(l.AmountCent);
            var transferAt = l.CreatedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            WriteLine($"INSERT INTO member_transfer_log (txn_id, member_id, operator_id, currency_sn, type, transfer_at, before_cent, transfer_cent, after_cent, status, note, created_at) VALUES ('{l.ReferenceId}', '{l.MemberId}', '{l.OperatorId}', {l.CurrencySn}, {type}, '{transferAt}', 999, {transferCent}, 999, 1, NULL, '{transferAt}');");
        }

        WriteLine(string.Empty);
        WriteLine($"=== Ledger amount_cent = 0（共 {noChangeList.Count} 筆）===");
        WriteLine($"{"ledger.id",-28} | {"reference_id",-36} | {"member_id",-28} | {"amount_cent",14} | {"created_at",22}");
        WriteLine(new string('-', 140));
        foreach (var l in noChangeList)
            WriteLine($"{l.Id,-28} | {l.ReferenceId,-36} | {l.MemberId,-28} | {l.AmountCent,14} | {l.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}");

        Console.WriteLine($"\n已匯出至：{outputPath}");

    }

    public void CheckLog()
    {
        var data = new List<string>
        {
            //"69f15fd790229a3f5eb940bc",
            //"69f0e798d11a08f8e2055fa3",
            //"69f0e7930b0ec9092ee92d9e",
            //"69f164df0b0ec9092e07339d",
            //"69f164ca0b0ec9092e073387",
            //"69f164b890229a3f5eb96471",
            //"69f164c290229a3f5eb9649d",
            //"69f164c00c5035e037eecb20",
            //"69f164c60b0ec9092e07337d",
            //"69f164c9ad53497e682dfc32",
            //"69f164c990229a3f5eb964a7",
            //"69f164cf90229a3f5eb964ae",
            //"69f164b95b0ad89711eeb1bd",
            //"69f164bb2d3edb2206191559",
            //"69f16419ba0c4eb943b46bae",
            //"69f164149c359798ecf28d22",
            //"69f164142d3edb22061912c3",
            //"69f164132d3edb22061912aa",
            //"69f1640e0b0ec9092e07302f",
            //"69f1640e90229a3f5eb960c0",
            //"69f16408d11a08f8e2236c72",
            //"69f164079c359798ecf28b8d",
            //"69f16404015b32855c850fe4",
            //"69f163f6ad53497e682df5e0",
            //"69f163e490229a3f5eb95ce7",
            //"69f163e35b0ad89711eea9b7",
            //"69f163e29c359798ecf287d1",
            //"69f16424015b32855c851373",
            //"69f1641dd11a08f8e2236f31",
            //"69f1642290229a3f5eb962e1",

            //"69f1641cad53497e682dfaf8",
            //"69f16228d11a08f8e2236681",
            //"69f161e9ba0c4eb943b4615f",
            //"69f1621990229a3f5eb959be",
            //"69f1621690229a3f5eb9599c",
            //"69f161dcad53497e682dee5c",
            //"69f161d4ba0c4eb943b45f94",
            //"69f161d20b0ec9092e072418",
            //"69f161ca5b0ad89711eea1d8",
            //"69f1606b5b0ad89711eea003",
            //"69f160300b0ec9092e071faf",
            //"69f1600b0c5035e037eeb6bf",
            //"69f160802d3edb2206190502",
            //"69f1600cd11a08f8e2235e81",
            //"69f1600dad53497e682de900",
            //"69f16014015b32855c85021f",
            //"69f16010d11a08f8e2235f14",
            //"69f16006ba0c4eb943b45874",
            //"69f160015b0ad89711ee9b07",
            //"69f160002d3edb220618ffd2",
            //"69f15ffc5b0ad89711ee9941",
            //"69f15ff69c359798ecf276d4",
            //"69f15fee2d3edb220618f9e7",
            //"69f15fe99c359798ecf272da",
            //"69f15fe9d11a08f8e2235363",
            //"69f15fe10b0ec9092e0710ab",
            //"69f15fded11a08f8e2234fc5",
            //"69f15fdd90229a3f5eb94280",
            //"69f15fd89c359798ecf26d41",
            //"69f15fd6ad53497e682dd725",

            //"69f15fc7ba0c4eb943b4452e",
            //"69f15fc50c5035e037ee9fd0",
            //"69f15fc29c359798ecf265f1",
            //"69f15f9b0c5035e037ee927e",
            //"69f15f949c359798ecf256dd",
            //"69f0e79a272250bd630ce58c",
            //"69f0e798272250bd630ce56c",
            //"69f0e7986b4a7f1e062760db",
            //"69f0e79890229a3f5e9aeb1a",
            //"69f0e7982d51f7db4c3c0440",
            //"69f0e7986b4a7f1e062760d5",
            //"69f0e798ad53497e680f6912",
            //"69f0e7987902159455d73247",
            //"69f0e7982d51f7db4c3c0420",
            //"69f0e7987766680f74c00145",
            //"69f0e79890229a3f5e9aeae9",
            //"69f0e797d11a08f8e2055f76",
            //"69f0e7972d51f7db4c3c041c",
            //"69f0e7975b0ad89711d09871",
            //"69f0e797015b32855c66b54f",
            //"69f0e797ad53497e680f68e0",
            //"69f0e79790229a3f5e9aead6",
            //"69f0e797ad53497e680f68dd",
            //"69f0e7967766680f74c00120",
            //"69f0e7960b0ec9092ee92e0c",
            //"69f0e7962d51f7db4c3c03f9",
            //"69f0e7966b4a7f1e0627608a",
            //"69f0e7960b0ec9092ee92e05",
            //"69f0e7956b4a7f1e06276088",
            //"69f0e795ad53497e680f68c1",

            //"69f0e7957766680f74c00115",
            //"69f0e7956b4a7f1e06276081",
            //"69f0e79590229a3f5e9aeab1",
            //"69f0e795d11a08f8e2055f37",
            //"69f0e7950c5035e037d0fd96",
            //"69f0e795ba0c4eb94396e56b",
            //"69f0e79590229a3f5e9aeaa5",
            //"69f0e7956b4a7f1e0627606c",
            //"69f0e794272250bd630ce501",
            //"69f0e794272250bd630ce4fe",
            //"69f0e7945b0ad89711d0982f",
            //"69f0e794015b32855c66b507",
            //"69f0e7946b4a7f1e06276057",
            //"69f0e794272250bd630ce4f2",
            //"69f0e7946b4a7f1e06276052",
            //"69f0e794ba0c4eb94396e54e",
            //"69f0e794272250bd630ce4f1",
            //"69f0e794ba0c4eb94396e54c",
            //"69f0e7945b0ad89711d09818",
            //"69f0e794272250bd630ce4ee",
            //"69f0e7947902159455d731c8",
            //"69f0e7947766680f74c000e1",

            //"69f0e7946b4a7f1e06276049",
            //"69f0e7937766680f74c000e0",
            //"69f0e7935b0ad89711d09817",
            //"69f0e7936b4a7f1e06276048",
            //"69f0e79390229a3f5e9aea83",
            //"69f0e7939c359798ecd47962",
            //"69f0e7937766680f74c000da",
            //"69f0e793ad53497e680f6885",
            //"69f0e79390229a3f5e9aea80",
            //"69f0e793ba0c4eb94396e543",
            //"69f0e7930b0ec9092ee92dc2",
            //"69f0e7930c5035e037d0fd5e",
            //"69f0e79390229a3f5e9aea7c",
            //"69f0e7939c359798ecd4795a",
            //"69f0e7937902159455d731b4",
            //"69f0e793d11a08f8e2055f07",
            //"69f0e7931e2bc6cc4649e60a",
            //"69f0e793272250bd630ce4da",
            //"69f0e7937766680f74c000c9",
            //"69f0e793015b32855c66b4e7",

            //"69f0e7932d51f7db4c3c03af",
            //"69f0e7936b4a7f1e06276039",
            //"69f0e7937766680f74c000c5",
            //"69f0e7939c359798ecd4794f",
            //"69f0e793ba0c4eb94396e53c",
            //"69f0e793015b32855c66b4e1",
            //"69f0e7930c5035e037d0fd55",
            //"69f0e793ba0c4eb94396e532",
            //"69f0e7935b0ad89711d097fb",
            //"69f0e793ba0c4eb94396e530",
            //"69f0e793015b32855c66b4d5",
            //"69f0e793d11a08f8e2055ef9",
            //"69f0e7935b0ad89711d097f7",
            //"69f0e7930b0ec9092ee92db0",
            //"69f0e7931e2bc6cc4649e5fd",
            //"69f0e7935b0ad89711d097f3",
            //"69f0e7935b0ad89711d097f2",
            //"69f0e793ba0c4eb94396e52d",

            //"69f0e793ba0c4eb94396e52b",
            //"69f0e7930c5035e037d0fd48",
            //"69f0e7935b0ad89711d097ec",
            //"69f0e7937902159455d731a2",
            //"69f0e793ba0c4eb94396e52a",
            //"69f0e79390229a3f5e9aea65",
            //"69f0e79390229a3f5e9aea64",
            //"69f0e7932d3edb2206fad155",
            //"69f0e7937902159455d7319d",
            //"69f0e7937766680f74c000b2",
            //"69f0e793ad53497e680f686d",
            //"69f0e7932d3edb2206fad152",
            //"69f0e793ad53497e680f686a",
            //"69f0e7932d51f7db4c3c039b",
            //"69f0e7930c5035e037d0fd41",
            //"69f0e7931e2bc6cc4649e5ea",
            //"69f0e793ad53497e680f6868",
            //"69f0e78a2d51f7db4c3c01cf",
            //"69f0e7892d3edb2206facf3b",
            //"69f0e780ad53497e680f6443",
            //"69f0e77eba0c4eb94396e09a",
            //"69f0e77d90229a3f5e9ae564",
            //"69f0e7752d51f7db4c3bfbe1",

            //"69f0e7716b4a7f1e062756a0",
            //"69f0e7707902159455d727f6",
            //"69f0e76bd11a08f8e205531a",
            //"69f0e7680c5035e037d0efff",
            //"69f0e7666b4a7f1e062752b4",
            //"69f0e75a90229a3f5e9ad951",
            //"69f0e759ad53497e680f563f",

            //"69f0e7577766680f74bfee75",
            //"69f0e74f6b4a7f1e06274a6e",
            //"69f0e74f1e2bc6cc4649cf94",
            //"69f0e74690229a3f5e9ad223",
            //"69f0e7422d51f7db4c3beadf",
            //"69f0e73a2d51f7db4c3be824",
            //"69f0e72e6b4a7f1e06273e24",
            //"69f0e70b6b4a7f1e06273152",
            //"69f15f6e5b0ad89711ee6c09",
            //"69f0e7939c359798ecd4795c",
            //"69f0e7937902159455d73195",
            //"69f164cb0c5035e037eecb29",
            //"69f164b89c359798ecf28f98",
            //"69f164ca015b32855c851476",
            //"69f164c82d3edb2206191566",
            //"69f164c990229a3f5eb964a6",

            //"69f164bc2d3edb220619155b",
            //"69f164c70b0ec9092e073382",
            //"69f164c1ad53497e682dfc1a",
            //"69f164c40c5035e037eecb26",
            //"69f164ba5b0ad89711eeb1c3",
            //"69f164b95b0ad89711eeb1c1",
            //"69f164c45b0ad89711eeb1e2",
            //"69f164152d3edb22061912f3",
            //"69f164159c359798ecf28d42",
            //"69f16410ba0c4eb943b46a8b",
            //"69f164089c359798ecf28b95",
            //"69f1640390229a3f5eb95f99",
            //"69f163fd9c359798ecf28a5c",
            //"69f163ee015b32855c850d8d",

            "69f163e29c359798ecf287db",
            "69f1641dad53497e682dfb02",
            "69f1641e90229a3f5eb962ae",
            "69f163e09c359798ecf287bb",
            "69f163db0c5035e037eec202",
            "69f1622dad53497e682df152",
            "69f162150b0ec9092e0727e8",
            "69f162230b0ec9092e072836",
            "69f161d7015b32855c85069f",
            "69f161c5ba0c4eb943b45e3e",
            "69f1602dba0c4eb943b45bdd",
            "69f16010d11a08f8e2235f13",
            "69f1600dd11a08f8e2235eca",
            "69f15f7b015b32855c84d15c",
            "69f15f64015b32855c84c995",
            "69f1600c2d3edb22061903a6",
            "69f1600ead53497e682de91d",
            "69f160149c359798ecf27e75",
            "69f1600a015b32855c8500ae",
            "69f160092d3edb220619029f",
            "69f16006015b32855c84ff38",
            "69f15fff015b32855c84fcda",
            "69f15ff990229a3f5eb94b7b",
            "69f15ff7d11a08f8e22357d5",
            "69f15fe7ba0c4eb943b44f05",
            "69f15fe69c359798ecf27204",
            "69f15fe35b0ad89711ee91ad",
            "69f15fe1015b32855c84f288",
            "69f15fded11a08f8e2234faf",
            "69f15fdc90229a3f5eb94215",

            //"69f15fd8ad53497e682dd803",
            //"69f15fd8ba0c4eb943b44a4f",
            //"69f15fd22d3edb220618f0db",
            //"69f15fcd9c359798ecf269eb",
            //"69f15fc45b0ad89711ee87c7",
            //"69f15fbf015b32855c84e7c7",
            //"69f0e79a90229a3f5e9aeb4a",
            //"69f0e79990229a3f5e9aeb40",
            //"69f0e79990229a3f5e9aeb27",
            //"69f0e7986b4a7f1e062760d8",
            //"69f0e798ad53497e680f6909",
            //"69f0e798272250bd630ce55d",
            //"69f0e7982d51f7db4c3c042c",
            //"69f0e798d11a08f8e2055f80",
            //"69f0e798ba0c4eb94396e5ab",
            //"69f0e79790229a3f5e9aeae1",
            //"69f0e797015b32855c66b54e",
            //"69f0e7979c359798ecd479cb",
            //"69f0e7977766680f74c0012f",
            //"69f0e7976b4a7f1e06276097",
            //"69f0e7972d51f7db4c3c0405",
            //"69f0e796d11a08f8e2055f4d",
            //"69f0e796ba0c4eb94396e581",
            //"69f0e796d11a08f8e2055f47",
            //"69f0e796d11a08f8e2055f44",
            //"69f0e795272250bd630ce51a",
            //"69f0e795d11a08f8e2055f3d",
            //"69f0e795272250bd630ce513",
            //"69f0e7959c359798ecd47994",
            //"69f0e7957766680f74c00109",

            "69f0e7956b4a7f1e0627606e",
            "69f0e7957766680f74c00103",
            "69f0e79590229a3f5e9aeaa4",
            "69f0e7950c5035e037d0fd8f",
            "69f0e7940b0ec9092ee92deb",
            "69f0e794272250bd630ce502",
            "69f0e794ba0c4eb94396e55f",
            "69f0e794272250bd630ce500",
            "69f0e7947766680f74c000f5",
            "69f0e7947766680f74c000f3",
            "69f0e7942d3edb2206fad191",
            "69f0e794d11a08f8e2055f1e",
            "69f0e7945b0ad89711d0982c",
            "69f0e79490229a3f5e9aea92",
            "69f0e794015b32855c66b505",
            "69f0e7946b4a7f1e06276055",
            "69f0e794272250bd630ce4f3",
            "69f0e79490229a3f5e9aea8c",
            "69f0e794d11a08f8e2055f1a",
            "69f0e7947766680f74c000e8",
            "69f0e7942d51f7db4c3c03c4",
            "69f0e7947766680f74c000e3",
            "69f0e7942d3edb2206fad183",
            "69f0e7940b0ec9092ee92dcc",
            "69f0e794015b32855c66b4f8",
            "69f0e7931e2bc6cc4649e610",
            "69f0e793272250bd630ce4e9",
            "69f0e7937766680f74c000d8",
            "69f0e7936b4a7f1e06276043",
            "69f0e7932d51f7db4c3c03bc",
            "69f0e793015b32855c66b4f0",
            "69f0e793d11a08f8e2055f0a",
            "69f0e793ba0c4eb94396e541",
            "69f0e7939c359798ecd47958",
            "69f0e7935b0ad89711d09808",
            "69f0e793ba0c4eb94396e53a",
            "69f0e793015b32855c66b4e3",
            "69f0e7937766680f74c000be",
            "69f0e793272250bd630ce4d7",
            "69f0e7932d51f7db4c3c03a7",
            "69f0e793015b32855c66b4dd",
            "69f0e7931e2bc6cc4649e5ff",
            "69f0e7937902159455d731a7",
            "69f0e79390229a3f5e9aea66",
            "69f0e7932d3edb2206fad154",
            "69f0e7930c5035e037d0fd43",
            "69f0e793d11a08f8e2055eee",
            "69f0e7937766680f74c000b3",
            "69f0e7937766680f74c000b1",
            "69f0e793d11a08f8e2055eed",
            "69f0e7930b0ec9092ee92da2",
            "69f0e793272250bd630ce4c6",
            "69f0e793d11a08f8e2055ee8",
            "69f0e7930c5035e037d0fd3e",
            "69f0e7932d51f7db4c3c0395",
            "69f0e7935b0ad89711d097e5",
            "69f0e7936b4a7f1e06276027",
            "69f0e7930c5035e037d0fd3d",
            "69f0e7937902159455d73193",
            "69f0e7930b0ec9092ee92d9b",
            "69f0e7932d51f7db4c3c038d",
            "69f0e7937902159455d73191",
            "69f0e792272250bd630ce4bd",
            "69f0e792272250bd630ce4ba",
            "69f0e7877902159455d72f40",
            "69f0e7810c5035e037d0f94f",
            "69f0e77b0b0ec9092ee9276c",
            "69f0e7792d51f7db4c3bfd52",
            "69f0e7785b0ad89711d09071",
            "69f0e777272250bd630cdd93",
            "69f0e7760b0ec9092ee925bd",
            "69f0e771d11a08f8e205553b",
            "69f0e76f90229a3f5e9ae080",
            "69f0e7666b4a7f1e062752c0",
            "69f0e7666b4a7f1e06275296",
            "69f0e7656b4a7f1e06275228",
            "69f0e76490229a3f5e9adcb2",
            "69f0e7609c359798ecd46995",
            "69f0e75ed11a08f8e2054e7a",
            "69f0e74a7766680f74bfe9f1",
            "69f164c8ad53497e682dfc2e",
            "69f16406ba0c4eb943b4696a",
            "69f15fd05b0ad89711ee8b70",
            "69f0e799ad53497e680f6921",
            "69f0e7970b0ec9092ee92e24",
            "69f0e7976b4a7f1e06276098",
            "69f0e7960b0ec9092ee92e08",
            "69f0e7930c5035e037d0fd5a",
            "69f0e7931e2bc6cc4649e5f9",
            "69f0e7931e2bc6cc4649e5e6",
            "69f0e792ad53497e680f6861",
            "69f0e7992d51f7db4c3c0448",
            "69f0e79890229a3f5e9aeb12",
            "69f0e7946b4a7f1e0627605a",
            "69f0e7939c359798ecd47960",
            "69f0e7930c5035e037d0fd5b",
            "69f0e771015b32855c66ab54",
            "69f0e7472d3edb2206fab81b",
            "69f15fff0b0ec9092e071a3e",
            "69f0e7937902159455d731b2",
            "69f0e7931e2bc6cc4649e5ef",
            "69f164bb2d3edb2206191557",
            "69f15fe2015b32855c84f2f5",
            "69f15fddad53497e682dd9ad",
            "69f0e7987902159455d73249",
            "69f0e7932d51f7db4c3c03a6",
            "69f0e7937766680f74c000bf",
            "69f0e793d11a08f8e2055ee9",
            "69f164c80b0ec9092e073384",
            "69f1641a0b0ec9092e0731cf",
            "69f1641190229a3f5eb9611e",
            "69f163f4ad53497e682df5bb",
            "69f164405b0ad89711eeb112",
            "69f16238ad53497e682df16d",
            "69f161f89c359798ecf284c8",
            "69f161d75b0ad89711eea316",
            "69f16014015b32855c850221",
            "69f1600b015b32855c8500cb",
            "69f16001015b32855c84fd7b",
            "69f15ff4d11a08f8e2235708",
            "69f15fcd2d3edb220618ef2e",
            "69f0e7987766680f74c0014e",
            "69f0e797015b32855c66b54c",
            "69f0e79790229a3f5e9aeadc",
            "69f0e7970c5035e037d0fdc6",
            "69f0e7960b0ec9092ee92e14",
            "69f0e795d11a08f8e2055f41",
            "69f0e795d11a08f8e2055f3f",
            "69f0e7942d51f7db4c3c03d8",
            "69f0e7941e2bc6cc4649e617",
            "69f0e7940b0ec9092ee92dca",
            "69f0e7932d51f7db4c3c03a8",
            "69f0e7937902159455d731a1",
            "69f0e7930b0ec9092ee92da7",
            "69f0e7935b0ad89711d097eb",
            "69f0e7937766680f74c000b5",
            "69f0e7937766680f74c000b4",
            "69f0e79390229a3f5e9aea63",
            "69f0e7721e2bc6cc4649dc43",
            "69f0e71b90229a3f5e9ac293",
            "69f164ba9c359798ecf28f9f",
            "69f164c25b0ad89711eeb1e0",
            "69f164bc5b0ad89711eeb1ce",
            "69f164c65b0ad89711eeb1e7",
            "69f164c05b0ad89711eeb1db",
            "69f1641fba0c4eb943b46c72",
            "69f1641dd11a08f8e2236f35",
            "69f164220c5035e037eeca84",
            "69f1641e5b0ad89711eeb099",
            "69f1641f0c5035e037eeca63",
            "69f1641ed11a08f8e2236f4d",
            "69f1640390229a3f5eb95fa1",
            "69f163ef5b0ad89711eeaae5",
            "69f163e52d3edb2206190d33",
            "69f1621a90229a3f5eb959c1",
            "69f160129c359798ecf27e60",
            "69f1600bad53497e682de898",
            "69f1600b5b0ad89711ee9e58",
            "69f15fe1015b32855c84f2e1",
            "69f15fd290229a3f5eb93f2a",
            "69f15fcd5b0ad89711ee8a94",
            "69f0e7977766680f74c0013a",
            "69f0e7976b4a7f1e062760a0",
            "69f0e794015b32855c66b4fe",
            "69f0e7937902159455d731b6",
            "69f0e793272250bd630ce4c7",
            "69f0e79290229a3f5e9aea5a",
            "69f0e792ba0c4eb94396e519",
            "69f0e7922d51f7db4c3c0386",
            "69f0e7927902159455d7318b",
            "69f0e77c6b4a7f1e06275a9b",
            "69f0e7587766680f74bfeefe",
            "69f0e74e6b4a7f1e062749c4",
            "69f0e740015b32855c66999f",
            "69f164bb9c359798ecf28fa7",
            "69f164bbd11a08f8e2237094",
            "69f1643e90229a3f5eb9632d",
            "69f164bc90229a3f5eb96489",
            "69f161ca0c5035e037eebb50",
            "69f15fff0b0ec9092e071a6d",
            "69f15fedd11a08f8e22354a8",
            "69f0e7977766680f74c00131",
            "69f0e7967902159455d7320b",
            "69f0e7947766680f74c000fc",
            "69f0e7947766680f74c000ee",
            "69f0e7939c359798ecd4795b",
            "69f0e793ad53497e680f687a",
            "69f0e793ba0c4eb94396e534",
            "69f0e7931e2bc6cc4649e602",
            "69f0e757015b32855c66a235",
            "69f0e751015b32855c66a000",
            "69f0e74a272250bd630ccee5",
            "69f0e749272250bd630cce9a",
            "69f1600590229a3f5eb94f40",
            "69f163fbad53497e682df68c",
            "69f1642390229a3f5eb962e4",
            "69f1641d015b32855c85130f",
            "69f1600c015b32855c850152",
            "69f15ff99c359798ecf277b1",
            "69f15ff12d3edb220618fa98",
            "69f15fe10b0ec9092e0710e8",
            "69f15fcd015b32855c84ec4b",
            "69f0e7992d51f7db4c3c044b",
            "69f0e799272250bd630ce56f",
            "69f0e79790229a3f5e9aeae0",
            "69f0e7962d51f7db4c3c03ff",
            "69f0e795015b32855c66b51c",
            "69f0e7940c5035e037d0fd82",
            "69f0e7942d51f7db4c3c03d1",
            "69f0e794ad53497e680f6890",
            "69f0e7930b0ec9092ee92dc4",
            "69f0e7932d3edb2206fad15d",
            "69f0e793ba0c4eb94396e525",
            "69f0e7622d51f7db4c3bf5b0",
            "69f0e75a7902159455d71f8e",
            "69f0e746d11a08f8e20545d2",
            "69f0e793015b32855c66b4d6",
            "69f0e73b1e2bc6cc4649c80a",
            "69f0e794d11a08f8e2055f24",
            "69f0e7997902159455d73259",
            "69f164bb9c359798ecf28fa8",
            "69f164bb0c5035e037eecb0c",
            "69f164d0ad53497e682dfc4a",
            "69f164c80c5035e037eecb28",
            "69f164bb015b32855c851447",
            "69f1640fad53497e682df931",
            "69f164265b0ad89711eeb0de",
            "69f1600cba0c4eb943b45a6b",
            "69f1601a0c5035e037eeb7f4",
            "69f1600c2d3edb220619039b",
            "69f1600f2d3edb220619042f",
            "69f1600f2d3edb2206190424",
            "69f1600c015b32855c850136",
            "69f15fcfba0c4eb943b447b2",
            "69f15fc52d3edb220618eca4",
            "69f15fbf90229a3f5eb93916",
            "69f15f9d5b0ad89711ee7b8e",
            "69f0e7996b4a7f1e062760e2",
            "69f0e7976b4a7f1e062760a2",
            "69f0e7972d51f7db4c3c040e",
            "69f0e795ba0c4eb94396e56c",
            "69f0e7940c5035e037d0fd71",
            "69f0e7937902159455d731c5",
            "69f0e7932d51f7db4c3c03ae",
            "69f0e7930c5035e037d0fd52",
            "69f0e76fd11a08f8e205549b",
            "69f164bd0b0ec9092e073363",
            "69f164ba0b0ec9092e07334f",
            "69f164bf90229a3f5eb96493",
            "69f164c69c359798ecf28fc6",
            "69f164b9d11a08f8e223708b",
            "69f164c90b0ec9092e073386",
            "69f164bd90229a3f5eb9648e",
            "69f164cbad53497e682dfc40",
            "69f164c0d11a08f8e22370a1",
            "69f164b8ba0c4eb943b46d46",
            "69f164bfba0c4eb943b46d4e",
            "69f16403ad53497e682df769",
            "69f163ebd11a08f8e2236961",
            "69f160142d3edb2206190475",
            "69f1600e90229a3f5eb951d6",
            "69f1600d5b0ad89711ee9ec6",
            "69f160112d3edb2206190451",
            "69f16010ba0c4eb943b45b2e",
            "69f160060b0ec9092e071c9b",
            "69f15fe10b0ec9092e0710e1",
            "69f15fc90b0ec9092e070927",
            "69f0e7996b4a7f1e062760ed",
            "69f0e799ad53497e680f6924",
            "69f0e7982d51f7db4c3c041d",
            "69f0e7977902159455d73222",
            "69f0e796ad53497e680f68d8",
            "69f0e7955b0ad89711d0983a",
            "69f0e795272250bd630ce510",
            "69f0e7956b4a7f1e0627606b",
            "69f0e7932d51f7db4c3c03b4",
            "69f0e793015b32855c66b4e9",
            "69f0e7937902159455d731ad",
            "69f0e7939c359798ecd4794a",
            "69f0e7932d51f7db4c3c03a4",
            "69f0e793ba0c4eb94396e52c",
            "69f0e7922d51f7db4c3c0389",
            "69f0e76890229a3f5e9ade09",
            "69f0e7380b0ec9092ee90f21",
            "69f164c9ad53497e682dfc36",
            "69f164cfad53497e682dfc44",
            "69f164c30c5035e037eecb25",
            "69f164c5015b32855c85146b",
            "69f1640f5b0ad89711eeaeb4",
            "69f1600c0b0ec9092e071e63",
            "69f16016ba0c4eb943b45b7d",
            "69f1600c0c5035e037eeb6dd",
            "69f1600b0c5035e037eeb6bc",
            "69f1600290229a3f5eb94e5a",
            "69f15fe9ad53497e682ddd70",
            "69f15fc190229a3f5eb939ec",
            "69f0e7992d51f7db4c3c044f",
            "69f0e79990229a3f5e9aeb31",
            "69f0e797015b32855c66b555",
            "69f0e7967766680f74c00122",
            "69f0e7950b0ec9092ee92e01",
            "69f0e7946b4a7f1e06276063",
            "69f0e7941e2bc6cc4649e621",
            "69f0e7940c5035e037d0fd7b",
            "69f0e7946b4a7f1e06276054",
            "69f0e7941e2bc6cc4649e613",
            "69f0e7930b0ec9092ee92db4",
            "69f0e7936b4a7f1e0627602d",
            "69f0e793d11a08f8e2055ee7",
            "69f0e7687902159455d7250c",
            "69f0e7666b4a7f1e0627529a",
            "69f164bd9c359798ecf28fb4",
            "69f164c95b0ad89711eeb1ee",
            "69f164cdad53497e682dfc43",
            "69f164caad53497e682dfc3f",
            "69f164bb015b32855c851442",
            "69f1640b2d3edb220619117d",
            "69f164079c359798ecf28b81",
            "69f1600cad53497e682de8ad",
            "69f1600d2d3edb22061903f9",
            "69f15fcd5b0ad89711ee8ab0",
            "69f0e79aad53497e680f6933",
            "69f0e7990b0ec9092ee92e76",
            "69f0e798ad53497e680f68fd",
            "69f0e79690229a3f5e9aead2",
            "69f0e7947766680f74c000f6",
            "69f0e7947766680f74c000f2",
            "69f0e794272250bd630ce4ef",
            "69f0e793015b32855c66b4e2",
            "69f0e793272250bd630ce4d4",
            "69f0e793015b32855c66b4cc",
            "69f0e7930b0ec9092ee92da4",
            "69f0e7847902159455d72e88",
            "69f0e7555b0ad89711d083fb",
            "69f163eb015b32855c850d3b",
            "69f160100b0ec9092e071f0d",
            "69f15fd20c5035e037eea3f1",
            "69f0e7997902159455d7325c",
            "69f0e7997902159455d73252",
            "69f0e7952d51f7db4c3c03e7",
            "69f0e7942d51f7db4c3c03ce",
            "69f0e7937902159455d731b3",
            "69f0e793015b32855c66b4de",
            "69f0e792015b32855c66b4bf",
            "69f0e773272250bd630cdc82",
            "69f164bad11a08f8e223708d",
            "69f16011015b32855c8501fe",
            "69f1600b5b0ad89711ee9e5c",
            "69f0e7976b4a7f1e062760a3",
            "69f0e7956b4a7f1e06276087",
            "69f0e7939c359798ecd4794d",
            "69f0e7931e2bc6cc4649e605",
            "69f164100c5035e037eec804",
            "69f1640b0b0ec9092e072fc7",
            "69f160120b0ec9092e071f38",
            "69f1600e9c359798ecf27e0b",
            "69f1600c015b32855c850132",
            "69f15fd10b0ec9092e070bcc",
            "69f0e7996b4a7f1e062760e8",
            "69f0e7976b4a7f1e062760a8",
            "69f0e797ad53497e680f68e3",
            "69f0e7976b4a7f1e06276099",
            "69f0e7960c5035e037d0fdae",
            "69f0e7959c359798ecd47997",
            "69f0e7950b0ec9092ee92df6",
            "69f0e795ba0c4eb94396e572",
            "69f0e7942d51f7db4c3c03db",
            "69f0e7949c359798ecd4797a",
            "69f0e7949c359798ecd47967",
            "69f0e7937766680f74c000dd",
            "69f0e7930b0ec9092ee92dc3",
            "69f0e7937766680f74c000ae",
            "69f0e7931e2bc6cc4649e5e5",
            "69f0e781015b32855c66b0dc",
            "69f164d9ad53497e682dfc4e",
            "69f164b95b0ad89711eeb1ba",
            "69f1600eba0c4eb943b45adf",
            "69f16001015b32855c84fd7c",
            "69f15ff82d3edb220618fd23",
            "69f0e7987766680f74c00148",
            "69f0e7935b0ad89711d097ff",
            "69f0e7932d3edb2206fad151",
            "69f0e7927766680f74c000a6",
            "69f0e7596b4a7f1e06274dbc",
            "69f0e715272250bd630cbce7",
            "69f164b9ba0c4eb943b46d47",
            "69f164bb015b32855c851444",
            "69f161ca015b32855c85054d",
            "69f1600bad53497e682de889",
            "69f15ff20b0ec9092e07160b",
            "69f0e798ad53497e680f691c",
            "69f0e7982d51f7db4c3c0444",
            "69f0e7970c5035e037d0fdc7",
            "69f0e794015b32855c66b50a",
            "69f0e7940c5035e037d0fd70",
            "69f0e7941e2bc6cc4649e619",
            "69f0e7947766680f74c000e2",
            "69f0e7937902159455d731ab",
            "69f0e7937766680f74c000b9",
            "69f0e7935b0ad89711d097f4",
            "69f0e7777766680f74bff9ab",
            "69f0e72c0b0ec9092ee90ad7",
            "69f0e7789c359798ecd47251",
            "69f164bb0c5035e037eecb12",
            "69f164bb9c359798ecf28fa0",
            "69f164142d3edb22061912da",
            "69f16285ad53497e682df1a3",
            "69f1600dd11a08f8e2235ebe",
            "69f15fd85b0ad89711ee8e00",
            "69f0e794015b32855c66b4f9",
            "69f0e7750b0ec9092ee92566",
            "69f16002015b32855c84fdeb",
            "69f1640a90229a3f5eb9605f",
            "69f1640290229a3f5eb95f7e",
            "69f0e798272250bd630ce561",
            "69f0e797015b32855c66b551",
            "69f0e7946b4a7f1e0627605f",
            "69f0e79390229a3f5e9aea6c",
            "69f164caad53497e682dfc3d",
            "69f164d69c359798ecf28fcb",
            "69f164c9015b32855c851473",
            "69f164cb015b32855c851477",
            "69f164c20c5035e037eecb23",
            "69f164c65b0ad89711eeb1e5",
            "69f164d0ad53497e682dfc48",
            "69f164c60b0ec9092e07337f",
            "69f164caad53497e682dfc3e",
            "69f164d2ad53497e682dfc4b",
            "69f164d20b0ec9092e073390",
            "69f164c090229a3f5eb96495",
            "69f164d0ad53497e682dfc49",
            "69f164c15b0ad89711eeb1df",
            "69f164b95b0ad89711eeb1b7",
            "69f164bc5b0ad89711eeb1cf",
            "69f164135b0ad89711eeaf16",
            "69f1640990229a3f5eb9603d",
            "69f16408ba0c4eb943b469a4",
            "69f16400ad53497e682df713",
            "69f163fc2d3edb2206190f8e",
            "69f163f3ba0c4eb943b46723",
            "69f163eb0b0ec9092e072c65",
            "69f163e89c359798ecf28858",
            "69f163e20c5035e037eec27e",
            "69f163e22d3edb2206190ced",
            "69f164c05b0ad89711eeb1da",
            "69f1641e0c5035e037eeca5f",
            "69f164220c5035e037eeca86",
            "69f1641e0c5035e037eeca56",
            "69f163e00b0ec9092e072b6b",
            "69f1621aad53497e682df121",
            "69f162000c5035e037eebf59",
            "69f161d4ba0c4eb943b45f9d",
            "69f1600d0c5035e037eeb728",
            "69f1602c2d3edb22061904c6",
            "69f1601290229a3f5eb9522f",
            "69f15f86ad53497e682dbd18",
            "69f160142d3edb220619047a",
            "69f1600cad53497e682de8c4",
            "69f1600bba0c4eb943b45a38",
            "69f160099c359798ecf27ceb",
            "69f160075b0ad89711ee9cc6",
            "69f160052d3edb220619016d",
            "69f160030c5035e037eeb3f9",
            "69f15fff90229a3f5eb94d61",
            "69f15ffeba0c4eb943b455ea",
            "69f15ff90b0ec9092e071867",
            "69f15fee2d3edb220618f9bc",
            "69f15fecad53497e682dde1a",
            "69f15fe3015b32855c84f391",
            "69f15fd35b0ad89711ee8c50",
            "69f15fced11a08f8e2234a95",
            "69f15fad2d3edb220618e4af",
            "69f0e799d11a08f8e2055fab",
            "69f0e798d11a08f8e2055fa6",
            "69f0e7980b0ec9092ee92e34",
            "69f0e797d11a08f8e2055f5e",
            "69f0e7960c5035e037d0fdb3",
            "69f0e7967902159455d731fe",
            "69f0e795ad53497e680f68c3",
            "69f0e795ad53497e680f68bd",
            "69f0e795ad53497e680f68ad",
            "69f0e7956b4a7f1e06276071",
            "69f0e794272250bd630ce4f4",
            "69f0e794015b32855c66b4fb",
            "69f0e794ba0c4eb94396e54a",
            "69f0e793272250bd630ce4eb",
            "69f0e7937902159455d731c1",
            "69f0e793d11a08f8e2055f08",
            "69f0e7935b0ad89711d09806",
            "69f0e7939c359798ecd47953",
            "69f0e79390229a3f5e9aea76",
            "69f0e793ad53497e680f6879",
            "69f0e7939c359798ecd4794b",
            "69f0e7935b0ad89711d097fa",
            "69f0e793ba0c4eb94396e52f",
            "69f0e7932d3edb2206fad157",
            "69f0e793272250bd630ce4c4",
            "69f0e7930c5035e037d0fd3c",
            "69f0e7932d51f7db4c3c0390",
            "69f0e7932d3edb2206fad14d",
            "69f0e7930c5035e037d0fd37",
            "69f0e79390229a3f5e9aea5d",
            "69f0e7932d3edb2206fad14c",
            "69f0e7937766680f74c000aa",
            "69f0e78bad53497e680f66cd",
            "69f0e7696b4a7f1e062753b5",
            "69f0e764015b32855c66a6e2",
            "69f0e75a0b0ec9092ee91b61",
            "69f0e7462d51f7db4c3bec24",
            "69f0e7932d51f7db4c3c03ac",
            "69f164bcba0c4eb943b46d4a",
            "69f164bd015b32855c851451",
            "69f1600e2d3edb2206190417",
            "69f15ff790229a3f5eb94acb",
            "69f0e793d11a08f8e2055f10",
            "69f0e7937766680f74c000d6",
            "69f0e793d11a08f8e2055f04",
            "69f0e77b2d51f7db4c3bfdfb",
            "69f15ffe2d3edb220618fef5",
            "69f0e7930b0ec9092ee92daf",
            "69f164e0ad53497e682dfc51",
            "69f164ce015b32855c851479",
            "69f164c70b0ec9092e073380",
            "69f164cdba0c4eb943b46d57",
            "69f164c9015b32855c851471",
            "69f164d1015b32855c85147d",
            "69f164c99c359798ecf28fc8",
            "69f164c75b0ad89711eeb1e9",
            "69f164be5b0ad89711eeb1d6",
            "69f164c62d3edb2206191565",
            "69f164bb90229a3f5eb96482",
            "69f164c9ad53497e682dfc38",
            "69f164c290229a3f5eb9649b",
            "69f164bb90229a3f5eb96480",
            "69f164bf90229a3f5eb96492",
            "69f164c690229a3f5eb964a3",
            "69f164c9ad53497e682dfc30",
            "69f164cbad53497e682dfc41",
            "69f164c00c5035e037eecb1f",
            "69f164d0ad53497e682dfc47",
            "69f164c60b0ec9092e07337c",
            "69f164c30b0ec9092e07337b",
            "69f164cf90229a3f5eb964ad",
            "69f164b9ba0c4eb943b46d48",
            "69f164ba5b0ad89711eeb1c5",
            "69f164bd5b0ad89711eeb1d1",
            "69f164b92d3edb2206191554",
            "69f1649f2d3edb22061914e7",
            "69f164c6015b32855c85146d",
            "69f164bbad53497e682dfc04",
            "69f1641eba0c4eb943b46c60",
            "69f1641c015b32855c8512f1",
            "69f1641bd11a08f8e2236ef8",
            "69f1641b0b0ec9092e0731ef",
            "69f164180c5035e037eec96e",
            "69f164175b0ad89711eeafb1",
            "69f164175b0ad89711eeaf9a",
            "69f16414ad53497e682df9c8",
            "69f164130c5035e037eec88d",
            "69f164122d3edb2206191281",
            "69f164110b0ec9092e073090",
            "69f164119c359798ecf28cb5",
            "69f1640f2d3edb2206191220",
            "69f1640f0c5035e037eec7ea",
            "69f1640d90229a3f5eb960b6",
            "69f1640c0c5035e037eec77a",
            "69f16408015b32855c851074",
            "69f164080c5035e037eec6c5",
            "69f164065b0ad89711eead73",
            "69f164060b0ec9092e072f20",
            "69f164059c359798ecf28b44",
            "69f16402015b32855c850fad",
            "69f164020b0ec9092e072ec6",
            "69f16400015b32855c850f7e",
            "69f163fead53497e682df6e3",
            "69f163fd0b0ec9092e072e42",
            "69f163fc9c359798ecf28a2b",
            "69f163fbba0c4eb943b467f7",
            "69f163fad11a08f8e2236ae8",
            "69f163f95b0ad89711eeabed",
            "69f163f8015b32855c850e91",
            "69f163f72d3edb2206190ee5",
            "69f163f42d3edb2206190ea0",
            "69f163f10b0ec9092e072d01",
            "69f163f10b0ec9092e072cfa",
            "69f163f190229a3f5eb95dcb",
            "69f163f15b0ad89711eeab0f",
            "69f163edd11a08f8e223698f",
            "69f163ec5b0ad89711eeaa9a",
            "69f163eb0c5035e037eec34a",
            "69f163e6015b32855c850cc9",
            "69f163e62d3edb2206190d38",
            "69f163e15b0ad89711eea987",
            "69f164bf2d3edb220619155e",
            "69f164ba90229a3f5eb9647b",
            "69f1641eba0c4eb943b46c65",
            "69f1641dd11a08f8e2236f38",
            "69f16421d11a08f8e2236f7f",
            "69f16423d11a08f8e2236f95",
            "69f1641d015b32855c85130c",
            "69f1641f9c359798ecf28e96",
            "69f16425d11a08f8e2236f99",
            "69f1641e015b32855c851332",
            "69f1641c015b32855c8512fb",
            "69f1641cd11a08f8e2236f2e",
            "69f1627eba0c4eb943b46367",
            "69f16238015b32855c850a18",
            "69f163dfad53497e682df3c5",
            "69f163dfad53497e682df3bf",
            "69f163de5b0ad89711eea94d",
            "69f163dd0b0ec9092e072b3c",
            "69f163dc9c359798ecf28774",
            "69f163db2d3edb2206190c6e",
            "69f1622dad53497e682df150",
            "69f162370c5035e037eec05e",
            "69f16219015b32855c8509cb",
            "69f1621a5b0ad89711eea677",
            "69f161de5b0ad89711eea3fc",
            "69f162170c5035e037eec010",
            "69f1621690229a3f5eb95993",
            "69f161dc5b0ad89711eea3af",
            "69f161daad53497e682dee27",
            "69f161d50c5035e037eebc82",
            "69f161d5ba0c4eb943b45fb0",
            "69f161d42d3edb2206190765",
            "69f161d4d11a08f8e223629a",
            "69f161d29c359798ecf281fd",
            "69f161d2015b32855c850611",
            "69f161d19c359798ecf281d6",
            "69f161ce015b32855c8505a9",
            "69f161c70b0ec9092e07232f",
            "69f160339c359798ecf27ed2",
            "69f160305b0ad89711ee9fbe",
            "69f1600c0c5035e037eeb702",
            "69f1600ed11a08f8e2235ef1",
            "69f1601bba0c4eb943b45ba7",
            "69f160150b0ec9092e071f5f",
            "69f15f8cba0c4eb943b43347",
            "69f15f629c359798ecf2463f",
            "69f15f55d11a08f8e223249a",
            "69f1600ed11a08f8e2235ee3",
            "69f1600cd11a08f8e2235e86",
            "69f1600c2d3edb22061903a9",
            "69f1602c0c5035e037eeb818",
            "69f160102d3edb2206190441",
            "69f16012015b32855c85020e",
            "69f1600d015b32855c8501a5",
            "69f160139c359798ecf27e6f",
            "69f15f500c5035e037ee7a0d",
            "69f16009ad53497e682de7f3",
            "69f160099c359798ecf27ce3",
            "69f1600990229a3f5eb9507e",
            "69f16008ba0c4eb943b45950",
            "69f16008d11a08f8e2235d55",
            "69f16007015b32855c84ffa2",
            "69f16007ba0c4eb943b458c4",
            "69f160060b0ec9092e071cb6",
            "69f16006d11a08f8e2235caa",
            "69f160069c359798ecf27bd2",
            "69f16004015b32855c84febb",
            "69f16003ba0c4eb943b45798",
            "69f160020b0ec9092e071b52",
            "69f160019c359798ecf27a68",
            "69f160010b0ec9092e071b13",
            "69f16001ad53497e682de520",
            "69f16001ba0c4eb943b456e8",
            "69f160009c359798ecf27a04",
            "69f15fffad53497e682de4a3",
            "69f15fff5b0ad89711ee9a84",
            "69f15ffd90229a3f5eb94d07",
            "69f15ffb015b32855c84fb78",
            "69f15ffa90229a3f5eb94bcc",
            "69f15ff9015b32855c84faf1",
            "69f15ff8ba0c4eb943b4545f",
            "69f15ff8015b32855c84fa70",
            "69f15ff80b0ec9092e07181d",
            "69f15ff62d3edb220618fc6e",
            "69f15ff50c5035e037eeaf21",
            "69f15ff59c359798ecf2765e",
            "69f15ff2d11a08f8e223565e",
            "69f15ff25b0ad89711ee964d",
            "69f15ff2ba0c4eb943b4528e",
            "69f15ff22d3edb220618fb0d",
            "69f15ff10c5035e037eeade7",
            "69f15ff0ad53497e682ddfb3",
            "69f15ff00c5035e037eead81",
            "69f15fefd11a08f8e2235576",
            "69f15feeba0c4eb943b45112",
            "69f15fec5b0ad89711ee9464",
            "69f15feb9c359798ecf27368",
            "69f15fea9c359798ecf272fe",
            "69f15fe89c359798ecf2727d",
            "69f15fe790229a3f5eb945a5",
            "69f15fe7d11a08f8e22352b4",
            "69f15fe40b0ec9092e0711bd",
            "69f15fe40c5035e037eea992",
            "69f15fe32d3edb220618f654",
            "69f15fe3015b32855c84f384",
            "69f15fe29c359798ecf2709f",
            "69f15fe2d11a08f8e223514b",
            "69f15fe10b0ec9092e0710dc",
            "69f15fe19c359798ecf2702c",
            "69f15fe0d11a08f8e223509d",
            "69f15fe0ad53497e682dda63",
            "69f15fdf015b32855c84f238",
            "69f15fdf0c5035e037eea841",
            "69f15fdf0c5035e037eea824",
            "69f15fde0c5035e037eea80c",
            "69f15fdd0c5035e037eea789",
            "69f15fddba0c4eb943b44bd1",
            "69f15fdc2d3edb220618f3eb",
            "69f15fdbba0c4eb943b44b68",
            "69f15fdbd11a08f8e2234ea4",
            "69f15fda90229a3f5eb941a3",
            "69f15fda0b0ec9092e070ea2",
            "69f15fda0b0ec9092e070ea1",
            "69f15fd6d11a08f8e2234d44",
            "69f15fd45b0ad89711ee8cbd",
            "69f15fd39c359798ecf26bae",
            "69f15fd3015b32855c84ee16",
            "69f15fd2015b32855c84ee02",
            "69f15fd20c5035e037eea418",
            "69f15fce0b0ec9092e070a9a",
            "69f15fcd2d3edb220618ef35",
            "69f15fcc5b0ad89711ee8a4e",
            "69f15fca0b0ec9092e070976",
            "69f15fc9015b32855c84eb03",
            "69f15fc80c5035e037eea0ec",
            "69f15fc8ba0c4eb943b4456b",
            "69f15fc70b0ec9092e0708b5",
            "69f15fc65b0ad89711ee8895",
            "69f15fc60c5035e037eea041",
            "69f15fc6015b32855c84e9e6",
            "69f15fc4015b32855c84e993",
            "69f15fc4d11a08f8e223472e",
            "69f15fc40b0ec9092e0707b7",
            "69f15fc49c359798ecf2667d",
            "69f15fc30c5035e037ee9f25",
            "69f15fc25b0ad89711ee8733",
            "69f15fc1ad53497e682dd056",
            "69f15fc05b0ad89711ee8688",
            "69f15fbe015b32855c84e75e",
            "69f15fbdad53497e682dceed",
            "69f15fb59c359798ecf261af",
            "69f15fb52d3edb220618e740",
            "69f15fb59c359798ecf261a2",
            "69f15fb0ba0c4eb943b43e36",
            "69f15f939c359798ecf256bb",
            "69f0e799d11a08f8e2055fbd",
            "69f0e7992d51f7db4c3c0456",
            "69f0e7992d51f7db4c3c0454",
            "69f0e799d11a08f8e2055faf",
            "69f0e798ad53497e680f690f",
            "69f0e798272250bd630ce562",
            "69f0e798ad53497e680f690c",
            "69f0e79890229a3f5e9aeb11",
            "69f0e798d11a08f8e2055fa0",
            "69f0e7986b4a7f1e062760cf",
            "69f0e7987902159455d7323f",
            "69f0e7986b4a7f1e062760c9",
            "69f0e797015b32855c66b558",
            "69f0e7977902159455d73220",
            "69f0e79790229a3f5e9aeadd",
            "69f0e7979c359798ecd479c9",
            "69f0e7976b4a7f1e0627609c",
            "69f0e797272250bd630ce533",
            "69f0e7977902159455d73215",
            "69f0e7970b0ec9092ee92e1b",
            "69f0e797d11a08f8e2055f60",
            "69f0e7977902159455d73212",
            "69f0e796272250bd630ce52b",
            "69f0e7967766680f74c00129",
            "69f0e796ad53497e680f68d6",
            "69f0e79690229a3f5e9aeac5",
            "69f0e7962d51f7db4c3c03f7",
            "69f0e7959c359798ecd4799f",
            "69f0e795d11a08f8e2055f3b",
            "69f0e795ba0c4eb94396e574",
            "69f0e7956b4a7f1e06276072",
            "69f0e7957902159455d731e4",
            "69f0e7957766680f74c00102",
            "69f0e794ba0c4eb94396e564",
            "69f0e7940c5035e037d0fd8a",
            "69f0e794ad53497e680f68a5",
            "69f0e7942d3edb2206fad194",
            "69f0e794ba0c4eb94396e55b",
            "69f0e794272250bd630ce4f9",
            "69f0e7942d3edb2206fad18c",
            "69f0e7940c5035e037d0fd77",
            "69f0e7945b0ad89711d09826",
            "69f0e7942d3edb2206fad189",
            "69f0e7940b0ec9092ee92dcd",
            "69f0e794015b32855c66b4fd",
            "69f0e7947766680f74c000e4",
            "69f0e7940b0ec9092ee92dcb",
            "69f0e79490229a3f5e9aea86",
            "69f0e794015b32855c66b4f7",
            "69f0e7947902159455d731c7",
            "69f0e793d11a08f8e2055f0f",
            "69f0e7932d3edb2206fad178",
            "69f0e7932d51f7db4c3c03be",
            "69f0e793d11a08f8e2055f0e",
            "69f0e7939c359798ecd4795f",
            "69f0e793ba0c4eb94396e544",
            "69f0e7937902159455d731bc",
            "69f0e7935b0ad89711d0980e",
            "69f0e7937766680f74c000cf",
            "69f0e7937902159455d731b8",
            "69f0e7937766680f74c000cd",
            "69f0e793ba0c4eb94396e53f",
            "69f0e793ba0c4eb94396e53d",
            "69f0e7932d51f7db4c3c03b0",
            "69f0e793272250bd630ce4d9",
            "69f0e7936b4a7f1e06276038",
            "69f0e793015b32855c66b4e5",
            "69f0e793d11a08f8e2055f00",
            "69f0e7932d3edb2206fad161",
            "69f0e79390229a3f5e9aea71",
            "69f0e793015b32855c66b4e0",
            "69f0e7932d3edb2206fad15f",
            "69f0e793015b32855c66b4da",
            "69f0e7930b0ec9092ee92db1",
            "69f0e7930c5035e037d0fd4f",
            "69f0e7935b0ad89711d097f9",
            "69f0e7936b4a7f1e06276031",
            "69f0e7930c5035e037d0fd4b",
            "69f0e7936b4a7f1e0627602f",
            "69f0e7935b0ad89711d097f0",
            "69f0e7937902159455d7319e",
            "69f0e793015b32855c66b4cb",
            "69f0e79390229a3f5e9aea61",
            "69f0e7937902159455d7319a",
            "69f0e7937766680f74c000af",
            "69f0e7931e2bc6cc4649e5eb",
            "69f0e7930b0ec9092ee92d9f",
            "69f0e7931e2bc6cc4649e5e9",
            "69f0e7931e2bc6cc4649e5e8",
            "69f0e7937766680f74c000ac",
            "69f0e7930b0ec9092ee92d9d",
            "69f0e7935b0ad89711d097e4",
            "69f0e7932d3edb2206fad14f",
            "69f0e7932d51f7db4c3c0393",
            "69f0e7930c5035e037d0fd3b",
            "69f0e7930c5035e037d0fd3a",
            "69f0e7932d51f7db4c3c0392",
            "69f0e7932d51f7db4c3c0391",
            "69f0e7932d3edb2206fad14e",
            "69f0e7936b4a7f1e06276026",
            "69f0e793ad53497e680f6865",
            "69f0e7937766680f74c000ab",
            "69f0e7936b4a7f1e06276025",
            "69f0e7932d51f7db4c3c038e",
            "69f0e7936b4a7f1e06276024",
            "69f0e793015b32855c66b4c7",
            "69f0e7939c359798ecd47938",
            "69f0e7935b0ad89711d097de",
            "69f0e7935b0ad89711d097dd",
            "69f0e7937902159455d73190",
            "69f0e7936b4a7f1e06276021",
            "69f0e79390229a3f5e9aea5c",
            "69f0e7920c5035e037d0fd35",
            "69f0e7921e2bc6cc4649e5e0",
            "69f0e792015b32855c66b4bc",
            "69f0e78f272250bd630ce425",
            "69f0e78d7766680f74bfff8f",
            "69f0e78d272250bd630ce389",
            "69f0e787272250bd630ce259",
            "69f0e7831e2bc6cc4649e29f",
            "69f0e78390229a3f5e9ae711",
            "69f0e7839c359798ecd475d7",
            "69f0e782ba0c4eb94396e1aa",
            "69f0e779d11a08f8e2055812",
            "69f0e779272250bd630cde61",
            "69f0e7762d51f7db4c3bfc4c",
            "69f0e7747902159455d7294f",
            "69f0e7716b4a7f1e062756c4",
            "69f0e7696b4a7f1e06275384",
            "69f0e7686b4a7f1e0627536a",
            "69f0e7657902159455d723ae",
            "69f0e76490229a3f5e9adc85",
            "69f0e7632d51f7db4c3bf5d4",
            "69f0e7632d51f7db4c3bf5ba",
            "69f0e762272250bd630cd675",
            "69f0e7600b0ec9092ee91dca",
            "69f0e75c6b4a7f1e06274ef4",
            "69f0e756272250bd630cd2ce",
            "69f0e7532d3edb2206fabc1f",
            "69f0e74d7902159455d71abc",
            "69f0e74a272250bd630cced9",
            "69f0e74690229a3f5e9ad211",
            "69f0e7445b0ad89711d07d75",
            "69f0e7440b0ec9092ee9135b",
            "69f0e72d90229a3f5e9ac8e1",
            "69f0e72a2d51f7db4c3be29c",
            "69f0e7287766680f74bfdda5",
            "69f0e7170b0ec9092ee90301",
            "69f0e79990229a3f5e9aeb1f",
            "69f164bf5b0ad89711eeb1d9",
            "69f0e793272250bd630ce4cc",
            "69f0e796015b32855c66b52b",
            "69f0e7932d51f7db4c3c03a3",
            "69f0e7932d51f7db4c3c03b8",
            "69f164c20c5035e037eecb24",
            "69f164180c5035e037eec97e",
            "69f163df0b0ec9092e072b61",
            "69f0e793272250bd630ce4d5",
            "69f0e7969c359798ecd479a1",
            "69f0e7950c5035e037d0fda8",
            "69f0e7936b4a7f1e0627603e",
            "69f0e7936b4a7f1e06276034",
            "69f0e7937902159455d7319c",
            "69f0e7795b0ad89711d090fd",
            "69f0e7477766680f74bfe8e2",
            "69f16018ba0c4eb943b45b9a",
            "69f0e797015b32855c66b544",
            "69f0e794ba0c4eb94396e54d",
            "69f0e793272250bd630ce4d1",
            "69f0e7357902159455d711cc",
            "69f164c9ad53497e682dfc33",
            "69f0e793ba0c4eb94396e531",
            "69f0e793d11a08f8e2055efb",
            "69f0e7931e2bc6cc4649e5ec",
            "69f164012d3edb220619102a",
            "69f0e796ba0c4eb94396e584",
            "69f0e7930b0ec9092ee92d9a",
            "69f161c89c359798ecf280ff",
            "69f0e794ad53497e680f68a7",
            "69f0e7939c359798ecd47951",
            "69f0e72f272250bd630cc599",
            "69f1641c2d3edb22061913fa",
            "69f15fb22d3edb220618e620",
            "69f0e7932d3edb2206fad179",
            "69f15ff99c359798ecf277a8",
            "69f0e795ba0c4eb94396e576",
            "69f0e79890229a3f5e9aeb08",
            "69f0e79890229a3f5e9aeafa",
            "69f0e7969c359798ecd479ba",
            "69f15ffa0b0ec9092e0718db",
            "69f0e7967902159455d73200",
            "69f0e7936b4a7f1e0627603c",
            "69f0e793d11a08f8e2055ef3",
            "69f1641c0c5035e037eeca1a",
            "69f0e79390229a3f5e9aea60",
            "69f0e77d2d51f7db4c3bfe92",
            "69f0e70b90229a3f5e9abc88",
            "69f15fe52d3edb220618f6de",
            "69f0e79990229a3f5e9aeb3e",
            "69f164c10b0e0780e59e79fa",
            "69f164c00b0e0780e59e79f9",
            "69f164bd0b0e0780e59e79f7",
            "69f164b82d73f0f7149f6083",
            "69f1641c0b0e0780e59e79dd",
            "69f1641c2d73f0f7149f606c",
            "69f164130b0e0780e59e79c0",
            "69f1640a2d73f0f7149f6022",
            "69f164010b0e0780e59e798c",
            "69f163fa2d73f0f7149f5fed",
            "69f163f22d73f0f7149f5fda",
            "69f164bc2d73f0f7149f6084",
            "69f164be0b0e0780e59e79f8",
            "69f164c20b0e0780e59e79fb",
            "69f164240b0e0780e59e79e9",
            "69f1641c0b0e0780e59e79de",
            "69f162792d73f0f7149f5f71",
            "69f1600c0b0e0780e59e784b",
            "69f1600c0b0e0780e59e7852",
            "69f160300b0e0780e59e786e",
            "69f1601c2d73f0f7149f5ea9",
            "69f1600b0b0e0780e59e7848",
            "69f1600d2d73f0f7149f5e92",
            "69f1600b0b0e0780e59e7846",
            "69f160050b0e0780e59e7813",
            "69f15ff80b0e0780e59e77a4",
            "69f15f752d73f0f7149f59b9",
            "69f1600d2d73f0f7149f5e8e",
            "69f1600c2d73f0f7149f5e8c",
            "69f1600e0b0e0780e59e785d",
            "69f1600c2d73f0f7149f5e86",
            "69f1600c2d73f0f7149f5e85",
            "69f1600c2d73f0f7149f5e84",
            "69f160050b0e0780e59e7816",
            "69f15ffd2d73f0f7149f5e23",
            "69f15ffc2d73f0f7149f5e11",
            "69f15ff80b0e0780e59e77a5",
            "69f15ff72d73f0f7149f5df4",
            "69f15fde2d73f0f7149f5d1f",
            "69f15fd72d73f0f7149f5ce3",
            "69f15fc42d73f0f7149f5c3f",
            "69f15fb42d73f0f7149f5bac",
            "69f15fb22d73f0f7149f5b9d",
            "69f0e7990b0e0780e59a3173",
            "69f0e7960b0e0780e59a3167",
            "69f0e7950b0e0780e59a3164",
            "69f0e7950b0e0780e59a315e",
            "69f0e7940b0e0780e59a315c",
            "69f0e7940b0e0780e59a3159",
            "69f0e7942d73f0f7149b566b",
            "69f0e7930b0e0780e59a3157",
            "69f0e7930b0e0780e59a3156",
            "69f0e7932d73f0f7149b5666",
            "69f0e7932d73f0f7149b5664",
            "69f0e7932d73f0f7149b5660",
            "69f0e7820b0e0780e59a3095",
            "69f0e76d0b0e0780e59a2f19",
            "69f0e76b0b0e0780e59a2eea",
            "69f0e7620b0e0780e59a2e47",
            "69f0e7440b0e0780e59a2bde",
            "69f0e73a0b0e0780e59a2b31",
            "69f0e7282d73f0f7149b4f0a",
            "69f0e70c0b0e0780e59a27f3",
            "69f15ff50b0ec9092e07172d",
            "69f15fb42d3edb220618e707",
            "69f0e7979c359798ecd479d5",
            "69f0e7942d51f7db4c3c03dd",
            "69f0e793015b32855c66b4f3",
            "69f0e792272250bd630ce4bb",
            "69f0e7922d51f7db4c3c0387",
            "69f0e7480b0ec9092ee914f0",
            "69f0e793ad53497e680f6880",
            "69f164bb9c359798ecf28fa3",
            "69f164c0ad53497e682dfc18",
            "69f1600c5b0ad89711ee9e71",
            "69f15fc49c359798ecf26691",
            "69f0e793272250bd630ce4e7",
            "69f0e79390229a3f5e9aea7f",
            "69f0e792015b32855c66b4ba",
            "69f0e7936b4a7f1e06276044",
        };

        var setting = ConfigManager.OpenObserveSetting;
        using var httpClient = new HttpClient();
        var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{setting.Username}:{setting.Password}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        var startMicros = new DateTimeOffset(2026, 4, 28, 16, 55, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() * 1000;
        var endMicros = new DateTimeOffset(2026, 4, 29, 2, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() * 1000;

        var outputPath = Path.Combine(AppContext.BaseDirectory, $"checklog_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");
        using var writer = new StreamWriter(outputPath, append: false, encoding: System.Text.Encoding.UTF8);
        var writeLock = new object();

        void WriteLine(string line) { Console.WriteLine(line); lock (writeLock) writer.WriteLine(line); }

        WriteLine($"=== CheckLog 開始，共 {data.Count} 筆 ===");

        // 每 30 筆合成一條 OR 查詢，8 個並發，避免 1297 次獨立掃描
        // str_match 走全文索引，比 LIKE '%...%' 快；size=batchSize 因為只需確認 exists
        const int batchSize = 30;
        const int maxConcurrent = 3;
        var semaphore  = new SemaphoreSlim(maxConcurrent);
        var foundCount = 0;
        var notFoundCount = 0;

        var tasks = data.Chunk(batchSize).Select(async batch =>
        {
            await semaphore.WaitAsync();
            try
            {
                var orClauses = string.Join(" OR ", batch.Select(id => $"str_match(raw_message, '{id}')"));
                var sql = $"SELECT raw_message FROM {setting.Stream} WHERE match_all('Request URI') AND ({orClauses})";
                var requestBody = System.Text.Json.JsonSerializer.Serialize(new
                {
                    query = new { sql, start_time = startMicros, end_time = endMicros, from = 0, size = batchSize }
                });

                var response     = await httpClient.PostAsync(
                    $"{setting.BaseUrl}/api/{setting.Org}/_search",
                    new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lock (writeLock)
                        WriteLine($"[SKIP] batch={string.Join(",", batch.Take(3))}... HTTP {(int)response.StatusCode} {response.StatusCode}");
                    return;
                }

                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    var allMessages = root.TryGetProperty("hits", out var hitsEl) && hitsEl.ValueKind == System.Text.Json.JsonValueKind.Array
                        ? hitsEl.EnumerateArray()
                                .Select(h => h.TryGetProperty("raw_message", out var r) ? r.GetString() : null)
                                .Where(m => m != null)
                                .ToList()
                        : [];

                    // 批次完成後立即印出，lock 確保多行不交錯
                    lock (writeLock)
                    {
                        foreach (var id in batch)
                        {
                            if (allMessages.Any(m => m!.Contains(id)))
                            {
                                Interlocked.Increment(ref foundCount);
                                WriteLine($"[{id}] FOUND");
                            }
                            else
                            {
                                Interlocked.Increment(ref notFoundCount);
                                WriteLine($"[{id}] NOT FOUND");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (writeLock)
                        WriteLine($"[SKIP] batch={string.Join(",", batch.Take(3))}... 解析失敗：{ex.Message}");
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        Task.WhenAll(tasks).GetAwaiter().GetResult();

        WriteLine($"\n=== 統計：有 {foundCount} 筆找到，{notFoundCount} 筆未找到 ===");

        Console.WriteLine($"\n已匯出至：{outputPath}");
    }

    public void GetLog()
    {
        var data = new List<string>
        {
            //"69f15fcd5b0ad89711ee8a94",
            "69f15fd290229a3f5eb93f2a",
            //"69f15fe1015b32855c84f2e1",
            //"69f1600b5b0ad89711ee9e58",
            //"69f1600bad53497e682de898",
            //"69f160129c359798ecf27e60"
        };

        var setting = ConfigManager.OpenObserveSetting;
        using var httpClient = new HttpClient();
        var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{setting.Username}:{setting.Password}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        var startMicros = new DateTimeOffset(2026, 4, 28, 16, 55, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() * 1000;
        var endMicros = new DateTimeOffset(2026, 4, 29, 2, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() * 1000;

        var outputPath = Path.Combine(AppContext.BaseDirectory, $"checklog_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");
        using var writer = new StreamWriter(outputPath, append: false, encoding: System.Text.Encoding.UTF8);
        var writeLock = new object();

        void WriteLine(string line) { Console.WriteLine(line); lock (writeLock) writer.WriteLine(line); }

        WriteLine($"=== GetLog 開始，共 {data.Count} 筆 ===");

        const int batchSize     = 30;
        const int maxConcurrent = 3;
        var semaphore     = new SemaphoreSlim(maxConcurrent);
        var foundCount    = 0;
        var notFoundCount = 0;

        var tasks = data.Chunk(batchSize).Select(async batch =>
        {
            await semaphore.WaitAsync();
            try
            {
                var orClauses   = string.Join(" OR ", batch.Select(id => $"str_match(raw_message, '{id}')"));
                var sql         = $"SELECT raw_message FROM {setting.Stream} WHERE ({orClauses})";  //(match_all('Request URI') OR match_all('Response URI')) AND 
                var requestBody = System.Text.Json.JsonSerializer.Serialize(new
                {
                    query = new { sql, start_time = startMicros, end_time = endMicros, from = 0, size = batchSize * 10 }
                });

                var response     = await httpClient.PostAsync(
                    $"{setting.BaseUrl}/api/{setting.Org}/_search",
                    new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lock (writeLock)
                        WriteLine($"[SKIP] batch={string.Join(",", batch.Take(3))}... HTTP {(int)response.StatusCode} {response.StatusCode}");
                    return;
                }

                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    var allMessages = root.TryGetProperty("hits", out var hitsEl) && hitsEl.ValueKind == System.Text.Json.JsonValueKind.Array
                        ? hitsEl.EnumerateArray()
                                .Select(h => h.TryGetProperty("raw_message", out var r) ? r.GetString() : null)
                                .Where(m => m != null)
                                .ToList()
                        : [];

                    lock (writeLock)
                    {
                        foreach (var id in batch)
                        {
                            var matching = allMessages.Where(m => m!.Contains(id)).ToList();
                            if (matching.Count > 0)
                            {
                                Interlocked.Increment(ref foundCount);
                                WriteLine($"[{id}] {matching.Count} 筆");
                                foreach (var msg in matching)
                                    WriteLine(FormatLogMessage(msg!));
                            }
                            else
                            {
                                Interlocked.Increment(ref notFoundCount);
                                WriteLine($"[{id}] NOT FOUND");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (writeLock)
                        WriteLine($"[SKIP] batch={string.Join(",", batch.Take(3))}... 解析失敗：{ex.Message}");
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        Task.WhenAll(tasks).GetAwaiter().GetResult();

        WriteLine($"\n=== 統計：有 {foundCount} 筆找到，{notFoundCount} 筆未找到 ===");

        Console.WriteLine($"\n已匯出至：{outputPath}");
    }

    // raw_message 結構：外層 JSON → log 欄位含 ANSI code + 內層 JSON → value.Message 又是 JSON
    private static string FormatLogMessage(string rawMessage)
    {
        try
        {
            using var outerDoc = System.Text.Json.JsonDocument.Parse(rawMessage);
            var outer = outerDoc.RootElement;

            if (!outer.TryGetProperty("log", out var logEl)) return rawMessage;
            var logStr = logEl.GetString() ?? rawMessage;

            // 移除 ANSI color code（e.g. [33m ... [0m）
            logStr = Regex.Replace(logStr, @"\x1b\[[0-9;]*m", "");

            var jsonStart = logStr.IndexOf('{');
            if (jsonStart < 0) return logStr.Trim();

            using var innerDoc = System.Text.Json.JsonDocument.Parse(logStr[jsonStart..]);
            var inner = innerDoc.RootElement;

            if (!inner.TryGetProperty("value", out var valueEl)) return logStr.Trim();

            var sb = new StringBuilder();

            var logLevel = valueEl.TryGetProperty("LogLevel",   out var lvl) ? lvl.GetString() : "";
            var app      = valueEl.TryGetProperty("Application", out var ap) ? ap.GetString()  : "";
            var ts       = valueEl.TryGetProperty("Timestamp",   out var t)  ? t.GetString()   : "";
            sb.AppendLine($"  [{ts}] {logLevel} {app}");

            if (valueEl.TryGetProperty("Message", out var msgEl))
            {
                var msgStr = msgEl.GetString();
                if (msgStr != null)
                {
                    using var msgDoc = System.Text.Json.JsonDocument.Parse(msgStr);
                    foreach (var prop in msgDoc.RootElement.EnumerateObject())
                    {
                        if (prop.Name == "access_token") continue;
                        sb.AppendLine($"    {prop.Name,-15}: {prop.Value}");
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }
        catch
        {
            return rawMessage;
        }
    }
}

/// <summary>
/// Ledger 相關服務介面
/// </summary>
public interface ILedgerService
{
    void GetHasLederButNotLog();
    void CheckLog();
    void GetLog();
}
