using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

#nullable enable

namespace TypeD.Helpers
{
    /// <summary>
    /// Calls the TypeO web API.
    /// </summary>
    public static class APICaller
    {
#if RELEASE
        /// <summary>Gets the base URL of the web API.</summary>
        public static string URL { get { return "http://typeo.typedeaf.com"; } }
#elif DEBUG
        /// <summary>Gets the base URL of the web API.</summary>
        public static string URL { get { return "http://typeo-dev.typedeaf.com"; } }
        //public static string URL { get { return "https://localhost:44387"; } }
#endif
        private static HttpClient Client { get; set; } = new HttpClient();

        /// <summary>Gets an endpoint and deserializes the JSON response.</summary>
        /// <typeparam name="T">The type to deserialize.</typeparam>
        /// <param name="endpoint">The endpoint relative to <see cref="URL"/>.</param>
        /// <returns>The deserialized object, or <c>default</c> if the call or deserialization fails.</returns>
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
