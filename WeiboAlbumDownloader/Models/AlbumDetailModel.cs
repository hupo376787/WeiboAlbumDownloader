using System.Collections.Generic;

namespace WeiboAlbumDownloader.Models
{
    public class AlbumDetailModel
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
        public PhotoListData data { get; set; }
    }

    public class PhotoListCount
    {
        /// <summary>
        /// 评论数
        /// </summary>
        public int comments { get; set; }
        /// <summary>
        /// 查数
        /// </summary>
        public int clicks { get; set; }
        /// <summary>
        /// 转发数
        /// </summary>
        public int retweets { get; set; }
        /// <summary>
        /// 点赞数
        /// </summary>
        public int likes { get; set; }
    }

    public class PhotoListItem
    {
        /// <summary>
        /// 相册ID
        /// </summary>
        public string? album_id { get; set; }
        /// <summary>
        /// 推文内容
        /// </summary>
        public string caption { get; set; }
        /// <summary>
        /// 推文内容
        /// </summary>
        public string caption_render { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PhotoListCount count { get; set; }
        /// <summary>
        /// 创建日期
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int @type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string @from { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string is_favorited { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string is_liked { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string oid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string photo_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string pic_host { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string pic_name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string pic_pid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int pic_type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string source { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string tags { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int timestamp { get; set; }
        /// <summary>
        /// 用户id
        /// </summary>
        public long uid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string updated_at { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int property { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int? visible_type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string mid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string is_private { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string feed_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double? latitude { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double? longitude { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string is_paid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int mblog_vip_type { get; set; }
    }

    public class PhotoListData
    {
        /// <summary>
        /// 相册ID
        /// </summary>
        public string? album_id { get; set; }
        /// <summary>
        /// 照片总数
        /// </summary>
        public int total { get; set; }
        /// <summary>
        /// 照片列表，分页
        /// </summary>
        public List<PhotoListItem>? photo_list { get; set; }
    }
}
