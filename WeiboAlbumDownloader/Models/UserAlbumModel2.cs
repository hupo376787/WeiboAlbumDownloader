using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WeiboAlbumDownloader.Models
{
    public partial class UserAlbumModel2
    {
        [JsonProperty("data")]
        public AlbumData2 Data { get; set; }

        [JsonProperty("bottom_tips_visible")]
        public bool BottomTipsVisible { get; set; }

        [JsonProperty("bottom_tips_text")]
        public string BottomTipsText { get; set; }

        [JsonProperty("ok")]
        public long Ok { get; set; }
    }

    public partial class AlbumData2
    {
        [JsonProperty("album_list")]
        public List<AlbumList2> AlbumList { get; set; }

        [JsonProperty("album_since_id")]
        public long AlbumSinceId { get; set; }

        [JsonProperty("since_id")]
        public string SinceId { get; set; }

        [JsonProperty("list")]
        public List<List2> List { get; set; }
    }

    public partial class AlbumList2
    {
        [JsonProperty("pic_title")]
        public string PicTitle { get; set; }

        [JsonProperty("containerid")]
        public string Containerid { get; set; }

        [JsonProperty("pic")]
        public string Pic { get; set; }
    }

    public partial class List2
    {
        [JsonProperty("pid")]
        public string Pid { get; set; }

        [JsonProperty("mid")]
        public string Mid { get; set; }

        [JsonProperty("is_paid")]
        public bool IsPaid { get; set; }

        [JsonProperty("timeline_month")]
        public string TimelineMonth { get; set; }

        [JsonProperty("timeline_year")]
        public string TimelineYear { get; set; }

        [JsonProperty("object_id")]
        public string ObjectId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
