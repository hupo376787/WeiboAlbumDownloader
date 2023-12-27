using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace WeiboAlbumDownloader.Helpers
{
    public class PushPlusHelper
    {
        private static readonly HttpClient client = new HttpClient();
        public static async Task SendMessage(string token, string title, string content)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            using HttpResponseMessage response = await client.GetAsync($"http://www.pushplus.plus/send?token={token}&title={title}&content={content}");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"{jsonResponse}");
        }
    }
}
