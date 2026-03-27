using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Tool.Helper;

/// <summary>
/// Google Sheets Helper
/// </summary>
public class GoogleSheetsHelper : IGoogleSheetsHelper
{
    private readonly SheetsService _sheetsService;

    public GoogleSheetsHelper()
    {
        var credentialPath = ConfigManager.GoogleSheetsCredentialPath;

        if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
        {
            throw new FileNotFoundException($"Google Sheets credential file not found: {credentialPath}");
        }

        var credential = GoogleCredential.FromFile(credentialPath)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Tool"
        });
    }

    /// <summary>
    /// 將資料附加到 Google Sheets
    /// </summary>
    public async Task AppendRowsAsync(string spreadsheetId, string sheetName, IList<IList<object>> rows)
    {
        var range = $"{sheetName}!A:Z";
        var valueRange = new ValueRange { Values = rows };

        var request = _sheetsService.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

        await request.ExecuteAsync();
    }

    /// <summary>
    /// 清除並寫入資料到 Google Sheets
    /// </summary>
    public async Task ClearAndWriteAsync(string spreadsheetId, string sheetName, IList<IList<object>> rows)
    {
        var range = $"{sheetName}!A:Z";

        // 清除現有資料
        var clearRequest = _sheetsService.Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId, range);
        await clearRequest.ExecuteAsync();

        // 寫入新資料
        var valueRange = new ValueRange { Values = rows };
        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, $"{sheetName}!A1");
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        await updateRequest.ExecuteAsync();
    }
}

/// <summary>
/// Google Sheets Helper 介面
/// </summary>
public interface IGoogleSheetsHelper
{
    /// <summary>
    /// 將資料附加到 Google Sheets
    /// </summary>
    Task AppendRowsAsync(string spreadsheetId, string sheetName, IList<IList<object>> rows);

    /// <summary>
    /// 清除並寫入資料到 Google Sheets
    /// </summary>
    Task ClearAndWriteAsync(string spreadsheetId, string sheetName, IList<IList<object>> rows);
}
