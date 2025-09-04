using MicaWPF.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WeiboAlbumDownloader.Enums;
using WeiboAlbumDownloader.Helpers;
using WeiboAlbumDownloader.Models;

namespace WeiboAlbumDownloader
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : MicaWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();
            this.Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox_DataSource.ItemsSource = new List<string>()
            {
                "m.weibo.cn(获取用户的时间流，推荐使用!!!)",
                "weibo.cn    (获取用户的时间流，不过只能获取原创微博相册)",
                "weibo.com (获取相册信息，可以获取原创微博相册、头像相册、自拍相册等。少数用户存在获取失败的问题，怀疑是微博内部api不统一造成的。截止日期仅对微博配图生效)",
                "weibo.com (获取用户的ajax相册，可以获取原创微博相册、头像相册、自拍相册等。但是获取不到博文信息，所以无法重命名图片和修改图片日期。貌似还获取不全。截止日期不生效，不推荐使用！！！)",
            };

            if (File.Exists("Settings.json"))
            {
                string settingsContent = File.ReadAllText("Settings.json");
                var settings = JsonConvert.DeserializeObject<SettingsModel>(settingsContent);
                if (settings?.DataSource == WeiboDataSource.WeiboCnMobile) ComboBox_DataSource.SelectedIndex = 0;
                else if (settings?.DataSource == WeiboDataSource.WeiboCn) ComboBox_DataSource.SelectedIndex = 1;
                else if (settings?.DataSource == WeiboDataSource.WeiboCom1) ComboBox_DataSource.SelectedIndex = 2;
                else if (settings?.DataSource == WeiboDataSource.WeiboCom2) ComboBox_DataSource.SelectedIndex = 3;

                TextBox_WeiboCnCookie.Text = settings?.WeiboCnCookie;
                TextBox_WeiboComCookie.Text = settings?.WeiboComCookie;
                TextBox_PushPlusToken.Text = settings?.PushPlusToken;
                ToggleSwitch_ShowHeadImage.IsChecked = settings?.ShowHeadImage;
                ToggleSwitch_EnableDownloadVideo.IsChecked = settings?.EnableDownloadVideo;
                ToggleSwitch_EnableDownloadLivePhoto.IsChecked = settings?.EnableDownloadLivePhoto;
                ToggleSwitch_EnableShortenName.IsChecked = settings?.EnableShortenName;
                ToggleSwitch_Crontab.IsChecked = settings?.EnableCrontab;
                TextBox_Crontab.Text = settings?.Crontab;
                TextBox_SkipCount.Text = settings?.CountDownloadedSkipToNextUser.ToString();
                ToggleSwitch_DatetimeRange.IsChecked = settings?.EnableDatetimeRange;
                DatePicker_Start.SelectedDate = settings?.StartDateTime;
            }
        }

        private void GetCookie(object sender, RoutedEventArgs e)
        {
            var tag = (sender as MicaWPF.Controls.Button)?.Tag as string;
            if (tag == "cn")
            {
                TextBox_WeiboCnCookie.Text = SeleniumHelper.GetCookie(Enums.WeiboDataSource.WeiboCnMobile);
            }
            else if (tag == "com")
            {
                TextBox_WeiboComCookie.Text = SeleniumHelper.GetCookie(Enums.WeiboDataSource.WeiboCom1);
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            //先判断日期是否合法
            if (ToggleSwitch_DatetimeRange.IsChecked == true)
            {
                if (DatePicker_Start.SelectedDate == null)
                {
                    MessageBox.Show("启用时间范围后，必须要设置起始日期。");
                    return;
                }
                else
                {
                    if (DatePicker_Start.SelectedDate > DateTime.Now)
                    {
                        MessageBox.Show("启用时间范围后，起始日期不能是未来的时间。");
                        return;
                    }
                }
            }

            var ds = WeiboDataSource.WeiboCnMobile;
            if (ComboBox_DataSource.SelectedIndex == 0)
            {
                ds = WeiboDataSource.WeiboCnMobile;
            }
            else if (ComboBox_DataSource.SelectedIndex == 1)
            {
                ds = WeiboDataSource.WeiboCn;
            }
            else if (ComboBox_DataSource.SelectedIndex == 2)
            {
                ds = WeiboDataSource.WeiboCom1;
            }
            else if (ComboBox_DataSource.SelectedIndex == 3)
            {
                ds = WeiboDataSource.WeiboCom2;
            }

            //再写入Json
            var settings = new SettingsModel()
            {
                DataSource = ds,
                WeiboCnCookie = TextBox_WeiboCnCookie.Text,
                WeiboComCookie = TextBox_WeiboComCookie.Text,
                PushPlusToken = TextBox_PushPlusToken.Text,
                ShowHeadImage = (bool)ToggleSwitch_ShowHeadImage.IsChecked!,
                EnableDownloadVideo = (bool)ToggleSwitch_EnableDownloadVideo.IsChecked!,
                EnableDownloadLivePhoto = (bool)ToggleSwitch_EnableDownloadLivePhoto.IsChecked!,
                EnableShortenName = (bool)ToggleSwitch_EnableShortenName.IsChecked!,
                EnableCrontab = (bool)ToggleSwitch_Crontab.IsChecked!,
                Crontab = TextBox_Crontab.Text,
                CountDownloadedSkipToNextUser = string.IsNullOrEmpty(TextBox_SkipCount.Text) ? 20 : Convert.ToInt32(TextBox_SkipCount.Text),
                EnableDatetimeRange = (bool)ToggleSwitch_DatetimeRange.IsChecked!,
                StartDateTime = DatePicker_Start.SelectedDate,
            };
            File.WriteAllText("Settings.json", JsonConvert.SerializeObject(settings, Formatting.Indented));
            this.Close();
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            TextBox_Crontab.IsEnabled = true;
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            TextBox_Crontab.IsEnabled = false;
        }

        private void OpenPushPlus(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.pushplus.plus/uc.html") { UseShellExecute = true });
        }

        private void ToggleSwitch_DatetimeRangeChecked(object sender, RoutedEventArgs e)
        {
            DatePicker_Start.IsEnabled = true;
        }

        private void ToggleSwitch_DatetimeRangeUnchecked(object sender, RoutedEventArgs e)
        {
            DatePicker_Start.IsEnabled = false;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start("explorer.exe", e.Uri.AbsoluteUri);
        }

        private void OpenGithub(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/hupo376787/WeiboAlbumDownloader") { UseShellExecute = true });
        }
    }
}
