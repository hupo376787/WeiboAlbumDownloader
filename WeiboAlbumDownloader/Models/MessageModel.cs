using System;
using WeiboAlbumDownloader.Enums;

namespace WeiboAlbumDownloader.Models
{
    public class MessageModel
    {
        public string? Message { get; set; }
        public string? Time { get; set; }
        public MessageEnum MessageType { get; set; }
    }
}
