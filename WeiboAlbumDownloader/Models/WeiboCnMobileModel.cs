using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WeiboAlbumDownloader.Models
{
    public partial class WeiboCnMobileModel
    {
        [JsonProperty("ok")]
        public long? Ok { get; set; }

        [JsonProperty("data")]
        public Data? Data { get; set; }
    }

    public partial class Data
    {
        [JsonProperty("cardlistInfo")]
        public CardlistInfo? CardlistInfo { get; set; }

        [JsonProperty("cards")]
        public List<Card>? Cards { get; set; }

        [JsonProperty("scheme")]
        public string? Scheme { get; set; }

        [JsonProperty("showAppTips")]
        public long? ShowAppTips { get; set; }
    }

    public partial class CardlistInfo
    {
        [JsonProperty("containerid")]
        public string? Containerid { get; set; }

        [JsonProperty("v_p")]
        public long? VP { get; set; }

        [JsonProperty("show_style")]
        public long? ShowStyle { get; set; }

        [JsonProperty("total")]
        public long? Total { get; set; }

        [JsonProperty("autoLoadMoreIndex")]
        public long? AutoLoadMoreIndex { get; set; }

        [JsonProperty("since_id")]
        public long? SinceId { get; set; }
    }

    public partial class Card
    {
        [JsonProperty("card_type")]
        public long? CardType { get; set; }

        [JsonProperty("profile_type_id")]
        public string? ProfileTypeId { get; set; }

        [JsonProperty("itemid")]
        public string? Itemid { get; set; }

        [JsonProperty("scheme")]
        public Uri? Scheme { get; set; }

        [JsonProperty("mblog")]
        public Mblog? Mblog { get; set; }
    }

    public partial class Mblog
    {
        [JsonProperty("visible")]
        public Visible? Visible { get; set; }

        [JsonProperty("created_at")]
        public string? CreatedAt { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("mid")]
        public string? Mid { get; set; }

        [JsonProperty("can_edit")]
        public bool? CanEdit { get; set; }

        [JsonProperty("text")]
        public string? Text { get; set; }

        [JsonProperty("textLength")]
        public long? TextLength { get; set; }

        [JsonProperty("source")]
        public string? Source { get; set; }

        [JsonProperty("favorited")]
        public bool? Favorited { get; set; }

        [JsonProperty("pic_ids")]
        public List<string>? PicIds { get; set; }

        [JsonProperty("thumbnail_pic")]
        public string? ThumbnailPic { get; set; }

        [JsonProperty("bmiddle_pic")]
        public string? BmiddlePic { get; set; }

        [JsonProperty("original_pic")]
        public string? OriginalPic { get; set; }

        [JsonProperty("is_paid")]
        public bool? IsPaid { get; set; }

        [JsonProperty("mblog_vip_type")]
        public long? MblogVipType { get; set; }

        [JsonProperty("user")]
        public User? User { get; set; }

        [JsonProperty("retweeted_status")]
        public RetweetedStatus? RetweetedStatus { get; set; }

        [JsonProperty("reposts_count")]
        public long? RepostsCount { get; set; }

        [JsonProperty("comments_count")]
        public long? CommentsCount { get; set; }

        [JsonProperty("reprint_cmt_count")]
        public long? ReprintCmtCount { get; set; }

        [JsonProperty("attitudes_count")]
        public long? AttitudesCount { get; set; }

        [JsonProperty("mixed_count")]
        public long? MixedCount { get; set; }

        [JsonProperty("pending_approval_count")]
        public long? PendingApprovalCount { get; set; }

        [JsonProperty("isLongText")]
        public bool? IsLongText { get; set; }

        [JsonProperty("show_mlevel")]
        public long? ShowMlevel { get; set; }

        [JsonProperty("darwin_tags")]
        public List<DarwinTag>? DarwinTags { get; set; }

        [JsonProperty("ad_marked")]
        public bool? AdMarked { get; set; }

        [JsonProperty("mblogtype")]
        public long? Mblogtype { get; set; }

        [JsonProperty("item_category")]
        public string? ItemCategory { get; set; }

        [JsonProperty("rid")]
        public string? Rid { get; set; }

        [JsonProperty("extern_safe")]
        public long? ExternSafe { get; set; }

        [JsonProperty("number_display_strategy")]
        public NumberDisplayStrategy? NumberDisplayStrategy { get; set; }

        [JsonProperty("content_auth")]
        public long? ContentAuth { get; set; }

        [JsonProperty("is_show_mixed")]
        public bool? IsShowMixed { get; set; }

        [JsonProperty("comment_manage_info")]
        public CommentManageInfo? CommentManageInfo { get; set; }

        [JsonProperty("pic_num")]
        public long? PicNum { get; set; }

        [JsonProperty("mlevel")]
        public long? Mlevel { get; set; }

        [JsonProperty("region_name")]
        public string? RegionName { get; set; }

        [JsonProperty("region_opt")]
        public long? RegionOpt { get; set; }

        [JsonProperty("analysis_extra")]
        public string? AnalysisExtra { get; set; }

        [JsonProperty("mblog_menu_new_style")]
        public long? MblogMenuNewStyle { get; set; }

        [JsonProperty("edit_config")]
        public EditConfig? EditConfig { get; set; }

        [JsonProperty("page_info")]
        public PageInfo? PageInfo { get; set; }

        [JsonProperty("pics")]
        //public List<Pic>? Pics { get; set; }
        public object? Pics { get; set; }

        [JsonProperty("live_photo")]
        public List<string>? LivePhoto { get; set; }

        [JsonProperty("bid")]
        public string? Bid { get; set; }

        [JsonProperty("safe_tags")]
        public long? SafeTags { get; set; }
    }

    public partial class RetweetedStatus
    {
    }

    public partial class CommentManageInfo
    {
        [JsonProperty("comment_permission_type")]
        public long? CommentPermissionType { get; set; }

        [JsonProperty("approval_comment_type")]
        public long? ApprovalCommentType { get; set; }

        [JsonProperty("comment_sort_type")]
        public long? CommentSortType { get; set; }
    }

    public partial class DarwinTag
    {
        [JsonProperty("object_type")]
        public string? ObjectType { get; set; }

        [JsonProperty("object_id")]
        public string? ObjectId { get; set; }

        [JsonProperty("display_name")]
        public string? DisplayName { get; set; }

        [JsonProperty("enterprise_uid")]
        public string? EnterpriseUid { get; set; }

        [JsonProperty("bd_object_type")]
        public string? BdObjectType { get; set; }
    }

    public partial class EditConfig
    {
        [JsonProperty("edited")]
        public bool? Edited { get; set; }
    }

    public partial class NumberDisplayStrategy
    {
        [JsonProperty("apply_scenario_flag")]
        public long? ApplyScenarioFlag { get; set; }

        [JsonProperty("display_text_min_number")]
        public long? DisplayTextMinNumber { get; set; }

        [JsonProperty("display_text")]
        public string? DisplayText { get; set; }
    }

    public partial class PageInfo
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("icon")]
        public string? Icon { get; set; }

        [JsonProperty("page_pic")]
        public PagePic? PagePic { get; set; }

        [JsonProperty("page_url")]
        public string? PageUrl { get; set; }

        [JsonProperty("page_title")]
        public string? PageTitle { get; set; }

        [JsonProperty("content1")]
        public string? Content1 { get; set; }

        [JsonProperty("content2")]
        public string? Content2 { get; set; }

        [JsonProperty("video_orientation")]
        public string? VideoOrientation { get; set; }

        [JsonProperty("play_count")]
        public string? PlayCount { get; set; }

        [JsonProperty("media_info")]
        public Mediainfo? Mediainfo { get; set; }

        [JsonProperty("urls")]
        public Urls? Urls { get; set; }
    }

    public partial class Mediainfo
    {
        [JsonProperty("stream_url")]
        public string? StreamUrl { get; set; }

        [JsonProperty("stream_url_hd")]
        public string? StreamUrlHd { get; set; }

        [JsonProperty("duration")]
        public string? Duration { get; set; }
    }

    public partial class Urls
    {
        [JsonProperty("mp4_8k_mp4")]
        public string? Mp48kMp4 { get; set; }

        [JsonProperty("mp4_4k_mp4")]
        public string? Mp44kMp4 { get; set; }

        [JsonProperty("mp4_2k_mp4")]
        public string? Mp42kMp4 { get; set; }

        [JsonProperty("mp4_1080p_mp4")]
        public string? Mp41080pMp4 { get; set; }

        [JsonProperty("mp4_720p_mp4")]
        public string? Mp4720pMp4 { get; set; }

        [JsonProperty("mp4_hd_mp4")]
        public string? Mp4HDMp4 { get; set; }

        [JsonProperty("mp4_ld_mp4")]
        public string? Mp4LDMp4 { get; set; }
    }

    public partial class PagePic
    {
        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }
    }

    public partial class Pic
    {
        [JsonProperty("pid")]
        public string? Pid { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("size")]
        public string? Size { get; set; }

        [JsonProperty("geo")]
        public object? Geo { get; set; }

        [JsonProperty("large")]
        public Large? Large { get; set; }

        [JsonProperty("videoSrc")]
        public string? VideoSrc { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }
    }

    public partial class PicGeo
    {
        [JsonProperty("width")]
        public long? Width { get; set; }

        [JsonProperty("height")]
        public long? Height { get; set; }

        [JsonProperty("croped")]
        public bool? Croped { get; set; }
    }

    public partial class Large
    {
        [JsonProperty("size")]
        public string? Size { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("geo")]
        public object? Geo { get; set; }
    }

    public partial class LargeGeo
    {
        [JsonProperty("width")]
        public long? Width { get; set; }

        [JsonProperty("height")]
        public long? Height { get; set; }

        [JsonProperty("croped")]
        public bool? Croped { get; set; }
    }

    public partial class User
    {
        [JsonProperty("id")]
        public long? Id { get; set; }

        [JsonProperty("screen_name")]
        public string? ScreenName { get; set; }

        [JsonProperty("profile_image_url")]
        public string? ProfileImageUrl { get; set; }

        [JsonProperty("profile_url")]
        public string? ProfileUrl { get; set; }

        [JsonProperty("close_blue_v")]
        public bool? CloseBlueV { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("follow_me")]
        public bool? FollowMe { get; set; }

        [JsonProperty("following")]
        public bool? Following { get; set; }

        [JsonProperty("follow_count")]
        public long? FollowCount { get; set; }

        [JsonProperty("followers_count")]
        public string? FollowersCount { get; set; }

        [JsonProperty("cover_image_phone")]
        public string? CoverImagePhone { get; set; }

        [JsonProperty("avatar_hd")]
        public string? AvatarHd { get; set; }

        [JsonProperty("badge")]
        public Dictionary<string, long?>? Badge { get; set; }

        [JsonProperty("statuses_count")]
        public long? StatusesCount { get; set; }

        [JsonProperty("verified")]
        public bool? Verified { get; set; }

        [JsonProperty("verified_type")]
        public long? VerifiedType { get; set; }

        [JsonProperty("gender")]
        public string? Gender { get; set; }

        [JsonProperty("mbtype")]
        public long? Mbtype { get; set; }

        [JsonProperty("svip")]
        public long? Svip { get; set; }

        [JsonProperty("urank")]
        public long? Urank { get; set; }

        [JsonProperty("mbrank")]
        public long? Mbrank { get; set; }

        [JsonProperty("followers_count_str")]
        public string? FollowersCountStr { get; set; }

        [JsonProperty("verified_reason")]
        public string? VerifiedReason { get; set; }

        [JsonProperty("like")]
        public bool? Like { get; set; }

        [JsonProperty("like_me")]
        public bool? LikeMe { get; set; }

        [JsonProperty("special_follow")]
        public bool? SpecialFollow { get; set; }
    }

    public partial class Visible
    {
        [JsonProperty("type")]
        public long? Type { get; set; }

        [JsonProperty("list_id")]
        public long? ListId { get; set; }
    }
}
