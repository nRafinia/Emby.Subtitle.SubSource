using System;
using System.Collections.Generic;

namespace Emby.Subtitle.SubSource.Models
{
    public class SubtitlesResponse:BaseResponse
    {
        public Subtitles[] data { get; set; }

        public class Subtitles
        {
            public int subtitleId { get; set; }
            public string language { get; set; }
            public List<string> releaseInfo { get; set; }
            public DateTime createdAt { get; set; }
            public string commentary { get; set; }
            public string productionType { get; set; }
            public string releaseType { get; set; }
            public Dictionary<string,int> rating { get; set; }
            public int downloads { get; set; }
            public string preview { get; set; }
            public List<Contributor> contributors { get; set; }
            public string link { get; set; }
        }
        
        public class Contributor
        {
            public int id { get; set; }
            public string displayname { get; set; }
        }
    }
}