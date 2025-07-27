using System.Collections.Generic;

namespace Emby.Subtitle.SubSource.Models
{
    public class FindMovie
    {
        public IEnumerable<TvEpisodeResult> tv_episode_results { get; set; }
    }
}