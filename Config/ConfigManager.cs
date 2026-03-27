using Tool.Config;

namespace Tool
{
    /// <summary>
    /// 模擬器參數設定檔
    /// </summary>
    public class ConfigManager
    {
        /// <summary>
        /// 連線訊息
        /// </summary>
        public static ConnectionStrings ConnectionStrings { get; set; }

        /// <summary>
        /// 開發相關設定
        /// </summary>
        public static DevelopmentSetting DevelopmentSetting { get; set; }

        /// <summary>
        /// 請求參數
        /// </summary>
        public static RequestSetting Request { get; set; } = new RequestSetting();

        /// <summary>
        ///
        /// </summary>
        public static SummarySetting SummarySetting { get; set; }

        /// <summary>
        /// Google Sheets 服務帳戶憑證檔案路徑
        /// </summary>
        public static string GoogleSheetsCredentialPath { get; set; }
    }
}
