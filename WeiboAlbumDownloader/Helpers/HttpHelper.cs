using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

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
        public static async Task<T> GetAsync<T>(string url, bool isFromWeiboCom, string cookie, string fileName = "")
        {
            try
            {
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                //加上UA就请求失败，是啥原因
                //myHttpWebRequest.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1";
                myHttpWebRequest.CookieContainer = new CookieContainer();

                var array = cookie.Split(";");
                foreach (var item in array)
                {
                    if (string.IsNullOrEmpty(item.Trim()) || item.Trim().StartsWith("UOR")) { continue; }

                    var temp = item.Split("=");
                    //myHttpWebRequest.CookieContainer.Add(
                    //    new Cookie("login_sid_t", "f896b3349c182456456481313fbb262") { Domain = "weibo.com" }
                    //    );
                    if (isFromWeiboCom)
                    {
                        myHttpWebRequest.CookieContainer.Add(new Cookie(temp[0].Trim(), temp[1].Trim()) { Domain = "weibo.com" });
                    }
                    else
                    {
                        myHttpWebRequest.CookieContainer.Add(new Cookie(temp[0].Trim(), temp[1].Trim()) { Domain = "weibo.cn" });
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
    }
}
