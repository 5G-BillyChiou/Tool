using System.ComponentModel;

namespace Tool.Enum;

/// <summary>
/// 遊戲中的返回首頁按鈕模式。
/// </summary>
public enum GameReturnButtonEnum
{
    /// <summary>
    /// 不指定功能。
    /// </summary>
    [Description("不指定功能")]
    GameReturnButtonEnum_None = 0,

    /// <summary>
    /// 轉址到別的網頁。
    /// </summary>
    [Description("轉址到別的網頁")]
    GameReturnButtonEnum_Redirect = 1,

    /// <summary>
    /// 直接關閉遊戲視窗。
    /// </summary>
    [Description("直接關閉遊戲視窗")]
    GameReturnButtonEnum_CloseWindow = 2,

    ///// <summary>
    ///// 返回遊戲平台網頁大廳。
    ///// </summary>
    //[Description("返回遊戲平台網頁大廳")]
    //GameReturnButtonEnum_Lobby = 3, 

    ///// <summary>
    ///// 切換回安卓網頁模式。
    ///// </summary>
    //[Description("切換回安卓網頁模式")]
    //GameReturnButtonEnum_AndroidWebView = 4, 

}