using HtmlAgilityPack;
using MicaWPF.Controls;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
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
    
        double currentVersion = 2.0;
        
        /// <summary>
        /// com1是根据uid获取相册id，https://photo.weibo.com/albums/get_all?uid=10000000000&page=1；根据uid和相册id以及相册type获取图片列表，https://photo.weibo.com/photos/get_all?uid=10000000000&album_id=3959362334782071&page=1&type=3
        /// com2是根据uid获取相册id，https://weibo.com/ajax/profile/getImageWall?uid=10000000000&sinceid=0&has_album=true；根据相册id和sinceid获取图片列表，https://weibo.com/ajax/profile/getAlbumDetail?containerid=123456789000123456_-_pc_profile_album_-_photo_-_camera_-_0_-_%25E5%258E%259F%25E5%2588%259B&since_id=0
        /// cn是从 https://weibo.cn/10000000000/profile?page=2获取html解析发布的微博
        /// </summary>
        private WeiboDataSource dataSource = WeiboDataSource.WeiboCn;
        private int countPerPage = 100;
        private string downloadFolder = Environment.CurrentDirectory + "//DownLoad//";
        private SettingsModel? settings;
        private string? cookie;
        private List<string> uids = new List<string>();
        //用来跳过到下一个uid的计数。如果当前uid下载的时候已存在文件超过此计数，则判定下载过了。
        private int countDownloadedSkipToNextUser;
        private CancellationTokenSource? cancellationTokenSource;
        public ObservableCollection<MessageModel> Messages { get; set; } = new ObservableCollection<MessageModel>();

        public MainWindow()
        {
            InitializeComponent();

            _ = GetVersion();
            AppendLog("数据源选择weibo.com的时候，程序是从微博相册采集数据，包括微博配图和头像相册等，但是没有视频；数据源选择weibo.cn的时候，程序是从微博时间流中采集数据，包括微博配图以及发布的视频。", MessageEnum.Info);

            InitData();

            Directory.CreateDirectory(downloadFolder);
            ComboBox_DataSource.ItemsSource = Enum.GetNames(typeof(WeiboDataSource));
            ComboBox_DataSource.SelectedIndex = 0;
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
                        Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "开启了定时任务");
                        await Start();
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        private async void StartDownLoad(object sender, RoutedEventArgs e)
        {
            await Start();
        }

        private void StopDownLoad(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        private async Task GetVersion()
        {
            AppendLog($"当前程序版本V{currentVersion}");

            var latestVersionString = await GithubHelper.GetLatestVersion();
            if (string.IsNullOrEmpty(latestVersionString))
            {
                AppendLog("从https://github.com/hupo376787/WeiboAlbumDownloader/releases获取最新版失败，请检查网络或稍后再试", MessageEnum.Warning);
            }
            else
            {
                var res = double.TryParse(latestVersionString, out double latestVersion);
                //获取成功
                if (res)
                {
                    if (latestVersion > currentVersion)
                        AppendLog($"Github最新版为V{latestVersion}。请从https://github.com/hupo376787/WeiboAlbumDownloader/releases获取最新版", MessageEnum.Success);
                }
                else
                {
                    AppendLog("从https://github.com/hupo376787/WeiboAlbumDownloader/releases获取最新版失败，请检查网络或稍后再试", MessageEnum.Warning);
                }
            }
        }

        private async Task Start()
        {
            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = new CancellationTokenSource();

                InitData();

                await Task.Run(async () =>
                {
                    bool isSkip = false;
                    foreach (var userId in uids)
                    {
                        if (string.IsNullOrEmpty(userId))
                        {
                            continue;
                        }
                        else
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

                                var albums = await HttpHelper.GetAsync<UserAlbumModel>(albumsUrl, dataSource, cookie);
                                Directory.CreateDirectory(downloadFolder + userId);
                                if (albums != null)
                                {
                                    foreach (var item in albums?.data?.album_list)
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

                                            string albumUrl = $"https://photo.weibo.com/photos/get_all?uid={userId}&album_id={item.album_id}&count={countPerPage}&page={page}&type={item.type}";
                                            var photos = await HttpHelper.GetAsync<AlbumDetailModel>(albumUrl, dataSource, cookie);
                                            if (photos != null && photos.data?.photo_list != null && photos.data?.photo_list.Count > 0)
                                            {
                                                int photoCount = 1;
                                                string oldCaption = "";
                                                foreach (var photo in photos.data?.photo_list)
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

                                                    var invalidChar = System.IO.Path.GetInvalidFileNameChars();
                                                    var newCaption = invalidChar.Aggregate(photo.caption_render, (o, r) => (o.Replace(r.ToString(), string.Empty)));
                                                    var fileName = downloadFolder + userId + "//" + item.caption + "//"
                                                        + timestamp.ToString("yyyy-MM-dd HH_mm_ss") + "-" + photoCount + newCaption + ".jpg";
                                                    Debug.WriteLine(fileName);
                                                    if (File.Exists(fileName))
                                                    {
                                                        AppendLog("文件已存在，跳过下载" + fileName, MessageEnum.Warning);
                                                        countDownloadedSkipToNextUser++;
                                                        await Task.Delay(500);
                                                        continue;
                                                    }

                                                    //传入图片/视频的名字，开始下载图片/视频
                                                    try
                                                    {
                                                        await HttpHelper.GetAsync<AlbumDetailModel>(photoUrl, dataSource, cookie, fileName);

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
                                            if (settings.CountDownloadedSkipToNextUser > 0 && countDownloadedSkipToNextUser > settings.CountDownloadedSkipToNextUser)
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

                                var albums = await HttpHelper.GetAsync<UserAlbumModel2>(albumsUrl, dataSource, cookie);
                                Directory.CreateDirectory(downloadFolder + userId);
                                if (albums != null)
                                {
                                    foreach (var item in albums?.Data?.AlbumList)
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
                                            var photos = await HttpHelper.GetAsync<AlbumDetailModel2>(albumUrl, dataSource, cookie);
                                            if (photos != null && photos.PhotoListData2?.PhotoListItem2 != null && photos.PhotoListData2?.PhotoListItem2.Count > 0)
                                            {
                                                sinceId = photos.PhotoListData2.SinceId;
                                                foreach (var photo in photos.PhotoListData2?.PhotoListItem2)
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
                                                        AppendLog("文件已存在，跳过下载" + fileName, MessageEnum.Warning);
                                                        countDownloadedSkipToNextUser++;
                                                        await Task.Delay(500);
                                                        continue;
                                                    }

                                                    //传入图片/视频的名字，开始下载图片/视频
                                                    try
                                                    {
                                                        await HttpHelper.GetAsync<AlbumDetailModel>(photoUrl, dataSource, cookie, fileName);

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
                                            if (settings.CountDownloadedSkipToNextUser > 0 && countDownloadedSkipToNextUser > settings.CountDownloadedSkipToNextUser)
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

                                    //filter，0-全部；1-原创；2-图片
                                    //https://weibo.cn/xxxxxxxxxxxxx?page=2
                                    //https://weibo.cn/xxxxxxxxxxxxx/profile?page=2
                                    string url = $"https://weibo.cn/{userId}?page={page}&filter=1";
                                    string text = await HttpHelper.GetAsync<string>(url, dataSource, cookie);
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
                                        string weiboDesc = string.Join(" ", desc);

                                        headUrl = "https://tvax2.sinaimg.cn/large/" + Path.GetFileName(temp).Split("?")[0];
                                        var fileName = downloadFolder + userId + "//" + Path.GetFileName(headUrl);
                                        //下载头像
                                        if (!File.Exists(fileName))
                                        {
                                            await HttpHelper.GetAsync<AlbumDetailModel>(headUrl, dataSource, cookie, fileName);
                                        }

                                        Image_Head?.Dispatcher.InvokeAsync(() =>
                                        {
                                            var bytes = File.ReadAllBytes(fileName);
                                            MemoryStream ms = new MemoryStream(bytes);
                                            BitmapImage bi = new BitmapImage();
                                            bi.BeginInit();
                                            bi.StreamSource = ms;
                                            bi.EndInit();

                                            Image_Head.ImageSource = bi;

                                            //Image_Head.ImageSource = new BitmapImage(new Uri(fileName));
                                            TextBlock_UID.Text = userId;
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
                                            text = await HttpHelper.GetAsync<string>(photoListUrl, dataSource, cookie);
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
                                                var res = await HttpHelper.GetAsync<VideoDetailModel>(videoUrl, dataSource, cookie);
                                                if (res != null && res.ok == 1)
                                                {
                                                    originalPics.Add(res?.data?.@object?.stream?.hd_url);
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
                                                + timestamp.ToString("yyyy-MM-dd HH_mm_ss") + "-" + photoCount + newCaption;
                                            //后缀名区分图片/视频
                                            if (picType == PicEnum.Video)
                                                fileName += ".mp4";
                                            else
                                                fileName += ".jpg";
                                            Debug.WriteLine(fileName);

                                            //已存在的文件超过设置值，判定该用户下载过了
                                            if (settings.CountDownloadedSkipToNextUser > 0 && countDownloadedSkipToNextUser > settings.CountDownloadedSkipToNextUser)
                                            {
                                                AppendLog($"已存在的文件{countDownloadedSkipToNextUser}超过设置值{settings.CountDownloadedSkipToNextUser}，跳到下一个用户", MessageEnum.Info);
                                                isSkip = true;
                                            }

                                            //已经下载过的跳过
                                            if (File.Exists(fileName))
                                            {
                                                AppendLog("文件已存在，跳过下载" + fileName, MessageEnum.Warning);
                                                countDownloadedSkipToNextUser++;
                                                await Task.Delay(500);
                                                continue;
                                            }

                                            //传入图片/视频的名字，开始下载图片/视频
                                            try
                                            {
                                                await HttpHelper.GetAsync<AlbumDetailModel>(item, dataSource, cookie, fileName);

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

                            //单个用户结束下载
                            string info = $"<a href=\"//weibo.com/u/{userId}\">{userId}{nickName}</a>于{DateTime.Now.ToString("HH:mm:ss")}结束下载，程序版本V{currentVersion}<img src=\"{headUrl}\">";
                            await PushPlusHelper.SendMessage(settings?.PushPlusToken, "微博相册下载", info);
                            AppendLog(info, MessageEnum.Info);
                        }
                    }
                });

                //所有用户结束下载
                AppendLog("结束下载。", MessageEnum.Info);
            }
            catch (Exception ex)
            {
                AppendLog("遇到错误" + ex.ToString() + "，请稍后重试。", MessageEnum.Error);
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

        //private void ToggleSwitch_Source_Click(object sender, RoutedEventArgs e)
        //{
        //    ToggleSwitch_Source.Content = ToggleSwitch_Source.Content.ToString() == "weibo.com" ? "weibo.cn" : "weibo.com";
        //    isFromWeiboCom = ToggleSwitch_Source.Content.ToString() == "weibo.com";
        //}

        private void InitData()
        {
            if (!File.Exists("uidList.txt"))
            {
                using (File.Create("uidList.txt")) { }
            }
            if (!File.Exists("Settings.json"))
            {
                File.WriteAllText("Settings.json", JsonConvert.SerializeObject(new SettingsModel(), Formatting.Indented));
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //Read users
                if (string.IsNullOrEmpty(TextBox_WeiboId.Text))
                {
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
                else
                {
                    uids = new List<string>() { TextBox_WeiboId.Text };
                }
            });

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
            if (dataSource == WeiboDataSource.WeiboCn)
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

        private void OpenDir(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", Path.GetFullPath(downloadFolder));
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start("explorer.exe", e.Uri.AbsoluteUri);
        }

        private void ListView_CopyLog(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText((ListView_Messages.SelectedItem as MessageModel)!.Message);
        }

        private void ComboBox_DataSource_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            dataSource = (WeiboDataSource)ComboBox_DataSource.SelectedIndex;
            if (ComboBox_DataSource.SelectedIndex == 0)
            {
                AppendLog("通过weibo.cn获取的是用户的时间流，不过只能获取原创微博相册。", MessageEnum.Info);
            }
            else if (ComboBox_DataSource.SelectedIndex == 1)
            {
                AppendLog("通过weibo.com获取的是相册信息，可以获取原创微博相册、头像相册、自拍相册等。少数用户存在获取失败的问题，怀疑是微博内部api不统一造成的。", MessageEnum.Info);
            }
            else if (ComboBox_DataSource.SelectedIndex == 2)
            {
                AppendLog("通过weibo.com获取的是用户的ajax相册，可以获取原创微博相册、头像相册、自拍相册等。但是获取不到博文信息，所以无法重命名图片和修改图片日期。貌似还获取不全。不推荐使用！！！", MessageEnum.Info);
            }
        }

        private void MicaWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Border_Head.Width = Border_Head.Height = Column_LeftGrid.ActualWidth * 0.8;
            Border_Head.CornerRadius = new CornerRadius(Column_LeftGrid.ActualWidth * 0.8);
        }

        private void GetCookie(object sender, RoutedEventArgs e)
        {
            string loginUrl = "https://passport.weibo.com/sso/signin?entry=wapsso&source=wapssowb&url=https://weibo.cn";
            if (ComboBox_DataSource.SelectedIndex != 0)
            {
                loginUrl = "https://passport.weibo.com/sso/signin?entry=miniblog&source=miniblog&url=https://weibo.com/";
            }

            IWebDriver driver = new ChromeDriver();
            driver.Url = loginUrl;
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60))
            {
                PollingInterval = TimeSpan.FromMilliseconds(500),
            };
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
            //wait.Until(d => d.FindElement(By.LinkText("title")));

            // 等待页面加载完成并获取页面标题
            wait.Until(d => d.Title.Equals("微博 – 随时随地发现新鲜事") || d.Title.Equals("我的首页"));

            // 获取页面标题并进行检查
            string pageTitle = driver.Title;
            if (pageTitle.Equals("微博 – 随时随地发现新鲜事") || pageTitle.Equals("我的首页"))
            {
                AppendLog("扫码登陆成功", MessageEnum.Success);
                // 获取所有的 Cookie 对象
                IReadOnlyCollection<OpenQA.Selenium.Cookie> cookies = driver.Manage().Cookies.AllCookies;

                // 将 Cookie 对象转换为一个字符串，格式类似于 HTTP 请求头的 Cookie 字符串
                string cookie = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));

                // 打印 Cookie 字符串
                Debug.WriteLine(cookie);

                string settingsContent = File.ReadAllText("Settings.json");
                settings = JsonConvert.DeserializeObject<SettingsModel>(settingsContent);
                if (settings == null)
                {
                    AppendLog("Settings.json文件缺失，请重新下载程序", MessageEnum.Error);
                    return;
                }
                if (dataSource == WeiboDataSource.WeiboCn)
                {
                    settings.WeiboCnCookie = cookie;
                }
                else
                {
                    settings.WeiboComCookie = cookie;
                }
                File.WriteAllText("Settings.json", JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            else
            {
                Debug.WriteLine("未登录");
            }

            // 程序结束时，手动关闭浏览器
            driver.Quit();
        }
    }
}
