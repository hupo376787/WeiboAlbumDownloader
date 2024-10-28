using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WeiboAlbumDownloader.Helpers
{
    public class GithubHelper
    {
        public static async Task<string?> GetLatestVersion()
        {
            try
            {
                HttpClient client = new HttpClient();
                var resp = await client.GetAsync("https://github.com/hupo376787/WeiboAlbumDownloader/tags");
                var body = await resp.Content.ReadAsStringAsync();
                string pattern = "tags.*zip";
                //匹配多个
                //MatchCollection list = Regex.Matches(body, pattern);
                //匹配第一个
                Match match = Regex.Match(body, pattern);
                if (match.Success)
                {
                    var version = match.Value.Replace("tags/", "").Replace(".zip", "");
                    return version;
                }

                return null;

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static async Task<string> GetGiteeLatestVersion()
        {
            try
            {
                HttpClient client = new HttpClient();
                var resp = await client.GetAsync("https://gitee.com/hupo376787/weibo-album-downloader/tags");
                var body = await resp.Content.ReadAsStringAsync();
                string pattern = @"<a\s+title=""(\d+\.\d+)""";
                Regex regex = new Regex(pattern);
                //MatchCollection matches = regex.Matches(body);
                Match match = Regex.Match(body, pattern);
                if (match.Success)
                {
                    var version = match.Value.Replace("<a title=\"", "").Replace("\"", "");
                    return version;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
