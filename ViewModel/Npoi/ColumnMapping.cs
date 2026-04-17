using System.Linq.Expressions;
using Tool.Enum;
using Tool.Extensions;

namespace Tool.ViewModel.Npoi;

/// <summary>
/// 欄位
/// </summary>
public class ColumnMapping
{
    private string _format;

    /// <summary>
    /// 欄位
    /// </summary>
    public string ModelFieldName { get; set; }

    /// <summary>
    /// 爛位名稱
    /// </summary>
    public string ExcelColumnName { get; set; }

    /// <summary>
    /// 欄位型態
    /// </summary>
    public NpoiDataTypeEnum DataType { get; set; }

    /// <summary>
    /// 格式化字串（對 String 類型無效）
    /// 如果沒有設定，Number 類型預設為 "#,##0.00"
    /// </summary>
    public string Format
    {
        get
        {
            // 如果有自訂格式，使用自訂格式
            if (!string.IsNullOrEmpty(_format))
                return _format;

            // 如果是數字類型且沒有自訂格式，使用預設格式
            if (DataType == NpoiDataTypeEnum.Number)
                return "#,##0.00";

            return null!;
        }
        set { _format = value; }
    }

    /// <summary>
    /// 欄位寬度（以字元為單位，適用於中文字）
    /// </summary>
    public int ColumnWidth { get; set; } = 10;

    /// <summary>
    /// 是否自動換行（當內容包含 \n 時需要設定為 true）
    /// </summary>
    public bool WrapText { get; set; } = false;

    /// <summary>
    /// 欄位底色（使用 NPOI IndexedColors 的索引值，例如：IndexedColors.LightYellow.Index）
    /// 設為 null 表示不設定底色
    /// </summary>
    public short? BackgroundColor { get; set; } = null;

    public static ColumnMapping Create<T>(Expression<Func<T, object>> propertySelector,
                                         string excelColumnName,
                                         NpoiDataTypeEnum dataType,
                                         int columnWidth,
                                         string? fromat = null,
                                         bool wrapText = false,
                                         short? backgroundColor = null)
    {
        return new ColumnMapping
        {
            ModelFieldName = PropertyExtensions.GetPropertyName(propertySelector),
            ExcelColumnName = excelColumnName,
            DataType = dataType,
            ColumnWidth = columnWidth,
            _format = fromat,
            WrapText = wrapText,
            BackgroundColor = backgroundColor
        };
    }
}