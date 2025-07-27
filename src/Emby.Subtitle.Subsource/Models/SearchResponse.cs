namespace Emby.Subtitle.SubSource.Models
{
    public class SearchResponse
    {
        public bool success { get; set; }
        public Results[] results { get; set; }
        public int count { get; set; }
        public string source { get; set; }

        public class Results
        {
            public int id { get; set; }
            public string title { get; set; }
            public string type { get; set; }
            public string link { get; set; }
            public int releaseYear { get; set; }
            public string poster { get; set; }
            public object rating { get; set; }
            public object[] cast { get; set; }
            public object[] genres { get; set; }
        }
    }
}