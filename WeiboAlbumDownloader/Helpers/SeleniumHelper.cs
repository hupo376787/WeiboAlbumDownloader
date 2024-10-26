using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WeiboAlbumDownloader.Enums;

namespace WeiboAlbumDownloader.Helpers
{
    internal class SeleniumHelper
    {
        public static string? GetCookie(WeiboDataSource dataSource)
        {
            string loginUrl = "https://passport.weibo.com/sso/signin?entry=wapsso&source=wapssowb&url=https://weibo.cn";
            if (dataSource == WeiboDataSource.WeiboCn)
                loginUrl = "https://passport.weibo.com/sso/signin?entry=wapsso&source=wapssowb&url=https://weibo.cn";
            else if (dataSource == WeiboDataSource.WeiboCnMobile)
                loginUrl = "https://passport.weibo.com/sso/signin?entry=wapsso&source=wapsso&url=https://m.weibo.cn";
            else
                loginUrl = "https://passport.weibo.com/sso/signin?entry=miniblog&source=miniblog&url=https://weibo.com/";

            IWebDriver driver = new ChromeDriver();
            driver.Url = loginUrl;
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromHours(8))
            {
                PollingInterval = TimeSpan.FromMilliseconds(500),
            };
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
            //wait.Until(d => d.FindElement(By.LinkText("title")));

            // 等待页面加载完成并获取页面标题
            wait.Until(d => d.Title.Equals("微博 – 随时随地发现新鲜事") || d.Title.Equals("我的首页") || d.Title.Equals("微博"));

            // 获取页面标题并进行检查
            string pageTitle = driver.Title;
            if (pageTitle.Equals("微博 – 随时随地发现新鲜事") || pageTitle.Equals("我的首页") || pageTitle.Equals("微博"))
            {
                //AppendLog("扫码登陆成功", MessageEnum.Success);
                // 获取所有的 Cookie 对象
                IReadOnlyCollection<Cookie> cookies = driver.Manage().Cookies.AllCookies;

                // 将 Cookie 对象转换为一个字符串，格式类似于 HTTP 请求头的 Cookie 字符串
                string cookie = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));

                // 打印 Cookie 字符串
                Debug.WriteLine(cookie);

                driver.Quit();

                return cookie;
            }
            else
            {
                Debug.WriteLine("未登录");
            }

            // 程序结束时，手动关闭浏览器
            driver.Quit();

            return null;
        }
    }
}
