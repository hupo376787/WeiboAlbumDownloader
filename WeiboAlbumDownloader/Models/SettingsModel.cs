using System;
using WeiboAlbumDownloader.Enums;

namespace WeiboAlbumDownloader.Models
{
    public class SettingsModel
    {
        //数据源
        public WeiboDataSource DataSource { get; set; } = WeiboDataSource.WeiboCnMobile;
        //是否显示作者头像
        public bool ShowHeadImage { get; set; } = true;
        //weibo.cn cookie
        public string? WeiboCnCookie { get; set; }
        //weibo.com cookie
        public string? WeiboComCookie { get; set; }
        //推送到微信，填了就会发送
        public string? PushPlusToken { get; set; }
        //是否开启Crontab定时任务
        public bool EnableCrontab { get; set; } = true;
        //Crontab定时任务
        public string? Crontab { get; set; } = "14 2 * * *";
        //用来跳过到下一个uid的计数。如果当前uid下载的时候已存在文件超过此计数，则判定下载过了。-1表示不判定
        public int CountDownloadedSkipToNextUser { get; set; } = 20;
        //是否开启时间范围
        public bool EnableDatetimeRange { get; set; } = false;
        public DateTime? StartDateTime { get; set; }
    }
}
