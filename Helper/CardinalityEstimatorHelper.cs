using CardinalityEstimation;
using System.IO.Compression;

namespace Tool.Helper;

/// <summary>
/// 協助使用 CardinalityEstimator 的工具庫。
/// </summary>
public static class CardinalityEstimatorHelper
{
    private static CardinalityEstimatorSerializer _CardinalityEstimatorSerializer = new CardinalityEstimatorSerializer();


    /// <summary>
    /// 將多個會員編號合併為一個統一的 CardinalityEstimator。
    /// </summary>
    public static bool MergeMemberIds(IEnumerable<string>? memberIds, out CardinalityEstimator estimator)
    {
        estimator = new CardinalityEstimator(16);

        if (memberIds is null || memberIds.Any() == false)
            return false;

        foreach (var memberId in memberIds)
            estimator.Add(memberId);

        return true;
    }


    /// <summary>
    /// 輔助合併 CardinalityEstimator 的方法。
    /// 這邊會將經過 GZip 壓縮的 byte[] 資料與 CardinalityEstimator 進行合併。
    /// </summary>
    public static byte[] Merge(byte[] compressedEstimator, CardinalityEstimator estimator)
    {
        if (compressedEstimator.Length <= 0)
            return SerializeAndCompressEstimator(estimator);

        var a = DeserializeAndDecompressEstimator(compressedEstimator);
        a.Merge(estimator);

        return SerializeAndCompressEstimator(a);
    }


    /// <summary>
    /// 接收多組經過 GZip 壓縮的 byte[] 資料，將它們解壓縮並合併為一個統一的 CardinalityEstimator，最後再進行 GZip 壓縮後以 byte[] 回傳。
    /// </summary>
    public static byte[] MergeBytesList(params byte[][]? compressedEstimators)
    {
        if (compressedEstimators is null || compressedEstimators.Any() == false)
            return Array.Empty<byte>();


        CardinalityEstimator? estimator = null;

        foreach (var bytes in compressedEstimators)
        {
            if (bytes is null || bytes.Length <= 0)
                continue;

            var e = CardinalityEstimatorHelper.DeserializeAndDecompressEstimator(bytes);

            if (estimator is null)
                estimator = e;
            else
                estimator.Merge(e);
        }

        if (estimator != null)
            return CardinalityEstimatorHelper.SerializeAndCompressEstimator(estimator);

        return Array.Empty<byte>();
    }


    /// <summary>
    /// 將 CardinalityEstimator 序列化並使用 GZip 壓縮成 byte[]。
    /// </summary>
    public static byte[] SerializeAndCompressEstimator(CardinalityEstimator estimator)
    {
        var stream = new MemoryStream();
        _CardinalityEstimatorSerializer.Serialize(stream, estimator);

        return CompressGZip(stream.ToArray());
    }


    /// <summary>
    /// 將 GZip 壓縮後的 byte[] 解壓縮並還原為 CardinalityEstimator。
    /// </summary>
    public static CardinalityEstimator DeserializeAndDecompressEstimator(byte[] compressedBytes)
    {
        if (compressedBytes == null || compressedBytes.Length <= 0)
            return new CardinalityEstimator();

        var bytes = DecompressGZip(compressedBytes);
        var stream = new MemoryStream(bytes);

        return _CardinalityEstimatorSerializer.Deserialize(stream);
    }


    /// <summary>
    /// GZip 壓縮。
    /// </summary>
    private static byte[] CompressGZip(byte[] rawBytes)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(rawBytes, 0, rawBytes.Length);
        }

        return output.ToArray();
    }


    /// <summary>
    /// GZip 解壓縮。
    /// </summary>
    private static byte[] DecompressGZip(byte[] compressedBytes)
    {
        using var input = new MemoryStream(compressedBytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);

        return output.ToArray();
    }

}
