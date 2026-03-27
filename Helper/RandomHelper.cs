using System.Security.Cryptography;

namespace Tool.Helper;

/// <summary>
/// 隨機數輔助類別 - 支援依賴注入
/// </summary>
public static class RandomHelper
{
    // 靜態持有實例 (由 DI 容器設置)
    private static IRandomGenerator? _instance;

    /// <summary>
    /// 由 DI 容器在啟動時設置
    /// </summary>
    public static void Initialize(IRandomGenerator randomGenerator)
    {
        _instance = randomGenerator ?? throw new ArgumentNullException(nameof(randomGenerator));
    }

    /// <summary>
    /// 隨機數生成器 - 向後兼容的靜態方法
    /// </summary>
    public static int RandFunction(int min, int max)
    {
        if (_instance == null)
        {
            // Fallback: 如果尚未初始化,使用預設實作
            return RandomNumberGenerator.GetInt32(min, max);
        }

        return _instance.Next(min, max);
    }

    /// <summary>
    /// 密碼學級別的隨機數生成器
    /// </summary>
    public static uint CryptoRandomFunction(uint min, uint max)
    {
        if (_instance == null)
        {
            // Fallback
            return (uint)RandomNumberGenerator.GetInt32((int)min, (int)max);
        }

        return _instance.NextUInt(min, max);
    }

    // 其他靜態方法保持不變...
    public static string GenerateBaseRandomChar(int length, string baseChar)
    {
        var chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = baseChar[RandFunction(0, baseChar.Length)];

        return new string(chars);
    }

    public static string GenerateRandomLowerChar(int length)
        => GenerateBaseRandomChar(length, "abcdefghijkmnopqrstuvwxyz0123456789");

    public static string GenerateRandomChar(int length)
        => GenerateBaseRandomChar(length, "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789");

    public static string GenerateToken(int length)
        => GenerateBaseRandomChar(length, "123456789abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ");

    public static string GenerateUpperRandomChar(int length, bool needNumber)
    {
        if (needNumber)
            return GenerateBaseRandomChar(length, "ABCDEFGHJKLMNPQRSTUVWXYZ123456789");
        else
            return GenerateBaseRandomChar(length, "ABCDEFGHJKLMNPQRSTUVWXYZ");
    }

    public static int GenerateRandomNumber(int minValue, int maxValue)
        => RandFunction(minValue, maxValue);

    public static string GenerateRandomChar(int minLength, int maxLength, string prefixStr = "", string suffixStr = "", string middleStr = "")
    {
        var result = GenerateBaseRandomChar(RandFunction(minLength, maxLength + 1),
            "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789");

        if (!string.IsNullOrEmpty(middleStr))
        {
            result = result.Insert(RandFunction(1, result.Length + 1), middleStr);
        }

        return $"{prefixStr}{result}{suffixStr}";
    }

    public static bool Probability(double num, int accuracy = 0)
    {
        if (num < 0 || num > 100)
        {
            throw new ArgumentException("機率必須在 0-100 之間", nameof(num));
        }

        int transformNum = (int)(num * Math.Pow(10, accuracy));
        int basicNum = (int)Math.Pow(10, accuracy + 2);
        int randomNum = RandFunction(0, basicNum);

        return randomNum <= (transformNum - 1);
    }
}


/// <summary>
/// 效能優先隨機數生成器 - 適用於遊戲邏輯等不需要密碼學安全的場景
/// 使用 Random.Shared 以獲得最佳效能
/// </summary>
public class SharedRandomGenerator : IRandomGenerator
{
    public int Next(int minValue, int maxValue)
    {
        if (minValue == maxValue)
            return minValue;

        int first = Math.Min(minValue, maxValue);
        int second = Math.Max(minValue, maxValue);

        return Random.Shared.Next(first, second);
    }

    public uint NextUInt(uint minValue, uint maxValue)
    {
        if (minValue == maxValue)
            return minValue;

        uint first = Math.Min(minValue, maxValue);
        uint second = Math.Max(minValue, maxValue);

        var range = second - first;
        var bytes = new byte[4];
        Random.Shared.NextBytes(bytes);
        var scale = (double)BitConverter.ToUInt32(bytes, 0) / uint.MaxValue;

        return (uint)(first + range * scale);
    }

    public void NextBytes(byte[] buffer)
    {
        Random.Shared.NextBytes(buffer);
    }
}

/// <summary>
/// 密碼學級別隨機數生成器 - 適用於需要高安全性的場景
/// </summary>
public class CryptoRandomGenerator : IRandomGenerator
{
    public int Next(int minValue, int maxValue)
    {
        if (minValue == maxValue)
            return minValue;

        int first = Math.Min(minValue, maxValue);
        int second = Math.Max(minValue, maxValue);

        return RandomNumberGenerator.GetInt32(first, second);
    }

    public uint NextUInt(uint minValue, uint maxValue)
    {
        if (minValue == maxValue)
            return minValue;

        uint first = Math.Min(minValue, maxValue);
        uint second = Math.Max(minValue, maxValue);

        var range = second - first;
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var scale = (double)BitConverter.ToUInt32(bytes, 0) / uint.MaxValue;

        return (uint)(first + range * scale);
    }

    public void NextBytes(byte[] buffer)
    {
        RandomNumberGenerator.Fill(buffer);
    }
}

/// <summary>
/// 隨機數生成器介面
/// </summary>
public interface IRandomGenerator
{
    /// <summary>
    /// 產生隨機整數，範圍 [minValue, maxValue)
    /// </summary>
    int Next(int minValue, int maxValue);

    /// <summary>
    /// 產生隨機無符號整數，範圍 [minValue, maxValue)
    /// </summary>
    uint NextUInt(uint minValue, uint maxValue);

    /// <summary>
    /// 填充位元組陣列
    /// </summary>
    void NextBytes(byte[] buffer);
}