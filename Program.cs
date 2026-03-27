using Tool;
using Tool.Extensions;
using Tool.Helper;
using Tool.Model.Entity.MySQL;
using Tool.Model.Repository.FiveGame;
using Tool.Service;
using Tool.ViewModel;
using Tool.ViewModel.Options;
using Microsoft.EntityFrameworkCore;
using Tool.Model.Repository.Mongo;

var builder = WebApplication.CreateBuilder(args);

// 綁定配置到 ConfigManager
builder.Configuration.Bind(new ConfigManager());

#region ---------- 這邊寫注入服務 ----------

builder.Services.AddHostedService<MonitorService>();

builder.Services.Configure<RepoOption>(options =>
{
    options.IsAdmin = true;
    options.TestOperatorIds = ConfigManager.SummarySetting.TestOperatorIds;
});

// 注入 Mongo 服務
builder.Services.InjectionMongo();

// Repository - FiveGame
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IMemberTransferLogRepository, MemberTransferLogRepository>();
builder.Services.AddScoped<IOperatorRepository, OperatorRepository>();
builder.Services.AddScoped<IMemberCleaningBackupRepository, MemberCleaningBackupRepository>();
builder.Services.AddScoped<IMemberSessionRepository, MemberSessionRepository>();
builder.Services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();

// Service
builder.Services.AddScoped<IAccountingCheckService, AccountingCheckService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<IMemberCleaningService, MemberCleaningService>();
builder.Services.AddScoped<ISummaryService, SummaryService>();
builder.Services.AddScoped<ISummaryCheckV1Service, SummaryCheckV1Service>();

// Options
builder.Services.Configure<WarmDbHost>(options =>
{
    options.Host = ConfigManager.ConnectionStrings.AgentWarmMongoConnection;
});

// Helper
builder.Services.AddSingleton<IDBHelper, DBHelper>();
builder.Services.AddScoped<IWarmDBRepositoryHelper, WarmDBRepositoryHelper>();

#endregion

builder.Services.AddDbContext<FiveGameEntities>(options => options.UseMySql(ConfigManager.ConnectionStrings.FiveGameConnection, new MySqlServerVersion(new Version(8, 0, 32)), mysqlOptions =>
{
    mysqlOptions.UseMicrosoftJson();
    mysqlOptions.CommandTimeout(300); // 設定命令超時為 300 秒
})
.EnableDetailedErrors(),
ServiceLifetime.Scoped);

var app = builder.Build();

app.Run();
