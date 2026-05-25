using MicaWPF.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WeiboAlbumDownloader.Enums;

namespace WeiboAlbumDownloader
{
    public partial class WebViewCookieWindow : MicaWindow
    {
        private readonly WeiboDataSource dataSource;

        public string? Cookie { get; private set; }

        public WebViewCookieWindow(WeiboDataSource dataSource)
        {
            InitializeComponent();
            this.dataSource = dataSource;
            Loaded += WebViewCookieWindow_Loaded;
        }

        private async void WebViewCookieWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await WebView.EnsureCoreWebView2Async();

                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                WebView.CoreWebView2.Navigate(GetLoginUrl());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"WebView2 初始化失败：{ex.Message}\r\n\r\n请安装 Microsoft Edge WebView2 Runtime 后重试。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Close();
            }
        }

        private string GetLoginUrl()
        {
            if (dataSource == WeiboDataSource.WeiboCn)
            {
                return "https://passport.weibo.com/sso/signin?entry=wapsso&source=wapssowb&url=https://weibo.cn";
            }

            if (dataSource == WeiboDataSource.WeiboCnMobile)
            {
                return "https://passport.weibo.com/sso/signin?entry=wapsso&source=wapsso&url=https://m.weibo.cn";
            }

            return "https://passport.weibo.com/sso/signin?entry=miniblog&source=miniblog&url=https://weibo.com/";
        }

        private async void GetCookie_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cookie = await GetCookieAsync();

                if (string.IsNullOrWhiteSpace(Cookie))
                {
                    MessageBox.Show("没有获取到 Cookie，请确认已经扫码登录成功。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取 Cookie 失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> GetCookieAsync()
        {
            if (WebView.CoreWebView2 == null)
            {
                return string.Empty;
            }

            string[] urls = dataSource switch
            {
                WeiboDataSource.WeiboCn => new[]
                {
                    "https://weibo.cn",
                    "https://passport.weibo.com"
                },
                WeiboDataSource.WeiboCnMobile => new[]
                {
                    "https://m.weibo.cn",
                    "https://weibo.cn",
                    "https://passport.weibo.com"
                },
                _ => new[]
                {
                    "https://weibo.com",
                    "https://passport.weibo.com"
                }
            };

            Dictionary<string, CoreWebView2Cookie> cookieMap = new Dictionary<string, CoreWebView2Cookie>();

            foreach (string url in urls)
            {
                IReadOnlyList<CoreWebView2Cookie> cookies =
                    await WebView.CoreWebView2.CookieManager.GetCookiesAsync(url);

                foreach (CoreWebView2Cookie cookie in cookies)
                {
                    cookieMap[cookie.Name] = cookie;
                }
            }

            return string.Join("; ", cookieMap.Values.Select(x => $"{x.Name}={x.Value}"));
        }
    }
}