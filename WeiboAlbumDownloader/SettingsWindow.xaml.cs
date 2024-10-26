using MicaWPF.Controls;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
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
            if (File.Exists("Settings.json"))
            {
                string settingsContent = File.ReadAllText("Settings.json");
                var settings = JsonConvert.DeserializeObject<SettingsModel>(settingsContent);
                TextBox_WeiboCnCookie.Text = settings?.WeiboCnCookie;
                TextBox_WeiboComCookie.Text = settings?.WeiboComCookie;
                TextBox_PushPlusToken.Text = settings?.PushPlusToken;
                ToggleSwitch_Crontab.IsChecked = settings?.EnableCrontab;
                TextBox_Crontab.Text = settings?.Crontab;
                TextBox_SkipCount.Text = settings?.CountDownloadedSkipToNextUser.ToString();
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
            var settings = new SettingsModel()
            {
                WeiboCnCookie = TextBox_WeiboCnCookie.Text,
                WeiboComCookie = TextBox_WeiboComCookie.Text,
                PushPlusToken = TextBox_PushPlusToken.Text,
                EnableCrontab = (bool)ToggleSwitch_Crontab.IsChecked!,
                Crontab = TextBox_Crontab.Text,
                CountDownloadedSkipToNextUser = Convert.ToInt32(TextBox_SkipCount.Text)
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
    }
}
