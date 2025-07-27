namespace Emby.Subtitle.SubSource.Models
{
    public class DownloadResponse
    {
        public SubtitleDownload subtitle { get; set; }
    }

    public class SubtitleDownload
    {
        public string download_token { get; set; }
    }
}
