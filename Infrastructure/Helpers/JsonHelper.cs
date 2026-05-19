using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Infrastructure.Helpers
{
    public static class JsonHelper
    {
        #region STJ 配置

        public static readonly JsonSerializerOptions DefaultOptions;
        public static readonly JsonSerializerOptions CamelCaseOptions;

        static JsonHelper()
        {
            DefaultOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
            CamelCaseOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
            DefaultOptions.MakeReadOnly();
            CamelCaseOptions.MakeReadOnly();
        }

        #endregion

        #region STJ 静态方法（新代码使用）

        public static string SerializeCamelCase(object obj)
            => System.Text.Json.JsonSerializer.Serialize(obj, CamelCaseOptions);

        public static string Serialize(object obj)
            => System.Text.Json.JsonSerializer.Serialize(obj, DefaultOptions);

        public static T? Deserialize<T>(string json)
            => System.Text.Json.JsonSerializer.Deserialize<T>(json, DefaultOptions);

        public static bool TrySerialize(object obj, out string result)
        {
            try { result = Serialize(obj); return true; }
            catch { result = string.Empty; return false; }
        }

        public static bool TryDeserialize<T>(string json, out T? result)
        {
            try { result = Deserialize<T>(json); return true; }
            catch { result = default; return false; }
        }

        public static T? DeepClone<T>(T obj)
            => Deserialize<T>(Serialize(obj));

        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try { using var doc = JsonDocument.Parse(json); return true; }
            catch (System.Text.Json.JsonException) { return false; }
        }

        public static async Task<string> SerializeAsync<T>(T obj)
        {
            using var stream = new MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(stream, obj, DefaultOptions);
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        public static async Task<T?> DeserializeAsync<T>(Stream stream)
            => await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream, DefaultOptions);

        #endregion

        #region Newtonsoft 兼容层（旧代码通过 Instance 调用，行为不变）

        /// <summary>
        /// 兼容旧代码：Infrastructure.JsonHelper.Instance.Serialize(obj)
        /// 等价于 Infrastructure.Helpers.JsonHelper.Instance.Serialize(obj)
        /// </summary>
        public static LegacyJsonHelper Instance { get; } = new LegacyJsonHelper();

        public class LegacyJsonHelper
        {
            private static readonly IsoDateTimeConverter DateTimeConverter =
                new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };

            public string Serialize(object obj)
                => JsonConvert.SerializeObject(obj, DateTimeConverter);

            public string SerializeByConverter(object obj, params JsonConverter[] converters)
                => JsonConvert.SerializeObject(obj, converters);

            public T Deserialize<T>(string input)
                => JsonConvert.DeserializeObject<T>(input);

            public T DeserializeByConverter<T>(string input, params JsonConverter[] converters)
                => JsonConvert.DeserializeObject<T>(input, converters);

            public T DeserializeBySetting<T>(string input, JsonSerializerSettings settings)
                => JsonConvert.DeserializeObject<T>(input, settings);
        }

        #endregion
    }
}