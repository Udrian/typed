using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

#nullable enable

namespace TypeD.Helpers
{
    public static class APICaller
    {
#if RELEASE
        public static string URL { get { return "http://typeo.typedeaf.com"; } }
#elif DEBUG
        public static string URL { get { return "http://typeo-dev.typedeaf.com"; } }
        //public static string URL { get { return "https://localhost:44387"; } }
#endif
        private static HttpClient Client { get; set; } = new HttpClient();

        public static async Task<T?> GetJsonObject<T>(string endpoint)
        {
            try
            {
                var stringTask = Client.GetStringAsync($"{URL}/{endpoint}");
                var msg = await stringTask;
                return JsonSerializer.Deserialize<T>(msg, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return default;
            }
        }
    }
}
