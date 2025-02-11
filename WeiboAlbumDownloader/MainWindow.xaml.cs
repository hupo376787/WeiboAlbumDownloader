using HtmlAgilityPack;
using MicaWPF.Controls;
using Newtonsoft.Json;
using Sentry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using TimeCrontab;
using WeiboAlbumDownloader.Enums;
using WeiboAlbumDownloader.Helpers;
using WeiboAlbumDownloader.Models;

namespace WeiboAlbumDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MicaWindow
    {
        //发行说明：
        //①此处升级一下GlobalVar版本号
        //②Github/Gitee release新建一个新版本Tag
        //③上传压缩包删除Settings.json以及uidList.txt
        public static double currentVersion = 5.1;

        /// <summary>
        /// com1是根据uid获取相册id，https://photo.weibo.com/albums/get_all?uid=10000000000&page=1；根据uid和相册id以及相册type获取图片列表，https://photo.weibo.com/photos/get_all?uid=10000000000&album_id=3959362334782071&page=1&type=3
        /// com2是根据uid获取相册id，https://weibo.com/ajax/profile/getImageWall?uid=10000000000&sinceid=0&has_album=true；根据相册id和sinceid获取图片列表，https://weibo.com/ajax/profile/getAlbumDetail?containerid=123456789000123456_-_pc_profile_album_-_photo_-_camera_-_0_-_%25E5%258E%259F%25E5%2588%259B&since_id=0
        /// cn是从 https://weibo.cn/10000000000/profile?page=2获取html解析发布的微博
        /// m.cn是移动端api，可以从 https://m.weibo.cn/api/container/getIndex?type=uid&value=10000000000&containerid=10760310000000000&since_id=5040866311798795，since_id第一次为空，containerid以107603开头是获取时间线，100505开头是个人资料
        /// </summary>
        private WeiboDataSource dataSource = WeiboDataSource.WeiboCnMobile;
        private int countPerPage = 100;
        private string downloadFolder = Environment.CurrentDirectory + "//DownLoad//";
        private SettingsModel? settings;
        private string? cookie;
        private List<string> uids = new List<string>();
        //用来跳过到下一个uid的计数。如果当前uid下载的时候已存在文件超过此计数，则判定下载过了。
        private int countDownloadedSkipToNextUser;
        private bool isDownloading;
        private CancellationTokenSource? cancellationTokenSource;
        public ObservableCollection<MessageModel> Messages { get; set; } = new ObservableCollection<MessageModel>();

        public MainWindow()
        {
            InitializeComponent();

            _ = GetVersion();
            InitUidsData();
            InitSettingsData();

            Directory.CreateDirectory(downloadFolder);
            ListView_Messages.ItemsSource = Messages;

            //定时任务
            if (settings?.Crontab != null)
            {
                var cron = Crontab.Parse(settings?.Crontab);
                Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        await Task.Delay((int)cron.GetSleepMilliseconds(DateTime.Now));
                        AppendLog(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "开启定时任务");

                        foreach (var item in uids)
                        {
                            await Start(item.Trim());
                            AppendLog("等待60s，下载下一个用户相册数据");
                            await Task.Delay(60 * 1000);
                        }

                        AppendLog(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "结束定时任务");
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        private async Task GetVersion()
        {
            //AppendLog($"当前程序版本 V{GlobalVar.currentVersion}");
            var latestVersionString = await GithubHelper.GetLatestVersion();
            if (string.IsNullOrEmpty(latestVersionString) || latestVersionString?.Length > 5)
            {
                latestVersionString = await GithubHelper.GetGiteeLatestVersion();
            }
            AppendLog($"当前程序版本 V{currentVersion}，最新版为 V{latestVersionString}");

            var res = double.TryParse(latestVersionString, out double latestVersion);
            if (res)
            {
                if (latestVersion > currentVersion)
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        var uiMessageBox = new MicaWPF.Dialogs.ContentDialog
                        {
                            Title = "提示",
                            Content = $"检测到新版本 V{latestVersionString}，当前程序版本 V{currentVersion}，点击确定下载",
                            PrimaryButtonText = "OK"
                        };

                        var res = await uiMessageBox.ShowAsync();
                        if (res == MicaWPF.Core.Enums.ContentDialogResult.PrimaryButton)
                        {
                            Process.Start(new ProcessStartInfo("https://github.com/hupo376787/WeiboAlbumDownloader/releases") { UseShellExecute = true });
                        }
                    });
                }
            }
        }

        private async Task Start(string userId)
        {
            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = new CancellationTokenSource();

                var conv = long.TryParse(userId, out long temp);
                if (string.IsNullOrEmpty(userId) || !conv)
                {
                    AppendLog("错误的微博uid，参考上面说明获取uid", MessageEnum.Error);
                    return;
                }

                //用于Sentry崩溃收集
                GlobalVar.gId = userId;
                GlobalVar.gDataSource = dataSource = settings.DataSource;

                //读取用户列表和设置
                InitSettingsData();

                await Task.Run(async () =>
                {
                    bool isSkip = false;

                    //四种api下载
                    {
                        string nickName = string.Empty;
                        string headUrl = string.Empty;
                        countDownloadedSkipToNextUser = 0;
                        isSkip = false;
                        AppendLog("开始下载" + userId, MessageEnum.Info);

                        //源是weibo.com的时候，获取的是获取相册列表，json格式
                        if (dataSource == WeiboDataSource.WeiboCom1)
                        {
                            string albumsUrl = $"https://photo.weibo.com/albums/get_all?uid={userId}&page=1";
                            TextBlock_UID?.Dispatcher.InvokeAsync(() =>
                            {
                                TextBlock_UID.Text = userId;
                            });

                            var albums = await HttpHelper.GetAsync<UserAlbumModel>(albumsUrl, dataSource, cookie!);
                            Directory.CreateDirectory(downloadFolder + userId);
                            if (albums != null)
                            {
                                foreach (var item in albums?.data?.album_list!)
                                {
                                    if (isSkip)
                                        break;

                                    Directory.CreateDirectory(downloadFolder + userId + "//" + item.caption);

                                    if (item.caption == "头像相册")
                                    {
                                        headUrl = item.cover_pic;
                                    }

                                    int page = 1;
                                    while (true)
                                    {
                                        if (isSkip)
                                            break;

                                        GlobalVar.gPage = page;

                                        string albumUrl = $"https://photo.weibo.com/photos/get_all?uid={userId}&album_id={item.album_id}&count={countPerPage}&page={page}&type={item.type}";
                                        var photos = await HttpHelper.GetAsync<AlbumDetailModel>(albumUrl, dataSource, cookie!);
                                        if (photos != null && photos.data?.photo_list != null && photos.data?.photo_list.Count > 0)
                                        {
                                            int photoCount = 1;
                                            string oldCaption = "";
                                            foreach (var photo in photos.data?.photo_list!)
                                            {
                                                if (cancellationTokenSource.IsCancellationRequested)
                                                {
                                                    AppendLog("用户手动终止。", MessageEnum.Info);
                                                    return;
                                                }

                                                if (oldCaption.Equals(photo.caption_render))
                                                {
                                                    photoCount++;
                                                }
                                                else
                                                {
                                                    photoCount = 1;
                                                }

                                                oldCaption = photo.caption_render;

                                                string photoUrl = photo.pic_host + "/large/" + photo.pic_name;
                                                DateTime timestamp = DateTime.UnixEpoch.AddSeconds(photo.timestamp + 8 * 3600);

                                                //时间范围过滤，比设置日期早的跳过
                                                if ((bool)settings?.EnableDatetimeRange)
                                                {
                                                    if (item.caption == "微博配图" && timestamp < settings?.StartDateTime)
                                                    {
                                                        AppendLog($"翻页到截至日期{settings?.StartDateTime}，停止下载");
                                                        return;
                                                    }
                                                    //if (card?.ProfileTypeId == "proweibotop_" && timestamp < settings?.StartDateTime)
                                                    //    continue;
                                                    //else if (card?.ProfileTypeId == "proweibotop_" && timestamp >= settings?.StartDateTime)
                                                    //{

                                                    //}
                                                    //else if (timestamp < settings?.StartDateTime)
                                                    //{
                                                    //    AppendLog($"翻页到截至日期{settings?.StartDateTime}，停止下载");
                                                    //    return;
                                                    //}
                                                }

                                                var invalidChar = System.IO.Path.GetInvalidFileNameChars();
                                                var newCaption = invalidChar.Aggregate(photo.caption_render, (o, r) => (o.Replace(r.ToString(), string.Empty)));
                                                var fileName = downloadFolder + userId + "//" + item.caption + "//"
                                                    + timestamp.ToString("yyyy-MM-dd HH_mm_ss") + newCaption + "_" + photoCount + ".jpg";
                                                Debug.WriteLine(fileName);
                                                if (File.Exists(fileName))
                                                {
                                                    AppendLog("文件已存在，跳过下载" + Path.GetFullPath(fileName), MessageEnum.Warning);
                                                    countDownloadedSkipToNextUser++;
                                                    await Task.Delay(500);
                                                    continue;
                                                }

                                                //传入图片/视频的名字，开始下载图片/视频
                                                try
                                                {
                                                    await HttpHelper.GetAsync<AlbumDetailModel>(photoUrl, dataSource, cookie!, fileName);

                                                    AppendLog("已完成 " + Path.GetFileName(fileName), MessageEnum.Success);

                                                    File.SetCreationTime(fileName, timestamp);
                                                    File.SetLastWriteTime(fileName, timestamp);
                                                    File.SetLastAccessTime(fileName, timestamp);
                                                }
                                                catch (Exception ex)
                                                {
                                                    AppendLog($"文件下载失败，原始url：{item}，下载路径{fileName}", MessageEnum.Error);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }

                                        //已存在的文件超过设置值，判定该用户下载过了
                                        if (settings!.CountDownloadedSkipToNextUser > 0 && countDownloadedSkipToNextUser > settings.CountDownloadedSkipToNextUser)
                                        {
                                            AppendLog($"已存在的文件{countDownloadedSkipToNextUser}超过设置值{settings.CountDownloadedSkipToNextUser}，跳到下一个用户", MessageEnum.Info);
                                            isSkip = true;
                                            break;
                                        }

                                        page++;
                                        //通过加入随机等待避免被限制。爬虫速度过快容易被系统限制(一段时间后限制会自动解除)，加入随机等待模拟人的操作，可降低被系统限制的风险。默认是每爬取1到5页随机等待6到10秒，如果仍然被限，可适当增加sleep时间
                                        Random rd = new Random();
                                        int rnd = rd.Next(5000, 10000);
                                        AppendLog($"随机等待{rnd}ms，避免爬虫速度过快被系统限制", MessageEnum.Info);
                                        await Task.Delay(rd.Next(5000, 10000));
                                    }
                                }
                            }
                        }
                        //ajax获取相册方法
                        else if (dataSource == WeiboDataSource.WeiboCom2)
                        {
                            string albumsUrl = $"https://weibo.com/ajax/profile/getImageWall?uid={userId}&sinceid=0&has_album=true";
                            TextBlock_UID?.Dispatcher.InvokeAsync(() =>
                            {
                                TextBlock_UID.Text = userId;
                            });

                            var albums = await HttpHelper.GetAsync<UserAlbumModel2>(albumsUrl, dataSource, cookie!);
                            Directory.CreateDirectory(downloadFolder + userId);
                            if (albums != null)
                            {
                                foreach (var item in albums?.Data?.AlbumList!)
                                {
                                    if (isSkip)
                                        break;

                                    if (item.PicTitle == "头像相册")
                                    {
                                        headUrl = item.Pic;
                                    }

                                    Directory.CreateDirectory(downloadFolder + userId + "//" + item.PicTitle);

                                    long sinceId = 0;
                                    while (true)
                                    {
                                        if (isSkip)
                                            break;

                                        string albumUrl = $"https://weibo.com/ajax/profile/getAlbumDetail?containerid={item.Containerid}&since_id={sinceId}";
                                        var photos = await HttpHelper.GetAsync<AlbumDetailModel2>(albumUrl, dataSource, cookie!);
                                        if (photos != null && photos.PhotoListData2?.PhotoListItem2 != null && photos.PhotoListData2?.PhotoListItem2.Count > 0)
                                        {
                                            GlobalVar.gSinceId = sinceId = photos.PhotoListData2.SinceId;
                                            foreach (var photo in photos.PhotoListData2?.PhotoListItem2!)
                                            {
                                                if (cancellationTokenSource.IsCancellationRequested)
                                                {
                                                    AppendLog("用户手动终止。", MessageEnum.Info);
                                                    return;
                                                }

                                                string photoUrl = "https://wx4.sinaimg.cn/large/" + photo.Pid + ".jpg";

                                                var fileName = downloadFolder + userId + "//" + item.PicTitle + "//" + photo.Pid + ".jpg";
                                                Debug.WriteLine(fileName);
                                                if (File.Exists(fileName))
                                                {
                                                    AppendLog("文件已存在，跳过下载" + Path.GetFullPath(fileName), MessageEnum.Warning);
                                                    countDownloadedSkipToNextUser++;
                                                    await Task.Delay(500);
                                                    continue;
                                                }

                                                //传入图片/视频的名字，开始下载图片/视频
                                                try
                                                {
                                                    await HttpHelper.GetAsync<AlbumDetailModel>(photoUrl, dataSource, cookie!, fileName);

                                                    AppendLog("已完成 " + Path.GetFileName(fileName), MessageEnum.Success);
                                                }
                                                catch (Exception ex)
                                                {
                                                    AppendLog($"文件下载失败，原始url：{item}，下载路径{fileName}", MessageEnum.Error);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }

                                        //已存在的文件超过设置值，判定该用户下载过了
                                        if (settings!.CountDownloadedSkipToNextUser > 0 && countDownloadedSkipToNextUser > settings.CountDownloadedSkipToNextUser)
                                        {
                                            AppendLog($"已存在的文件{countDownloadedSkipToNextUser}超过设置值{settings.CountDownloadedSkipToNextUser}，跳到下一个用户", MessageEnum.Info);
                                            isSkip = true;
                                            break;
                                        }

                                        //通过加入随机等待避免被限制。爬虫速度过快容易被系统限制(一段时间后限制会自动解除)，加入随机等待模拟人的操作，可降低被系统限制的风险。默认是每爬取1到5页随机等待6到10秒，如果仍然被限，可适当增加sleep时间
                                        Random rd = new Random();
                                        int rnd = rd.Next(5000, 10000);
                                        AppendLog($"随机等待{rnd}ms，避免爬虫速度过快被系统限制", MessageEnum.Info);
                                        await Task.Delay(rd.Next(5000, 10000));
                                    }
                                }
                            }
                        }
                        //源是weibo.cn的时候，获取的是微博时间线列表，解析html格式
                        else if (dataSource == WeiboDataSource.WeiboCn)
                        {
                            int page = 1;
                            int totalPage = -1;
                            bool cachedUserInfo = false;

                            Directory.CreateDirectory(downloadFolder + userId + "//" + "微博配图");
                            while (true)
                            {
                                if (isSkip)
                                    break;

                                GlobalVar.gPage = page;

                                //filter，0-全部；1-原创；2-图片
                                //https://weibo.cn/xxxxxxxxxxxxx?page=2
                                //https://weibo.cn/xxxxxxxxxxxxx/profile?page=2
                                string url = $"https://weibo.cn/{userId}?page={page}&filter=1";
                                string text = await HttpHelper.GetAsync<string>(url, dataSource, cookie!);
                                var doc = new HtmlDocument();
                                doc.LoadHtml(text);

                                //获取总页数
                                if (totalPage == -1)
                                {
                                    var totalPageHtml = doc.DocumentNode.Descendants("input").Where(x => x.Attributes["type"]?.Value == "hidden").ToList();
                                    if (totalPageHtml.Count > 0)
                                    {
                                        totalPage = Convert.ToInt32(totalPageHtml[0].Attributes["value"].Value);
                                        AppendLog($"获取到{totalPage}页数据", MessageEnum.Info);
                                    }
                                }
                                //当只有一页数据的时候，totalPage获取不到。需要叠加判定doc.DocumentNode.Descendants("div").Where(x => x.Attributes["class"]?.Value == "c").ToList().Count
                                if (totalPage == -1 && doc.DocumentNode.Descendants("div").Where(x => x.Attributes["class"]?.Value == "c").ToList().Count == 0)
                                {
                                    AppendLog("获取总页数失败，请重新登录https://weibo.cn/获取Cookie重试", MessageEnum.Error);
                                    return;
                                }

                                //获取用户资料
                                if (!cachedUserInfo)
                                {
                                    var userInfoXmlString = doc.DocumentNode.Descendants("div").Where(x => x.Attributes["class"]?.Value == "u").ToList()?[0].OuterHtml;
                                    var docUser = new HtmlDocument();
                                    docUser.LoadHtml(userInfoXmlString);
                                    string temp = docUser.DocumentNode.Descendants("img").Where(x => x.Attributes["alt"]?.Value == "头像").ToList()?[0].Attributes["src"].Value!;
                                    nickName = docUser.DocumentNode.Descendants("span").Where(x => x.Attributes["class"]?.Value == "ctt").ToList()?[0].InnerText.Split("&nbsp;")[0]!;
                                    using (File.Create(downloadFolder + userId + "//" + nickName)) { }

                                    var desc = docUser.DocumentNode.Descendants("div").Where(x => x.Attributes["class"]?.Value == "tip2").ToList()?[0].InnerText.Split("&nbsp;");
                                    string weiboDesc = string.Join(" ", desc!);

                                    headUrl = "https://tvax2.sinaimg.cn/large/" + Path.GetFileName(temp).Split("?")[0];
                                    var fileName = downloadFolder + userId + "//" + Path.GetFileName(headUrl);
                                    //下载头像
                                    if (!File.Exists(fileName))
                                    {
                                        await HttpHelper.GetAsync<AlbumDetailModel>(headUrl, dataSource, cookie!, fileName);
                                    }

                                    Image_Head?.Dispatcher.InvokeAsync(() =>
                                    {
                                        if (settings.ShowHeadImage)
                                        {
                                            var bytes = File.ReadAllBytes(fileName);
                                            MemoryStream ms = new MemoryStream(bytes);
                                            BitmapImage bi = new BitmapImage();
                                            bi.BeginInit();
                                            bi.StreamSource = ms;
                                            bi.EndInit();

                                            Image_Head.ImageSource = bi;

                                            //Image_Head.ImageSource = new BitmapImage(new Uri(fileName));
                                        }
                                        TextBlock_UID!.Text = userId;
                                        TextBlock_NickName.Text = nickName;
                                        TextBlock_WeiboDesc.Text = weiboDesc;
                                    });

                                    cachedUserInfo = true;
                                }

                                //获取当前页的微博
                                var nodes = doc.DocumentNode.Descendants("div").Where(x => x.Attributes["class"]?.Value == "c").ToList();
                                foreach (var node in nodes)
                                {
                                    if (isSkip)
                                        break;

                                    if (cancellationTokenSource.IsCancellationRequested)
                                    {
                                        AppendLog("用户手动终止。", MessageEnum.Info);
                                        return;
                                    }

                                    // 使用OuterXml属性将HtmlNode对象转换为Xml字符串
                                    string xmlString = node.OuterHtml;
                                    //页尾html调过解析
                                    if (xmlString.Contains("设置") && xmlString.Contains("图片") && xmlString.Contains("条数") && xmlString.Contains("隐私"))
                                        continue;

                                    var doc1 = new HtmlDocument();
                                    doc1.LoadHtml(xmlString);

                                    //微博内容
                                    var weiboContent = doc1.DocumentNode.Descendants("span").Where(x => x.Attributes["class"]?.Value == "ctt").ToList()[0].InnerText;
                                    //微博来源
                                    var temp = doc1.DocumentNode.Descendants("span").Where(x => x.Attributes["class"]?.Value == "ct").ToList()[0].InnerText;
                                    var sourceDevice = temp.Split("&nbsp;").Length > 1 ? temp.Split("&nbsp;")[1] : "";
                                    //发布时间
                                    var timestamp = DateTime.Parse(temp.Split("&nbsp;")[0].Replace("今天", ""));

                                    //图片列表链接
                                    string photoListUrl = string.Empty;
                                    //视频链接
                                    string videoUrl = string.Empty;
                                    //转评赞
                                    int likeCount, repostCount, commentCount;
                                    //是单图、组图、视频
                                    PicEnum picType = PicEnum.Picture;
                                    var list = doc1.DocumentNode.Descendants("a").ToList();
                                    foreach (var item in list)
                                    {
                                        if (isSkip)
                                            break;

                                        if (item.InnerText.Contains("组图共"))
                                        {
                                            photoListUrl = item.Attributes["href"].Value;
                                            picType = PicEnum.Pictures;
                                        }
                                        else if (item.Attributes["href"].Value.Contains("s/video/show"))
                                        {
                                            videoUrl = item.Attributes["href"].Value.Replace("s/video/show", "s/video/object");
                                            picType = PicEnum.Video;
                                        }
                                        else if (item.InnerText.Contains("赞"))
                                        {
                                            likeCount = Convert.ToInt32(item.InnerText.Replace("赞[", "").Replace("]", ""));
                                        }
                                        else if (item.InnerText.Contains("转发"))
                                        {
                                            repostCount = Convert.ToInt32(item.InnerText.Replace("转发[", "").Replace("]", ""));
                                        }
                                        else if (item.InnerText.Contains("评论"))
                                        {
                                            commentCount = Convert.ToInt32(item.InnerText.Replace("评论[", "").Replace("]", ""));
                                        }
                                    }
                                    //如果已赞，那么获取方式是下面的
                                    try
                                    {
                                        likeCount = Convert.ToInt32(doc1.DocumentNode.Descendants("span").Where(x => x.Attributes["class"]?.Value == "cmt").ToList()[0].InnerText.Replace("已赞[", "").Replace("]", ""));
                                    }
                                    catch { }

                                    //获取图片列表中的每一个图片的原图超链接url
                                    List<string> originalPics = new List<string>();
                                    if (picType == PicEnum.Pictures)
                                    {
                                        text = await HttpHelper.GetAsync<string>(photoListUrl, dataSource, cookie!);
                                        var doc2 = new HtmlDocument();
                                        doc2.LoadHtml(text);
                                        list = doc2.DocumentNode.Descendants("img").ToList().ToList();
                                        foreach (var item in list)
                                        {
                                            var photoUrl = "https://wx4.sinaimg.cn/large/" + Path.GetFileName(item.Attributes["src"]?.Value);
                                            originalPics.Add(photoUrl);
                                        }
                                    }
                                    else if (picType == PicEnum.Picture)
                                    {
                                        list = doc1.DocumentNode.Descendants("a").ToList();
                                        foreach (var item in list)
                                        {
                                            if (item.InnerText.Contains("原图"))
                                            {
                                                originalPics.Add("https://wx4.sinaimg.cn/large/" + item.Attributes["href"].Value.Split("u=")[1] + ".jpg");
                                                break;
                                            }
                                        }
                                    }
                                    else if (picType == PicEnum.Video)
                                    {
                                        try
                                        {
                                            var res = await HttpHelper.GetAsync<VideoDetailModel>(videoUrl, dataSource, cookie!);
                                            if (res != null && res.ok == 1)
                                            {
                                                originalPics.Add(res?.data?.@object?.stream?.hd_url!);
                                            }
                                        }
                                        catch
                                        {
                                            AppendLog($"视频解析错误，原始url：{videoUrl}", MessageEnum.Error);
                                        }
                                    }

                                    //下载获取图片列表中的图片原图
                                    int photoCount = 1;
                                    foreach (var item in originalPics)
                                    {
                                        if (isSkip)
                                            break;

                                        if (string.IsNullOrEmpty(item))
                                            continue;

                                        //替换非法字符
                                        var invalidChar = Path.GetInvalidFileNameChars();
                                        var newCaption = invalidChar.Aggregate(weiboContent, (o, r) => (o.Replace(r.ToString(), string.Empty)));
                                        var fileName = downloadFolder + userId + "//" + "微博配图" + "//"
                                            + timestamp.ToString("yyyy-MM-dd HH_mm_ss") + newCaption + "_" + photoCount;
                                        //后缀名区分图片/视频
                                        if (picType == PicEnum.Video)
                                            fileName += ".mp4";
                                        else
                                            fileName += ".jpg";
                                        Debug.WriteLine(fileName);

                                        //已存在的文件超过设置值，判定该用户下载过了
                                        if (settings!.CountDownloadedSkipToNextUser > 0 && countDownloadedSkipToNextUser > settings.CountDownloadedSkipToNextUser)
                                        {
                                            AppendLog($"已存在的文件{countDownloadedSkipToNextUser}超过设置值{settings.CountDownloadedSkipToNextUser}，跳到下一个用户", MessageEnum.Info);
                                            isSkip = true;
                                        }

                                        //已经下载过的跳过
                                        if (File.Exists(fileName))
                                        {
                                            AppendLog("文件已存在，跳过下载" + Path.GetFullPath(fileName), MessageEnum.Warning);
                                            countDownloadedSkipToNextUser++;
                                            await Task.Delay(500);
                                            continue;
                                        }

                                        //传入图片/视频的名字，开始下载图片/视频
                                        try
                                        {
                                            await HttpHelper.GetAsync<AlbumDetailModel>(item, dataSource, cookie!, fileName);

                                            //修改文件日期时间为发博的时间
                                            File.SetCreationTime(fileName, timestamp);
                                            File.SetLastWriteTime(fileName, timestamp);
                                            File.SetLastAccessTime(fileName, timestamp);

                                            AppendLog("已完成 " + Path.GetFileName(fileName), MessageEnum.Success);
                                        }
                                        catch (Exception ex)
                                        {
                                            AppendLog($"文件下载失败，原始url：{item}，下载路径{fileName}", MessageEnum.Error);
                                        }

                                        photoCount++;
                                    }
                                }

                                page++;
                                if (page > totalPage)
                                {
                                    break;
                                }
                                //通过加入随机等待避免被限制。爬虫速度过快容易被系统限制(一段时间后限制会自动解除)，加入随机等待模拟人的操作，可降低被系统限制的风险。默认是每爬取1到5页随机等待6到10秒，如果仍然被限，可适当增加sleep时间
                                Random rd = new Random();
                                int rnd = rd.Next(5000, 10000);
                                AppendLog($"随机等待{rnd}ms，避免爬虫速度过快被系统限制", MessageEnum.Info);
                                await Task.Delay(rnd);
                            }
                        }
                        //通过m.weibo.cn移动端api获取
                        else if (dataSource == WeiboDataSource.WeiboCnMobile)
                        {
                            //https://m.weibo.cn/api/container/getIndex?type=uid&value=10000000000&containerid=10760310000000000&since_id=5040866311798795
                            long sinceId = 0;
                            bool cachedUserInfo = false;
                            string personalFolder = userId;
                            int page = 1;

                            while (true)
                            {
                                if (isSkip)
                                    break;

                                string url = $"https://m.weibo.cn/api/container/getIndex?type=uid&value={userId}&containerid=107603{userId}&since_id={sinceId}";
                                var res = await HttpHelper.GetAsync<WeiboCnMobileModel>(url, dataSource, cookie!);
                                if (res != null && res?.Ok == 1 && res?.Data != null && res?.Data?.Cards != null && res?.Data?.Cards?.Count > 0)
                                {
                                    if (res?.Data?.CardlistInfo?.SinceId != null)
                                        GlobalVar.gSinceId = sinceId = (long)res?.Data?.CardlistInfo?.SinceId!;
                                    AppendLog($"获取到{res?.Data?.CardlistInfo?.Total}条数据，正在下载第{page}页", MessageEnum.Info);

                                    if (res?.Data?.Cards?[0]?.Mblog?.User?.ScreenName == null)
                                        return;

                                    //获取用户资料
                                    if (!cachedUserInfo)
                                    {
                                        nickName = res?.Data?.Cards?[0]?.Mblog?.User?.ScreenName!;
                                        personalFolder = $"{nickName}({userId})";
                                        Directory.CreateDirectory(downloadFolder + "//" + personalFolder);
                                        using (File.Create(downloadFolder + "//" + personalFolder + "//" + nickName)) { }

                                        headUrl = "https://tvax2.sinaimg.cn/large/" + Path.GetFileName(res?.Data?.Cards?[0]?.Mblog?.User?.AvatarHd!).Split("?")[0];
                                        var fileName = downloadFolder + "//" + personalFolder + "//" + Path.GetFileName(headUrl);
                                        //下载头像
                                        if (!File.Exists(fileName))
                                        {
                                            await HttpHelper.GetAsync<AlbumDetailModel>(headUrl, dataSource, cookie!, fileName);
                                        }


                                        Image_Head?.Dispatcher.InvokeAsync(() =>
                                        {
                                            if (settings.ShowHeadImage)
                                            {
                                                var bytes = File.ReadAllBytes(fileName);
                                                MemoryStream ms = new MemoryStream(bytes);
                                                BitmapImage bi = new BitmapImage();
                                                bi.BeginInit();
                                                bi.StreamSource = ms;
                                                bi.EndInit();

                                                Image_Head.ImageSource = bi;

                                                //Image_Head.ImageSource = new BitmapImage(new Uri(fileName));
                                            }
                                            TextBlock_UID!.Text = userId;
                                            TextBlock_NickName.Text = nickName;
                                            TextBlock_WeiboDesc.Text = res?.Data?.Cards?[0]?.Mblog?.User?.Description!;
                                        });

                                        cachedUserInfo = true;
                                    }

                                    //获取图片列表中的每一个图片的原图超链接url
                                    List<string> originalPics = new List<string>();
                                    List<string> originalVideos = new List<string>();
                                    List<string> originalLivePhotos = new List<string>();
                                    string weiboContent = "";
                                    DateTime timestamp = DateTime.Now;

                                    foreach (var card in res?.Data?.Cards!)
                                    {
                                        if (isSkip)
                                            break;
                                        //9是微博，RetweetedStatus是转发
                                        if (card?.CardType != 9 || card?.Mblog?.RetweetedStatus != null)
                                            continue;

                                        if (cancellationTokenSource.IsCancellationRequested)
                                        {
                                            AppendLog("用户手动终止。", MessageEnum.Info);
                                            return;
                                        }
                                        originalPics.Clear();
                                        originalVideos.Clear();
                                        originalLivePhotos.Clear();
                                        //weiboContent = card?.Mblog?.Text!;
                                        // 使用正则表达式去除 <a> 和 <span> 标签及其内容
                                        string result = Regex.Replace(card?.Mblog?.Text!, @"<a.*?>.*?</a>|<span.*?>.*?</span>", string.Empty);
                                        // 去除其他不需要的标签（如 <br />）
                                        weiboContent = Regex.Replace(result, @"<.*?>", string.Empty);


                                        string format = "ddd MMM dd HH:mm:ss K yyyy"; // 定义日期格式

                                        timestamp = DateTime.ParseExact(card?.Mblog?.CreatedAt!, format, System.Globalization.CultureInfo.InvariantCulture);

                                        //时间范围过滤，比设置日期早的跳过
                                        if ((bool)settings?.EnableDatetimeRange)
                                        {
                                            if (card?.ProfileTypeId == "proweibotop_" && timestamp < settings?.StartDateTime)
                                                continue;
                                            else if (card?.ProfileTypeId == "proweibotop_" && timestamp >= settings?.StartDateTime)
                                            {

                                            }
                                            else if (timestamp < settings?.StartDateTime)
                                            {
                                                AppendLog($"翻页到截至日期{settings?.StartDateTime}，停止下载");
                                                return;
                                            }
                                        }


                                        if (card?.Mblog?.PicIds != null && (bool)card?.Mblog?.PicIds?.Any()!)
                                        {
                                            foreach (var item in card?.Mblog?.PicIds!)
                                            {
                                                var photoUrl = "https://wx4.sinaimg.cn/large/" + Path.GetFileName(item) + ".jpg";
                                                originalPics.Add(photoUrl);
                                            }
                                        }
                                        if (card?.Mblog?.LivePhoto != null && (bool)(card?.Mblog?.LivePhoto?.Any()!))
                                        {
                                            foreach (var item in card?.Mblog?.LivePhoto!)
                                            {
                                                originalLivePhotos.Add(item);
                                            }
                                        }
                                        //选最高清晰度
                                        if (card?.Mblog?.PageInfo?.Urls?.Mp48kMp4 != null)
                                        {
                                            originalVideos.Add(card?.Mblog?.PageInfo?.Urls?.Mp48kMp4!);
                                        }
                                        else if (card?.Mblog?.PageInfo?.Urls?.Mp44kMp4 != null)
                                        {
                                            originalVideos.Add(card?.Mblog?.PageInfo?.Urls?.Mp44kMp4!);
                                        }
                                        else if (card?.Mblog?.PageInfo?.Urls?.Mp42kMp4 != null)
                                        {
                                            originalVideos.Add(card?.Mblog?.PageInfo?.Urls?.Mp42kMp4!);
                                        }
                                        else if (card?.Mblog?.PageInfo?.Urls?.Mp41080pMp4 != null)
                                        {
                                            originalVideos.Add(card?.Mblog?.PageInfo?.Urls?.Mp41080pMp4!);
                                        }
                                        else if (card?.Mblog?.PageInfo?.Urls?.Mp4720pMp4 != null)
                                        {
                                            originalVideos.Add(card?.Mblog?.PageInfo?.Urls?.Mp4720pMp4!);
                                        }
                                        else if (card?.Mblog?.PageInfo?.Urls?.Mp4HDMp4 != null)
                                        {
                                            originalVideos.Add(card?.Mblog?.PageInfo?.Urls?.Mp4HDMp4!);
                                        }
                                        else if (card?.Mblog?.PageInfo?.Urls?.Mp4LDMp4 != null)
                                        {
                                            originalVideos.Add(card?.Mblog?.PageInfo?.Urls?.Mp4LDMp4!);
                                        }

                                        //替换非法字符
                                        var invalidChar = Path.GetInvalidFileNameChars();
                                        var newCaption = invalidChar.Aggregate(weiboContent, (o, r) => (o.Replace(r.ToString(), string.Empty)));
                                        var fileName = downloadFolder + "//" + personalFolder + "//" + "//"
                                            + timestamp.ToString("yyyy-MM-dd HH_mm_ss") + newCaption;
                                        Debug.WriteLine(fileName);

                                        int id = 1;
                                        //下载获取图片列表中的图片原图
                                        foreach (var item in originalPics)
                                        {
                                            if (isSkip)
                                                break;

                                            if (string.IsNullOrEmpty(item))
                                                continue;
                                            var fileNamee = fileName + $"_{id}.jpg";
                                            //已存在的文件超过设置值，判定该用户下载过了
                                            if (settings!.CountDownloadedSkipToNextUser > 0 && countDownloadedSkipToNextUser > settings.CountDownloadedSkipToNextUser)
                                            {
                                                AppendLog($"已存在的文件{countDownloadedSkipToNextUser}超过设置值{settings.CountDownloadedSkipToNextUser}，跳到下一个用户", MessageEnum.Info);
                                                isSkip = true;
                                            }

                                            //已经下载过的跳过
                                            if (File.Exists(fileNamee))
                                            {
                                                AppendLog("文件已存在，跳过下载" + Path.GetFullPath(fileNamee), MessageEnum.Warning);
                                                countDownloadedSkipToNextUser++;
                                                await Task.Delay(500);
                                                continue;
                                            }

                                            //传入图片/视频的名字，开始下载图片/视频
                                            try
                                            {
                                                await HttpHelper.GetAsync<AlbumDetailModel>(item, dataSource, cookie!, fileNamee);

                                                //修改文件日期时间为发博的时间
                                                File.SetCreationTime(fileNamee, timestamp);
                                                File.SetLastWriteTime(fileNamee, timestamp);
                                                File.SetLastAccessTime(fileNamee, timestamp);

                                                AppendLog("已完成 " + Path.GetFileName(fileNamee), MessageEnum.Success);
                                            }
                                            catch (Exception ex)
                                            {
                                                AppendLog($"文件下载失败，原始url：{item}，下载路径{fileNamee}", MessageEnum.Error);
                                            }
                                            id++;
                                        }
                                        foreach (var item in originalVideos)
                                        {
                                            if (isSkip)
                                                break;

                                            if (string.IsNullOrEmpty(item))
                                                continue;
                                            var fileNamee = fileName + $"_{id}.mp4";
                                            //已存在的文件超过设置值，判定该用户下载过了
                                            if (settings!.CountDownloadedSkipToNextUser > 0 && countDownloadedSkipToNextUser > settings.CountDownloadedSkipToNextUser)
                                            {
                                                AppendLog($"已存在的文件{countDownloadedSkipToNextUser}超过设置值{settings.CountDownloadedSkipToNextUser}，跳到下一个用户", MessageEnum.Info);
                                                isSkip = true;
                                            }

                                            //已经下载过的跳过
                                            if (File.Exists(fileNamee))
                                            {
                                                AppendLog("文件已存在，跳过下载" + Path.GetFullPath(fileNamee), MessageEnum.Warning);
                                                countDownloadedSkipToNextUser++;
                                                await Task.Delay(500);
                                                continue;
                                            }

                                            //传入图片/视频的名字，开始下载图片/视频
                                            try
                                            {
                                                await HttpHelper.GetAsync<AlbumDetailModel>(item, dataSource, cookie!, fileNamee);

                                                //修改文件日期时间为发博的时间
                                                File.SetCreationTime(fileNamee, timestamp);
                                                File.SetLastWriteTime(fileNamee, timestamp);
                                                File.SetLastAccessTime(fileNamee, timestamp);

                                                AppendLog("已完成 " + Path.GetFileName(fileNamee), MessageEnum.Success);
                                            }
                                            catch (Exception ex)
                                            {
                                                AppendLog($"文件下载失败，原始url：{item}，下载路径{fileNamee}", MessageEnum.Error);
                                            }
                                            id++;
                                        }
                                        foreach (var item in originalLivePhotos)
                                        {
                                            if (isSkip)
                                                break;

                                            if (string.IsNullOrEmpty(item))
                                                continue;
                                            var fileNamee = fileName + $"_{id}.mov";
                                            //已存在的文件超过设置值，判定该用户下载过了
                                            if (settings!.CountDownloadedSkipToNextUser > 0 && countDownloadedSkipToNextUser > settings.CountDownloadedSkipToNextUser)
                                            {
                                                AppendLog($"已存在的文件{countDownloadedSkipToNextUser}超过设置值{settings.CountDownloadedSkipToNextUser}，跳到下一个用户", MessageEnum.Info);
                                                isSkip = true;
                                            }

                                            //已经下载过的跳过
                                            if (File.Exists(fileNamee))
                                            {
                                                AppendLog("文件已存在，跳过下载" + Path.GetFullPath(fileNamee), MessageEnum.Warning);
                                                countDownloadedSkipToNextUser++;
                                                await Task.Delay(500);
                                                continue;
                                            }

                                            //传入图片/视频的名字，开始下载图片/视频
                                            try
                                            {
                                                await HttpHelper.GetAsync<AlbumDetailModel>(item, dataSource, cookie!, fileNamee);

                                                //修改文件日期时间为发博的时间
                                                File.SetCreationTime(fileNamee, timestamp);
                                                File.SetLastWriteTime(fileNamee, timestamp);
                                                File.SetLastAccessTime(fileNamee, timestamp);

                                                AppendLog("已完成 " + Path.GetFileName(fileNamee), MessageEnum.Success);
                                            }
                                            catch (Exception ex)
                                            {
                                                AppendLog($"文件下载失败，原始url：{item}，下载路径{fileNamee}", MessageEnum.Error);
                                            }
                                            id++;
                                        }
                                    }

                                    page++;
                                }
                                else
                                {
                                    break;
                                }

                                //通过加入随机等待避免被限制。爬虫速度过快容易被系统限制(一段时间后限制会自动解除)，加入随机等待模拟人的操作，可降低被系统限制的风险。默认是每爬取1到5页随机等待6到10秒，如果仍然被限，可适当增加sleep时间
                                Random rd = new Random();
                                int rnd = rd.Next(5000, 10000);
                                AppendLog($"随机等待{rnd}ms，避免爬虫速度过快被系统限制", MessageEnum.Info);
                                await Task.Delay(rnd);
                            }
                        }


                        //单个用户结束下载
                        if (!string.IsNullOrEmpty(nickName))
                        {
                            string info = $"{nickName} <a href=\"//weibo.com/u/{userId}\">{userId}{nickName}</a>于{DateTime.Now.ToString("HH:mm:ss")}结束下载，程序版本V{currentVersion}<img src=\"{headUrl}\">";
                            await PushPlusHelper.SendMessage(settings?.PushPlusToken!, "微博相册下载", info);
                            SentrySdk.CaptureMessage(info);

                            AddToUserIdList(userId, nickName);

                            AppendLog(info, MessageEnum.Info);
                        }
                        else
                        {
                            AppendLog("遇到错误，请重试", MessageEnum.Error);
                        }
                    }
                });

                //所有用户结束下载
                AppendLog("结束下载。", MessageEnum.Info);
            }
            catch (Exception ex)
            {
                string msg = $"遇到错误，uid: {GlobalVar.gId}，DataSource: {dataSource}，Page:{GlobalVar.gPage}，SinceId: {GlobalVar.gSinceId}，" + ex.ToString() + "，请稍后重试。";
                AppendLog(msg, MessageEnum.Error);
                SentrySdk.CaptureMessage(msg, SentryLevel.Error);
            }
            finally
            {
                isDownloading = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    tbDownload.Text = "开始下载";
                });
            }
        }

        private void AppendLog(string text, MessageEnum messageEnum = MessageEnum.Info)
        {
            string msg = $"{DateTime.Now} {text}";
            Debug.WriteLine(msg);

            ListView_Messages?.Dispatcher.InvokeAsync(() =>
            {
                Messages.Insert(0, new MessageModel()
                {
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    Message = text,
                    MessageType = messageEnum
                });
            });
        }

        private void AddToUserIdList(string userId, string nickName)
        {
            //不在列表的，才写入文件
            if (!uids.Contains(userId))
            {
                uids.Add(userId);
                File.AppendAllText("uidList.txt", Environment.NewLine + $"{userId},{nickName}");
            }
        }

        private void InitUidsData()
        {
            //配置文件不存在就创建
            if (!File.Exists("uidList.txt"))
            {
                File.WriteAllText("uidList.txt", "//可以是多用户，换行隔开。\r\n//行内用英文逗号隔开，用户id(必填),用户名(可选)\r\n");
            }

            uids.Clear();
            //文件中可以是多用户，换行隔开。行内用英文逗号隔开，用户id(必填),用户名(可选)
            var lines = File.ReadAllLines("uidList.txt");
            foreach (var line in lines)
            {
                if (line.StartsWith("//"))
                    continue;

                var temp = line.Split(',');
                uids.Add(temp[0]);
            }

        }

        private void InitSettingsData()
        {
            if (!File.Exists("Settings.json"))
            {
                File.WriteAllText("Settings.json", JsonConvert.SerializeObject(new SettingsModel(), Formatting.Indented));
            }

            string settingsContent = File.ReadAllText("Settings.json");
            settings = JsonConvert.DeserializeObject<SettingsModel>(settingsContent);
            if (settings == null)
            {
                AppendLog("Settings.json文件缺失，请重新下载程序", MessageEnum.Error);
                return;
            }
            if (string.IsNullOrEmpty(settings.PushPlusToken))
            {
                AppendLog("没有检测到PushPlus token，程序将不会推送消息到微信。如需推送，请登录https://www.pushplus.plus/设置", MessageEnum.Info);
            }
            if (dataSource == WeiboDataSource.WeiboCn || dataSource == WeiboDataSource.WeiboCnMobile)
            {
                cookie = settings.WeiboCnCookie;
            }
            else
            {
                cookie = settings.WeiboComCookie;
            }
            if (string.IsNullOrEmpty(cookie))
            {
                AppendLog("没有检测到cookie，程序将无法抓取数据", MessageEnum.Error);
                return;
            }
            if (!settings.EnableCrontab)
            {
                AppendLog("没有开启Crontab定时任务，程序将不会自动执行", MessageEnum.Info);
            }
            if (settings.EnableCrontab && !string.IsNullOrEmpty(settings.Crontab))
            {
                AppendLog($"已开启Crontab定时任务，{CronExpressionDescriptor.ExpressionDescriptor.GetDescription(settings.Crontab)}", MessageEnum.Info);
            }
        }

        #region UI操作
        private async void StartDownLoad(object sender, RoutedEventArgs e)
        {
            if (tbDownload.Text == "开始下载")
            {
                if (isDownloading)
                {
                    AppendLog("正在执行下载任务");
                    return;
                }

                isDownloading = true;
                tbDownload.Text = "停止下载";

                string pattern = @"\d+";  // 匹配一个或多个数字
                Match match = Regex.Match(TextBox_WeiboId.Text.Trim(), pattern);

                if (match.Success)
                {
                    await Start(match.Value);
                }
                else
                {
                    AppendLog("没有找到微博账号", MessageEnum.Warning);
                    isDownloading = false;
                    tbDownload.Text = "开始下载";
                }
            }
            else
            {
                isDownloading = false;
                tbDownload.Text = "开始下载";
                cancellationTokenSource?.Cancel();
            }
        }

        private void StopDownLoad(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        private async void BatchDownLoad(object sender, RoutedEventArgs e)
        {
            if (isDownloading)
            {
                AppendLog("正在执行下载任务");
                return;
            }

            isDownloading = true;
            AppendLog(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "开启批量任务");

            foreach (var item in uids)
            {
                await Start(item.Trim());
                AppendLog("等待60s，下载下一个用户相册数据");
                await Task.Delay(60 * 1000);
            }

            AppendLog(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "结束批量任务");
            isDownloading = false;
        }

        private void ListView_CopyLog(object sender, RoutedEventArgs e)
        {
            if (ListView_Messages.SelectedItem != null)
                Clipboard.SetText((ListView_Messages.SelectedItem! as MessageModel)!.Message);
        }

        private void ListView_ClearLog(object sender, RoutedEventArgs e)
        {
            Messages.Clear();
        }

        private void ListView_ExportLog(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = $"{GlobalVar.gId}-log";
            dialog.DefaultExt = ".txt";
            dialog.Filter = "下载日志 (.txt)|*.txt";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var message in Messages)
                {
                    string line = $"{message.Time},{message.Message},{message.MessageType}";
                    sb.AppendLine(line);
                }

                File.WriteAllText(dialog.FileName, sb.ToString());
            }
        }

        private void ListView_OpenDownloadFolder(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", Path.GetFullPath(downloadFolder));
        }

        private async void TextBox_WeiboId_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (isDownloading)
                    return;

                isDownloading = true;
                tbDownload.Text = "停止下载";
                await Start(TextBox_WeiboId.Text.Trim());
            }
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            var set = new SettingsWindow();
            set.Owner = this;
            set.ShowDialog();
            AppendLog("设置已更新");
        }

        private void MicaWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Border_Head.Width = Border_Head.Height = Column_LeftGrid.ActualWidth * 0.8;
            Border_Head.CornerRadius = new CornerRadius(Column_LeftGrid.ActualWidth * 0.8);
        }
        #endregion

    }
}
