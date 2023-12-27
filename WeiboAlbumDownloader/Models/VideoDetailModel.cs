namespace WeiboAlbumDownloader.Models
{
    public class VideoDetailModel
    {
        /// <summary>
        /// 
        /// </summary>
        public int ok { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public VideoData data { get; set; }
    }

    public class VideoData
    {
        /// <summary>
        /// 
        /// </summary>
        public string object_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string object_type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public @object @object { get; set; }
    }

    public class @object
    {
        /// <summary>
        /// 
        /// </summary>
        public string summary { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Author author { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public @Stream stream { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Image image { get; set; }
    }

    public class Author
    {
        /// <summary>
        /// 
        /// </summary>
        public long id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string screen_name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string profile_image_url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string profile_url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int statuses_count { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string verified { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int verified_type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string close_blue_v { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string description { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string gender { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int mbtype { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int svip { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int urank { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int mbrank { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string follow_me { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string following { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int follow_count { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string followers_count { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string followers_count_str { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string cover_image_phone { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string avatar_hd { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string like { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string like_me { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string special_follow { get; set; }
    }

    public class @Stream
    {
        /// <summary>
        /// 
        /// </summary>
        public double duration { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string format { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int width { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string hd_url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int height { get; set; }
    }

    public class Image
    {
        /// <summary>
        /// 
        /// </summary>
        public int width { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string pid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int source { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int is_self_cover { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int height { get; set; }
    }
}
