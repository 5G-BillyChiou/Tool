using MongoDB.Driver;
using Tool.Model.Entity.Mongo;

namespace Tool.Model.Repository.Mongo;


/// <summary>
/// 貨幣匯率表。
/// </summary>
public class ExchangeRateRepository(IMongoDatabase mongoDbContext) : MongoRepository<ExchangeRate>(mongoDbContext), IExchangeRateRepository
{
    /// <summary>
    /// 通過幣別代號取得該幣別最新的匯率。
    /// </summary>
    public RateData[]? GetRateListByCurrency(string currency)
        => this.GetAll()
                .Where(d => d.Currency.Equals(currency))
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => d.RateDatas)
                .FirstOrDefault();


    /// <summary>
    /// 通過幣別代號取得該幣別最新的資料
    /// </summary>
    public ExchangeRate? GetByCurrency(string currency)
        => this.GetAll()
                .Where(d => d.Currency.Equals(currency))
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();

    /// <summary>
    /// 通過幣別代號、時間取得該幣別最新的匯率。
    /// </summary>
    public RateData[]? GetRateListByCurrencyAndMonth(string currency, DateTimeOffset month)
        => this.GetAll()
                .Where(d => d.Currency.Equals(currency))
                .Where(d => d.CreatedAt >= month && d.CreatedAt < month.AddMonths(1))
                .OrderBy(d => d.CreatedAt)
                .Select(d => d.RateDatas)
                .FirstOrDefault();

    /// <summary>
    /// 取得指定幣別的最新匯率
    /// </summary>
    /// <param name="currencyCodes">要查找的幣別代號列表</param>
    /// <returns>幣別代號對匯率的字典</returns>
    public Dictionary<string, decimal> GetLatestRatesForCurrencies(IEnumerable<string> currencyCodes)
    {
        var result = new Dictionary<string, decimal>();

        // 按時間倒序排列，取得最新的匯率記錄
        var latestExchangeRates = _mongoCollection
            .Find(FilterDefinition<ExchangeRate>.Empty)
            .SortByDescending(x => x.Timestamp)
            .ToList();

        foreach (var currencyCode in currencyCodes)
        {
            // 找到包含該幣別的最新匯率記錄
            foreach (var exchangeRate in latestExchangeRates)
            {
                var rateData = exchangeRate.RateDatas?.FirstOrDefault(r => r.CurrencyCode == currencyCode);
                if (rateData != null)
                {
                    result[currencyCode] = rateData.Rate;
                    break; // 找到就跳出，因為已經是按時間排序的最新資料
                }
            }
        }

        return result;
    }
}


/// <summary>
/// 貨幣匯率表 介面。
/// </summary>
public interface IExchangeRateRepository : IMongoRepository<ExchangeRate>
{
    /// <summary>
    /// 通過幣別代號取得該幣別最新的匯率。
    /// </summary>
    RateData[]? GetRateListByCurrency(string currency);

    /// <summary>
    /// 通過幣別代號取得該幣別最新的資料
    /// </summary>
    ExchangeRate? GetByCurrency(string currency);

    /// <summary>
    /// 通過幣別代號、時間取得該幣別最新的匯率。
    /// </summary>
    RateData[]? GetRateListByCurrencyAndMonth(string currency, DateTimeOffset month);

    /// <summary>
    /// 取得指定幣別的最新匯率
    /// </summary>
    /// <param name="currencyCodes">要查找的幣別代號列表</param>
    /// <returns>幣別代號對匯率的字典</returns>
    Dictionary<string, decimal> GetLatestRatesForCurrencies(IEnumerable<string> currencyCodes);
}