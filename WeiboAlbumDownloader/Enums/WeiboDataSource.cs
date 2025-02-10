using System.ComponentModel;

namespace WeiboAlbumDownloader.Enums
{
    public enum WeiboDataSource
    {
        [Description("m.weibo.cn")]
        WeiboCnMobile = 0,
        [Description("weibo.cn")]
        WeiboCn = 1,
        [Description("weibo.com1")]
        WeiboCom1 = 2,
        [Description("weibo.com2")]
        WeiboCom2 = 3,
    }
}
