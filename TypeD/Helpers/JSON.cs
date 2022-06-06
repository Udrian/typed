using System.IO;
using System.Text.Json;

namespace TypeD.Helpers
{
    public static class JSON
    {
        public static bool Serialize(object obj, string filePath)
        {
            var json = JsonSerializer.Serialize(obj, obj.GetType());
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

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = ignoreCase };

            var json = File.ReadAllText(filePath);
            var obj = (T)JsonSerializer.Deserialize(json, typeof(T), options);

            return obj;
        }
    }
}
