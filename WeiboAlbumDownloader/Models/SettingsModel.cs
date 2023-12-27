namespace WeiboAlbumDownloader.Models
{
    public class SettingsModel
    {
        //weibo.cn cookie
        public string WeiboCnCookie { get; set; }
        //weibo.com cookie
        public string WeiboComCookie { get; set; }
        //推送到微信，填了就会发送
        public string PushPlusToken { get; set; }
        //是否开启Crontab定时任务
        public bool EnableCrontab { get; set; }
        //Crontab定时任务
        public string Crontab { get; set; }
    }
}
