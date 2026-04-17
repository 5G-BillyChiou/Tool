using ClosedXML.Excel;
using NPOI.SS.UserModel;
using System.Reflection;
using Tool.Enum;
using Tool.ViewModel.Npoi;

namespace Tool.Helper;

/// <summary>
/// NPOI
/// </summary>
public class NpoiHelper : INpoiHelper
{
    /// <summary>
    /// EXCEL轉list
    /// </summary>
    public List<T> ExcelToData<T>(IFormFile file) where T : class, new()
    {
        var rtn = new List<T>();

        using (var stream = file.OpenReadStream())
        {
            // 使用 ClosedXML 打开 Excel 文件
            XLWorkbook wb = new XLWorkbook(stream);

            // 读取第一个工作表
            IXLWorksheet sheet = wb.Worksheet(1);

            // 提高性能：只读取有数据的行和列
            int rowMax = sheet.RowsUsed().Count();
            int numMax = sheet.ColumnsUsed().Count();

            // 从第二行开始读取，跳过标题行
            for (int row = 2; row <= rowMax; row++)
            {
                T obj = new T(); // 动态创建泛型对象
                int num = 1;
                bool flag = false;

                foreach (PropertyInfo prop in obj.GetType().GetProperties())
                {
                    if (num > numMax) { break; }

                    var cell = sheet.Cell(row, num);
                    if (!cell.IsEmpty())
                    {
                        try
                        {
                            // 获取单元格的值并转换为字符串
                            string cellValue = cell.GetString();

                            // 尝试将单元格的值转换为属性的类型
                            object convertedValue = Convert.ChangeType(cellValue, prop.PropertyType);
                            prop.SetValue(obj, convertedValue);

                            flag = true;  // 记录有有效数据
                        }
                        catch (Exception)
                        {
                            // 忽略无法转换的单元格
                        }
                    }
                    num++;
                }

                // 如果对象有有效数据则添加到结果列表中
                if (flag) { rtn.Add(obj); }
            }
        }

        return rtn;
    }

    /// <summary>
    /// 創造 Excel 檔
    /// </summary>
    public MemoryStream ExportExcel<T>(NpoiParam<T> parm)
    {
        foreach (var sheetEntry in parm.Sheets)
        {
            // 依情况决定要建新的 Sheet 或是用旧的 (即来自范本)
            ISheet sheet = GetSheet(parm, sheetEntry.Key);

            var sheetName = sheetEntry.Key;
            var columnMappings = parm.ColumnMappings.ContainsKey(sheetName) 
                ? parm.ColumnMappings[sheetName] 
                : new List<ColumnMapping>();

            if (sheetEntry.Value.Any())
            {
                // 将 IEnumerable<object> 转换为 List<object>
                var data = sheetEntry.Value.ToList();
                // 有资料塞格子
                SetSheetValue(ref parm, ref sheet, data, columnMappings);
            }
            else
            {
                // 若没资料在起点写入 No Data !
                CreateNewRowOrNot(ref sheet, parm.RowStartFrom, parm.ColumnStartFrom);
                sheet.GetRow(parm.RowStartFrom).CreateCell(parm.ColumnStartFrom).SetCellValue("No Data !");
            }
        }

        MemoryStream memoryStream = new MemoryStream();
        parm.Workbook.Write(memoryStream); // 将 workbook 写入 MemoryStream
        parm.Workbook.Close();

        return memoryStream;
    }

    #region ExportExcel的私有方法

    /// <summary>
    /// 在 workbook 中以 sheet name 寻找 是否找得到 sheet
    /// </summary>
    private ISheet GetSheet<T>(NpoiParam<T> param, string sheetName)
    {
        // 在 workbook 中以 sheet name 寻找 是否找得到 sheet
        if (param.Workbook.GetSheet(sheetName) == null)
        {
            // 找不到建一张新的 sheet
            ISheet sheet = param.Workbook.CreateSheet(sheetName);
            sheet = CreateColumn(param, sheetName);

            return sheet;
        }
        else
        {
            // 找得到即为要塞值的目标
            return param.Workbook.GetSheet(sheetName);
        }
    }

    /// <summary>
    /// 建立欄位
    /// </summary>
    private ISheet CreateColumn<T>(NpoiParam<T> parm, string sheetName)
    {
        var sheet = parm.Workbook.GetSheet(sheetName);
        sheet.CreateRow(0);
        ICellStyle headerStyle = GetBaseCellStyle(parm.Workbook, parm.FontStyle);
        headerStyle.Alignment = HorizontalAlignment.Center;
        headerStyle.VerticalAlignment = VerticalAlignment.Center;

        var columnMappings = parm.ColumnMappings.ContainsKey(sheetName)
                            ? parm.ColumnMappings[sheetName]
                            : new List<ColumnMapping>();

        if (parm.ShowHeader)
        {
            for (int i = 0; i < columnMappings.Count; i++)
            {
                var offset = i + parm.ColumnStartFrom;
                sheet.GetRow(0).CreateCell(offset);                                                //先創建格子
                sheet.GetRow(0).GetCell(offset).CellStyle = headerStyle;                           //綁定基本格式
                sheet.GetRow(0).GetCell(offset).SetCellValue(columnMappings[i].ExcelColumnName);  //给值

                // NPOI 使用 1/256 個字元寬度作為單位
                // 將字元寬度乘以 256 轉換為 NPOI 單位
                sheet.SetColumnWidth(offset, columnMappings[i].ColumnWidth * 256);
            }
        }

        return sheet;
    }

    /// <summary>
    /// 塞值
    /// </summary>
    private void SetSheetValue<T>(ref NpoiParam<T> p, ref ISheet sht, IEnumerable<object> data, List<ColumnMapping> columnMappings)
    {
        //要從哪一行開始塞資料 (有可能自定範本 可能你原本範本內就有好幾行表頭 2行 3行...)
        int line = p.RowStartFrom;

        //有可能前面幾欄是自訂好的 得跳過幾個欄位再開始塞
        int columnOffset = p.ColumnStartFrom;


        //根據標題列先處理所有Style (對Npoi來說 '創建'Style在workbook中是很慢的操作 作越少次越好 絕對不要foreach在塞每行列實際資料時重覆作 只通通在標題列做一次就好)
        ICellStyle[] cellStyleArr = InitialColumnStyle(p.Workbook, columnMappings, p.FontStyle);

        foreach (var item in data)
        {
            //如果 x 軸有偏移值 則表示這行他已經自己建了某幾欄的資料 我們只負責塞後面幾欄 所以並非每次都create new row
            CreateNewRowOrNot(ref sht, line, columnOffset);

            for (int i = 0; i < columnMappings.Count; i++)
            {
                //建立格子 (需考量 x 軸有偏移值)
                var cell = sht.GetRow(line).CreateCell(i + columnOffset);

                //綁定style (記得 綁定是不慢的 但建新style是慢的 不要在迴圈裡無意義的反覆建style 只在標題處理一次即可)
                cell.CellStyle = cellStyleArr[i];

                //給值
                string value = GetValue<T>(item, columnMappings, i) ?? "";               //reflection取值
                SetCellValue(value, ref cell, columnMappings[i].DataType);      //幫cell填值
            }

            line++;
        }
    }

    /// <summary>
    /// 處理格式輸出
    /// </summary>
    private ICellStyle[] InitialColumnStyle(IWorkbook wb, List<ColumnMapping> columnMapping, FontStyle fontStyle)
    {
        ICellStyle[] styleArr = new ICellStyle[columnMapping.Count];

        for (int i = 0; i < columnMapping.Count; i++)
        {
            //取通用格式
            ICellStyle cellStyle = GetBaseCellStyle(wb, fontStyle);

            if (columnMapping[i].Format != null)
            {
                cellStyle.DataFormat = GetCellFormat(wb, columnMapping[i].Format);
            }

            // 設定自動換行
            if (columnMapping[i].WrapText)
            {
                cellStyle.WrapText = true;
            }

            // 設定底色
            if (columnMapping[i].BackgroundColor.HasValue)
            {
                cellStyle.FillForegroundColor = columnMapping[i].BackgroundColor.Value;
                cellStyle.FillPattern = FillPattern.SolidForeground;
            }

            styleArr[i] = cellStyle;
        }

        return styleArr;
    }
    /// <summary>
    /// 給值
    /// </summary>
    private void SetCellValue(string value, ref ICell cell, NpoiDataTypeEnum type)
    {
        switch (type)
        {
            //字串沒有格式
            case NpoiDataTypeEnum.String:
                if (!String.IsNullOrWhiteSpace(value)) cell.SetCellValue(value);
                break;

            //轉日期
            case NpoiDataTypeEnum.Date:
                if (!String.IsNullOrWhiteSpace(value)) cell.SetCellValue(Convert.ToDateTime(value));
                break;

            //轉數字
            case NpoiDataTypeEnum.Number:
                if (!String.IsNullOrWhiteSpace(value)) cell.SetCellValue(Convert.ToDouble(value));
                break;

            //不會發生;
            default:
                break;
        }
    }
    /// <summary>
    /// 建立格子
    /// </summary>
    private void CreateNewRowOrNot(ref ISheet sht, int line, int columnOffset)
    {
        //如果是從自定範本來則不能重畫格子 例如他給我範本 只要我畫後面三格 前兩格他自己做好了 如果我整行重畫 他自己畫的兩格也會消失
        if (columnOffset == 0 || line > sht.LastRowNum)
        {
            sht.CreateRow(line);
        }
    }
    /// <summary>
    /// 綁定基本格式
    /// </summary>
    private ICellStyle GetBaseCellStyle(IWorkbook wb, FontStyle fontStyle)
    {
        //畫線
        ICellStyle cellStyle = wb.CreateCellStyle();
        cellStyle.BorderLeft = BorderStyle.Thin;
        cellStyle.BorderBottom = BorderStyle.Thin;
        cellStyle.BorderTop = BorderStyle.Thin;
        cellStyle.BorderRight = BorderStyle.Thin;

        //預設字型大小
        IFont font1 = wb.CreateFont();
        font1.FontName = (fontStyle.FontName == null) ? "Arial" : fontStyle.FontName;
        font1.FontHeightInPoints = (fontStyle.FontHeightInPoints == null) ? (short)10 : fontStyle.FontHeightInPoints.Value;
        cellStyle.SetFont(font1);

        return cellStyle;
    }
    /// <summary>
    /// 取得格式
    /// </summary>
    private short GetCellFormat(IWorkbook wb, string formatStr)
    {
        IDataFormat dataFormat = wb.CreateDataFormat();
        return dataFormat.GetFormat(formatStr);
    }
    /// <summary>
    /// 取值
    /// </summary>
    private string? GetValue<T>(object obj, List<ColumnMapping> columnMapping, int order)
    {
        var fieldName = columnMapping[order].ModelFieldName;
        var prop = obj.GetType().GetProperty(fieldName);
        if (prop != null)
        {
            var value = prop.GetValue(obj, null);
            return (value == null) ? "" : value.ToString();
        }

        return "";
    }

    #endregion

}

/// <summary>
/// NPOI
/// </summary>
public interface INpoiHelper
{
    /// <summary>
    /// 匯出
    /// </summary>
    MemoryStream ExportExcel<T>(NpoiParam<T> p);

    /// <summary>
    /// Excel 轉 list
    /// </summary>
    List<T> ExcelToData<T>(IFormFile file) where T : class, new();
}
