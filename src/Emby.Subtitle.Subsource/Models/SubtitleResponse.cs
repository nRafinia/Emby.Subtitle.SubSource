namespace Emby.Subtitle.SubSource.Models
{
    public class SubtitlesResponse
    {
        public Subtitles[] subtitles { get; set; }

        public class Subtitles
        {
            public int id { get; set; }
            public string language { get; set; }
            public string release_type { get; set; }
            public string release_info { get; set; }
            public string upload_date { get; set; }
            public int? hearing_impaired { get; set; }
            public string caption { get; set; }
            public string rating { get; set; }
            public int uploader_id { get; set; }
            public string uploader_displayname { get; set; }
            public string[] uploader_badges { get; set; }
            public string link { get; set; }
            public string production_type { get; set; }
            public bool last_subtitle { get; set; }
        }
    }
}