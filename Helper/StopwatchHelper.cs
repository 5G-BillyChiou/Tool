using System.Diagnostics;


namespace Tool.Helper;


/// <summary>
/// 提供高精確度的程式碼效能計時功能，用於測量程式碼區塊的執行時間。
/// </summary>
public class StopwatchHelper
{
    private readonly Stopwatch _Stopwatch = new Stopwatch();
    private long _LastElapsedTicks;


    /// <summary> 區間秒數 </summary>
    public double ElapsedSeconds 
    {
        get 
        {
            var elapsedTicks = _Stopwatch.ElapsedTicks - _LastElapsedTicks;
            _LastElapsedTicks = _Stopwatch.ElapsedTicks;

            return Math.Round( elapsedTicks / (double)Stopwatch.Frequency, 4 );
        }
    }

    /// <summary> 累積總秒數 </summary>
    public double TotalSeconds => Math.Round( _Stopwatch.ElapsedTicks / (double)Stopwatch.Frequency, 4 );


    /// <summary>
    /// 開始計時新的操作區段。
    /// </summary>
    public void BeginTiming()
    {
        _Stopwatch.Restart();
        _LastElapsedTicks = 0;
    }


    /// <summary>
    /// 輸出目前區間的執行時間及累計執行時間。
    /// </summary>
    public string LogElapsedTime( string note = "", bool consoleLog = false )
    {
        var log = $"Interval time: {ElapsedSeconds:0.0000} seconds, Total time: {TotalSeconds:0.0000} seconds: {note}";

        if ( consoleLog )
            Console.WriteLine( log );

        return log;
    }


    /// <summary>
    /// 結束計時並輸出最終統計資訊。
    /// </summary>
    public string EndTiming( string note = "", bool consoleLog = false )
    {
        var log = $"Interval time: {ElapsedSeconds:0.0000} seconds, Total time: {TotalSeconds:0.0000} seconds: {note}";
        _Stopwatch.Stop();

        if ( consoleLog )
            Console.WriteLine( log );

        return log;
    }

}