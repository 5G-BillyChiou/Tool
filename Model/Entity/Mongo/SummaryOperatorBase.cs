using MongoDB.Bson.Serialization.Attributes;

namespace Tool.Model.Entity.Mongo;


/// <summary>
/// 個別代理商的下注彙總表
/// </summary>
public class SummaryOperatorBase : SummaryBetBase
{    
    /// <summary>
    /// 經過 Estimator 的不重複會員人數資訊。
    /// </summary>
    [BsonElement("estimator_bytes")]
    public byte[] EstimatorBytes { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 暫存 EstimatorBytes 待彙總的 Bytes。
    /// </summary>
    [BsonElement("estimator_bytes_temp")]
    public byte[][]? EstimatorBytesTemp { get; set; }

    //----------------------------BasicBet------------------------

    /// <summary>
    /// 經過 Estimator 的不重複會員人數資訊。
    /// </summary>
    [BsonElement("basic_estimator_bytes")]
    public byte[] BasicEstimatorBytes { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 暫存 EstimatorBytes 待彙總的 Bytes。
    /// </summary>
    [BsonElement("basic_estimator_bytes_temp")]
    public byte[][]? BasicEstimatorBytesTemp { get; set; }

    //----------------------------ExtraBet-----------------------

    /// <summary>
    /// 經過 Estimator 的不重複會員人數資訊。
    /// </summary>
    [BsonElement("extra_estimator_bytes")]
    public byte[] ExtraEstimatorBytes { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 暫存 EstimatorBytes 待彙總的 Bytes。
    /// </summary>
    [BsonElement("extra_estimator_bytes_temp")]
    public byte[][]? ExtraEstimatorBytesTemp { get; set; }

    //--------------------------FeatureBuy---------------------------

    /// <summary>
    /// 經過 Estimator 的不重複會員人數資訊。
    /// </summary>
    [BsonElement("feature_buy_estimator_bytes")]
    public byte[] FeatureBuyEstimatorBytes { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 暫存 EstimatorBytes 待彙總的 Bytes。
    /// </summary>
    [BsonElement("feature_buy_estimator_bytes_temp")]
    public byte[][]? FeatureBuyEstimatorBytesTemp { get; set; }

}