using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WeiboAlbumDownloader.Helpers
{
    public static class WeiboMidHelper
    {
        private static readonly HttpClient Http = CreateClient();

        public static async Task<List<string>> GetImageIdsByMidAsync(string mid, string? cookie = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mid))
                return new List<string>();

            // 1) PC 接口
            var pc = await TryFromPcAsync(mid, cookie, ct);
            if (pc.Count > 0) return pc;

            // 2) 移动接口
            var mobile = await TryFromMobileAsync(mid, cookie, ct);
            return mobile;
        }

        private static async Task<List<string>> TryFromPcAsync(string mid, string? cookie, CancellationToken ct)
        {
            var url = $"https://weibo.com/ajax/statuses/show?id={mid}&locale=zh-CN&isGetLongText=true";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Referrer = new Uri("https://weibo.com/");
            req.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
            if (!string.IsNullOrWhiteSpace(cookie))
            {
                req.Headers.TryAddWithoutValidation("Cookie", cookie);
                var xsrf = TryGetCookieValue(cookie, "XSRF-TOKEN");
                if (!string.IsNullOrEmpty(xsrf))
                    req.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", xsrf);
            }

            using var resp = await Http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return new List<string>();

            var json = await resp.Content.ReadAsStringAsync(ct);
            return ExtractPicIds(json);
        }

        private static async Task<List<string>> TryFromMobileAsync(string mid, string? cookie, CancellationToken ct)
        {
            var url = $"https://m.weibo.cn/statuses/show?id={mid}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Referrer = new Uri($"https://m.weibo.cn/detail/{mid}");
            req.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
            if (!string.IsNullOrWhiteSpace(cookie))
                req.Headers.TryAddWithoutValidation("Cookie", cookie);

            using var resp = await Http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return new List<string>();

            var json = await resp.Content.ReadAsStringAsync(ct);
            return ExtractPicIds(json);
        }

        private static List<string> ExtractPicIds(string json)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(json)) return result.ToList();

            var root = JToken.Parse(json);

            // 兼容 m.weibo.cn: { ok: 1, data: {...} }
            if (root["ok"] != null && root["data"] != null)
                root = root["data"]!;

            void CollectFromNode(JToken? node)
            {
                if (node == null) return;

                // 优先 pic_ids
                var picIds = node["pic_ids"] as JArray;
                if (picIds != null)
                {
                    foreach (var id in picIds.Values<string>())
                        if (!string.IsNullOrWhiteSpace(id)) result.Add(id);
                }

                // 其次 pics[].pid
                var pics = node["pics"] as JArray;
                if (pics != null)
                {
                    foreach (var p in pics)
                    {
                        var pid = p?["pid"]?.Value<string>();
                        if (!string.IsNullOrWhiteSpace(pid)) result.Add(pid!);
                    }
                }

                // 兼容转发博文
                if (node["retweeted_status"] != null)
                    CollectFromNode(node["retweeted_status"]);
            }

            CollectFromNode(root);

            return result.ToList();
        }

        private static string? TryGetCookieValue(string cookieHeader, string key)
        {
            // 从整段 Cookie 文本中提取某个键的值
            // 例: "SUB=...; XSRF-TOKEN=abc; SUBP=...;"
            if (string.IsNullOrWhiteSpace(cookieHeader) || string.IsNullOrWhiteSpace(key))
                return null;

            var parts = cookieHeader.Split(';');
            foreach (var p in parts)
            {
                var kv = p.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (kv.Length == 2 && kv[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                    return kv[1];
            }
            return null;
        }

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All
            };

            var c = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            c.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
            c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9,en;q=0.8");

            return c;
        }
    }
}