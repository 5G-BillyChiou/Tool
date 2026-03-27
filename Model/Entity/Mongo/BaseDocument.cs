using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Tool.Model.Entity.Mongo
{
    public class BaseDocument
    {
        public BaseDocument()
        {
            CreatedAt = DateTimeOffset.Now;
        }

        [BsonId]
        [BsonElement("_id")]
        [JsonConverter(typeof(ObjectIdConverter))]
        public virtual ObjectId Id { get; set; }

        [BsonElement("created_at")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// MongoDB Collection Name Attribute
    /// </summary>
    /// <remarks>
    /// 建構子
    /// </remarks>
    /// <param name="name"></param>
    [AttributeUsage(AttributeTargets.Class)]
    public class CollectionNameAttribute(string name) : Attribute
    {
        /// <summary>
        /// CollectionName
        /// </summary>
        public string Name { get; private set; } = name;
    }

    /// <summary>
    /// ObjectId 轉換器
    /// 將 ObjectId 轉換成 string 並反轉的過程，若不加入轉換器將換導致string和ObjectId的無法正確轉換
    /// </summary>
    public class ObjectIdConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ObjectId);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader?.Value == null)
                throw new JsonException("ObjectId value can't be empty or null");

            if (reader.TokenType == JsonToken.String)
            {
                string value = (string)reader.Value;
                return ObjectId.Parse(value);
            }
            throw new JsonException("Expected string value for ObjectId");
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
                throw new JsonException("ObjectId value can't be empty or null");

            writer.WriteValue(value.ToString());
        }
    }
}
