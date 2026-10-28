using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypeD.Helpers
{
    public static class JSON
    {
        private static readonly List<JsonConverter> CustomConverters = new();
        private static readonly object ConvertersLock = new();

        /// <summary>
        /// Registers a converter used for all serialization and deserialization of editor data, including property values.
        /// </summary>
        public static void AddConverter(JsonConverter converter)
        {
            lock (ConvertersLock)
            {
                if (!CustomConverters.Exists(c => c.GetType() == converter.GetType()))
                    CustomConverters.Add(converter);
            }
        }

        public static void RemoveConverter(JsonConverter converter)
        {
            lock (ConvertersLock)
            {
                CustomConverters.RemoveAll(c => c.GetType() == converter.GetType());
            }
        }

        /// <summary>
        /// Creates options that include all registered converters.
        /// </summary>
        public static JsonSerializerOptions CreateOptions(bool web = false, bool ignoreCase = false)
        {
            var options = web ? new JsonSerializerOptions(JsonSerializerDefaults.Web) : new JsonSerializerOptions();
            if (ignoreCase)
                options.PropertyNameCaseInsensitive = true;
            lock (ConvertersLock)
            {
                foreach (var converter in CustomConverters)
                    options.Converters.Add(converter);
            }
            return options;
        }

        public static bool Serialize(object obj, string filePath)
        {
            var json = JsonSerializer.Serialize(obj, obj.GetType(), CreateOptions());
            if (string.IsNullOrEmpty(json)) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, json);

            return true;
        }

        public static T Deserialize<T>(string filePath, bool ignoreCase = false)
        {
            if (!File.Exists(filePath))
            {
                return default;
            }

            var options = CreateOptions(ignoreCase: ignoreCase);

            var json = File.ReadAllText(filePath);
            var obj = (T)JsonSerializer.Deserialize(json, typeof(T), options);

            return obj;
        }
    }
}
