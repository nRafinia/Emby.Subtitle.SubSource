namespace Emby.Subtitle.SubSource.Models
{
    public class SearchResponse : BaseResponse
    {
        public Results[] data { get; set; }
        public int count => data.Length;

        public class Results
        {
            public int movieId { get; set; }
            public string title { get; set; }
            public string type { get; set; }
            public int releaseYear { get; set; }
            public int? Season { get; set; }
        }
    }
}