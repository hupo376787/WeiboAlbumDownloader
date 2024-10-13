using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using WeiboAlbumDownloader.Enums;

namespace WeiboAlbumDownloader.Helpers
{
    public class HttpHelper
    {
        static HttpWebRequest myHttpWebRequest;
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
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.AllowAutoRedirect = true;
                //加上UA就请求失败，是啥原因
                //myHttpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                myHttpWebRequest.CookieContainer = new CookieContainer();

                var array = cookie.Split(";");
                foreach (var item in array)
                {
                    if (string.IsNullOrEmpty(item.Trim()) || item.Trim().StartsWith("UOR")) { continue; }

                    var temp = item.Split("=");
                    //myHttpWebRequest.CookieContainer.Add(
                    //    new Cookie("login_sid_t", "f896b3349c182456456481313fbb262") { Domain = "weibo.com" }
                    //    );
                    if (dataSource == WeiboDataSource.WeiboCn)
                    {
                        myHttpWebRequest.CookieContainer.Add(new Cookie(temp[0].Trim(), temp[1].Trim()) { Domain = "weibo.cn" });
                    }
                    else
                    {
                        myHttpWebRequest.CookieContainer.Add(new Cookie(temp[0].Trim(), temp[1].Trim()) { Domain = "weibo.com" });
                    }
                }

                using (HttpWebResponse lxResponse = (HttpWebResponse)myHttpWebRequest.GetResponse())
                {
                    var stream = lxResponse.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        FileStream lxFS = File.Create(fileName);
                        await stream.CopyToAsync(lxFS);
                        lxFS.Close();
                        lxFS.Dispose();
                        stream.Dispose();
                    }

                    string text = reader.ReadToEnd();
                    Type type = typeof(T);
                    if (type == typeof(string))
                        return (T)Convert.ChangeType(text, typeof(T));
                    else
                        return JsonConvert.DeserializeObject<T>(text);
                }
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
