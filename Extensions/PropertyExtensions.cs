using System.Linq.Expressions;

namespace Tool.Extensions;

/// <summary>
/// 擴展方法：安全地獲取對象的屬性值
/// </summary>
public static class PropertyExtensions
{
    public static TValue? GetPropertyValue<TObject, TValue>(this TObject obj, string propertyName)
    {
        if (obj == null)
            return default;

        var property = typeof(TObject).GetProperty(propertyName);
        if (property == null)
            return default;

        if (!typeof(TValue).IsAssignableFrom(property.PropertyType) &&
            !(property.PropertyType.IsValueType && typeof(TValue).IsValueType))
            return default;

        var value = property.GetValue(obj);
        if (value == null)
            return default;

        return (TValue)value;
    }

    public static object? GetPropertyValue<TObject>(this TObject obj, string propertyName)
    {
        if (obj == null)
            return null;
        var property = typeof(TObject).GetProperty(propertyName);
        if (property == null)
            return null;
        return property.GetValue(obj);
    }

    /// <summary>
    /// 從表達式中提取屬性名稱
    /// </summary>
    public static string GetPropertyName<T>(Expression<Func<T, object>> expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        // 處理不同類型的表達式
        MemberExpression memberExpression = null;

        // 處理 x => x.Property 類型的表達式
        if (expression.Body is MemberExpression directMemberExpression)
        {
            memberExpression = directMemberExpression;
        }
        // 處理 x => x.IntProperty 類型的表達式（會被轉換為 object）
        else if (expression.Body is UnaryExpression unaryExpression &&
                 unaryExpression.Operand is MemberExpression operand)
        {
            memberExpression = operand;
        }
        // 嘗試處理更複雜的表達式，如 x => x.Property.ToString()
        else if (expression.Body is MethodCallExpression methodCallExpression)
        {
            // 嘗試獲取方法調用的對象
            var methodObject = methodCallExpression.Object;
            if (methodObject is MemberExpression methodObjectMember)
            {
                memberExpression = methodObjectMember;
            }
            else
            {
                throw new ArgumentException("不支持的表達式類型: 方法調用", nameof(expression));
            }
        }
        else
        {
            throw new ArgumentException("不支持的表達式類型", nameof(expression));
        }

        // 收集嵌套屬性的名稱
        var propertyNames = new List<string>();

        // 遍歷成員表達式鏈，處理嵌套屬性
        while (memberExpression != null)
        {
            // 添加當前成員名稱
            propertyNames.Add(memberExpression.Member.Name);

            // 移動到下一個嵌套級別（如果有）
            memberExpression = memberExpression.Expression as MemberExpression;
        }

        // 由於我們是從最內層開始添加的，所以需要反轉列表
        propertyNames.Reverse();

        // 使用點號連接屬性名稱
        return string.Join(".", propertyNames);
    }
}
