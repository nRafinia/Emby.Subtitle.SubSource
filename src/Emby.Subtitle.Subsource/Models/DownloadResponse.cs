namespace Emby.Subtitle.SubSource.Models
{
    public class DownloadResponse
    {
        public Subtitle subtitle { get; set; }

        public class Subtitle
        {
            public int id { get; set; }
            public string uploaded_at { get; set; }
            public string language { get; set; }
            public string rating { get; set; }
            public int uploaded_by { get; set; }
            public string[] release_info { get; set; }
            public string commentary { get; set; }
            public object files { get; set; }
            public object size { get; set; }
            public int downloads { get; set; }
            public object production_type { get; set; }
            public object release_type { get; set; }
            public object hearing_impaired { get; set; }
            public object foreign_parts { get; set; }
            public object framerate { get; set; }
            public object preview { get; set; }
            public bool user_uploaded { get; set; }
            public string download_token { get; set; }
        }
    }
}