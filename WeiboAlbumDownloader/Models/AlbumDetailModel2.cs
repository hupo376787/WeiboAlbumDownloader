using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeiboAlbumDownloader.Models
{
    public partial class AlbumDetailModel2
    {
        [JsonProperty("data")]
        public PhotoListData2 PhotoListData2 { get; set; }

        [JsonProperty("ok")]
        public long Ok { get; set; }
    }

    public partial class PhotoListData2
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("list")]
        public List<PhotoListItem2> PhotoListItem2 { get; set; }

        [JsonProperty("since_id")]
        public long SinceId { get; set; }
    }

    public partial class PhotoListItem2
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
