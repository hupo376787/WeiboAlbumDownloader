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
        public static async Task<T> GetAsync<T>(string url, WeiboDataSource dataSource, string cookie, string fileName = "")
        {
            try
            {
                var handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                client = new HttpClient(handler);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Referer", "https://m.weibo.cn/");
                if (string.IsNullOrEmpty(fileName))
                {
                    //request.Headers.Add("Host", "m.weibo.cn");
                    //request.Headers.Add("Connection", "keep-alive");
                    //request.Headers.Add("Pragma", "no-cache");
                    //request.Headers.Add("Cache-Control", "no-cache");
                    //request.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
                    request.Headers.Add("Accept", "application/json, text/plain, */*");
                    request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                    request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
                    request.Headers.Add("Cookie", cookie);
                }

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var contentEncoding = response.Content.Headers.ContentEncoding.ToString();
                var contentType = response.Content.Headers.ContentType.ToString();

                Debug.WriteLine("Content-Encoding: " + contentEncoding);
                Debug.WriteLine("Content-Type: " + contentType);

                string responseBody;
                // 检查是否是压缩数据并手动解压
                if (contentEncoding.Contains("gzip"))
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var decompressedStream = new GZipStream(stream, CompressionMode.Decompress))
                    using (var reader = new StreamReader(decompressedStream))
                    {
                        responseBody = await reader.ReadToEndAsync();
                        Debug.WriteLine(responseBody);  // 打印解压后的内容
                    }
                }
                else
                {
                    // 如果没有压缩，直接读取
                    responseBody = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine(responseBody);  // 打印返回的内容
                }


                if (!string.IsNullOrEmpty(fileName))
                {
                    var invalidChar = Path.GetInvalidFileNameChars();
                    var newFileName = invalidChar.Aggregate(Path.GetFileName(fileName), (o, r) => (o.Replace(r.ToString(), string.Empty)));

                    if (newFileName.Length > 200)
                        newFileName = GetUniqueFileName(newFileName.Substring(0, 200) + Path.GetExtension(newFileName));
                    else
                        newFileName = GetUniqueFileName(fileName);

                    var stream = response.Content.ReadAsStream();
                    FileStream lxFS = File.Create(newFileName);
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
            catch
            {
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
