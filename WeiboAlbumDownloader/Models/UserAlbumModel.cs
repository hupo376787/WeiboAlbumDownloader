using System.Collections.Generic;

namespace WeiboAlbumDownloader.Models
{
    internal class UserAlbumModel
    {
        /// <summary>
        /// 
        /// </summary>
        public string result { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string msg { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int timestamp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AlbumData data { get; set; }
    }
    public class AlbumCount
    {
        /// <summary>
        /// 
        /// </summary>
        public int photos { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int likes { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int comments { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int retweets { get; set; }
    }

    public class AlbumListItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string album_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string uid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string property { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string source { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string album_order { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string usort { get; set; }
        /// <summary>
        /// 头像相册
        /// </summary>
        public string caption { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string description { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string cover_pic { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string cover_photo_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string question { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string answer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string updated_at { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string timestamp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int updated_at_int { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string is_favorited { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string is_private { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string thumb120_pic { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string thumb300_pic { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string sq612_pic { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AlbumCount count { get; set; }
    }

    public class AlbumData
    {
        /// <summary>
        /// 
        /// </summary>
        public int total { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<AlbumListItem> album_list { get; set; }
    }

}
