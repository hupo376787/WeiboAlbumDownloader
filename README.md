# WeiboAlbumDownloader
微博相册下载工具C#版

# 项目说明

本项目可以批量采集指定微博账号下的所有图片（视频）。

![image-20231227192302467](./img/a.jpg)



# 使用说明

获取微博用户uid以及web版微博Cookie，填入到软件根目录的Settings.json中即可。

如果想要在下载完发送push+通知，请填写PushPlusToken字段。不填就不发送。

如果想要开启定时任务，即在某个时间自动触发批量下载任务，那么需要填写EnableCrontab和Crontab字段。

| 字段           | 说明                                                         |
| -------------- | ------------------------------------------------------------ |
| WeiboCnCookie  | [weibo.cn](https://weibo.cn/) Cookie                         |
| WeiboComCookie | [weibo.com](https://weibo.com/) Cookie                       |
| PushPlusToken  | 推送到微信，填了就会发送                                     |
| EnableCrontab  | 否开启Crontab定时任务                                        |
| Crontab        | Crontab定时任务，例如"14 2 * * *"表示每天凌晨2点14分开始执行 |



# 数据源

数据源区分[weibo.com](https://weibo.com/) 和[weibo.cn](https://weibo.cn/) ，[weibo.com](https://weibo.com/) 是获取用户相册的数据（不包含视频），返回的是json格式。 [weibo.cn](https://weibo.cn/) 是获取的用户的时间流数据（包含视频），返回的是html格式。

对于某些用户（可能时间线很长的用户）来说，数据源选择[weibo.com](https://weibo.com/) 可能采集到之前的某一个时间点，就没有数据了。我遇到过一个用户就是这样。



# 获取uid以及Cookie

PC打开[weibo.com](https://weibo.com/)，点击某一用户头像，进入主页。uid就是地址栏中的最后一串数字，比如https://weibo.com/u/1000000000。



按F12进入控制台，网络-全部，在名称栏选择uid，标头-请求标头-Cookie。右键复制后请填入到Seetings.json。



[weibo.com](https://weibo.com/)和[weibo.cn](https://weibo.cn/) cookie不一样，请注意区分。



# 参考

本软件的实现参考/使用了一下项目/技术：

1. [微软WPF](https://learn.microsoft.com/zh-cn/dotnet/desktop/wpf/?view=netdesktop-8.0)，本程序的基础。
2. [MicaWPF](https://github.com/Simnico99/MicaWPF)，实现窗体Mica/Acrylic效果。
3. [Newtonsoft.Json](https://www.newtonsoft.com/)，解析api返回的json数据。
4. [HtmlAgilityPack](https://html-agility-pack.net/)，解析网页返回的html数据。
5. [TimeCrontab](https://github.com/MonkSoul/TimeCrontab)，解析crontab时间数据。
6. [CronExpressionDescriptor](https://github.com/bradymholt/cron-expression-descriptor)，翻译crontab数据为可阅读的文本。
7. [Weibo Spider](https://github.com/dataabc/weiboSpider)，一个开源的很牛逼的微博爬虫。
