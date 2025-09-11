using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using WeiboAlbumDownloader.Enums;
using WeiboAlbumDownloader.Models;

namespace WeiboAlbumDownloader.Helpers
{
    public class HttpHelper
    {
        static HttpClient client;
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="fileName">不为空的时候，表示存图</param>
        /// <returns></returns>
        public static async Task<T> GetAsync<T>(string url, WeiboDataSource dataSource, string cookie, string fileName = "", Action<string, MessageEnum> logAction = null)
        {
            string responseBody = null;
            try
            {
                logAction?.Invoke($"[Request URL]: {url}", MessageEnum.Info);
                logAction?.Invoke($"[Request Cookie]: {cookie}", MessageEnum.Info);

                var handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                client = new HttpClient(handler);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Referer", "https://m.weibo.cn/");
                if (string.IsNullOrEmpty(fileName))
                {
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
                    request.Headers.Add("Accept", "application/json, text/plain, */*");
                    request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                    request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
                    request.Headers.Add("Cookie", cookie);
                }

                var response = await client.SendAsync(request);

                var contentEncoding = response.Content.Headers.ContentEncoding.ToString();

                if (contentEncoding.Contains("gzip"))
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var decompressedStream = new GZipStream(stream, CompressionMode.Decompress))
                    using (var reader = new StreamReader(decompressedStream))
                    {
                        responseBody = await reader.ReadToEndAsync();
                    }
                }
                else
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                }

                logAction?.Invoke($"[Response Status Code]: {response.StatusCode}", MessageEnum.Info);
                logAction?.Invoke($"[Response Body]: {responseBody}", MessageEnum.Info);

                response.EnsureSuccessStatusCode();

                if (!string.IsNullOrEmpty(fileName))
                {
                    string directory = Path.GetDirectoryName(fileName);
                    string fileNameOnly = Path.GetFileName(fileName);

                    var invalidChar = Path.GetInvalidFileNameChars();
                    var cleanedFileNameOnly = invalidChar.Aggregate(fileNameOnly, (o, r) => (o.Replace(r.ToString(), string.Empty)));

                    string finalFullPath;
                    if (cleanedFileNameOnly.Length > 200)
                    {
                        // Truncate the file name part, then recombine with the original directory
                        string truncatedFileName = cleanedFileNameOnly.Substring(0, 200) + Path.GetExtension(cleanedFileNameOnly);
                        finalFullPath = Path.Combine(directory, truncatedFileName);
                    }
                    else
                    {
                        // Recombine the original directory with the cleaned file name
                        finalFullPath = Path.Combine(directory, cleanedFileNameOnly);
                    }

                    string uniquePath = GetUniqueFileName(finalFullPath);

                    var stream = response.Content.ReadAsStream();
                    FileStream lxFS = File.Create(uniquePath);
                    await stream.CopyToAsync(lxFS);
                    lxFS.Close();
                    lxFS.Dispose();
                    stream.Dispose();

                    return default(T);
                }

                Type type = typeof(T);
                if (type == typeof(string))
                    return (T)Convert.ChangeType(responseBody, typeof(T));
                else
                    return JsonConvert.DeserializeObject<T>(responseBody);
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"[Request Failed]: URL: {url}", MessageEnum.Error);
                if (responseBody != null)
                {
                    logAction?.Invoke($"[Failed Response Body]: {responseBody}", MessageEnum.Error);
                }
                logAction?.Invoke($"[Exception]: {ex.ToString()}", MessageEnum.Error);
                throw;
            }
        }

        public static string GetUniqueFileName(string filePath)
        {
            // 获取文件目录和扩展名
            string directory = Path.GetDirectoryName(filePath)!;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int count = 1;

            // 初始文件路径
            string uniqueFilePath = filePath;

            // 检查文件是否存在，如果存在则循环添加编号直到找到一个不存在的文件名
            while (File.Exists(uniqueFilePath))
            {
                string newFileName = $"{fileNameWithoutExtension}({count}){extension}";
                uniqueFilePath = Path.Combine(directory, newFileName);
                count++;
            }

            return uniqueFilePath;
        }
    }
}
