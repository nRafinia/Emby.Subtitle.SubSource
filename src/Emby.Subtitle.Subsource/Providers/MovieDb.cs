using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Subtitle.SubSource.Models;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.Subtitle.SubSource.Providers
{
    public class MovieDb
    {
        private const string Token = "d9d7bb04fb2c52c2b594c5e30065c23c"; // Get https://www.themoviedb.org/ API token
        private const string MovieDbDomain = "https://api.themoviedb.org";
        private const string MovieUrl =  "/3/movie/{0}?api_key={1}";
        private const string TvUrl = "/3/tv/{0}?api_key={1}";
        private const string SearchMovieUrl = "/3/find/{0}/?api_key={1}&external_source={2}";

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationHost _appHost;
        private readonly ILogger _logger;

        public MovieDb(IJsonSerializer jsonSerializer, IHttpClient httpClient, IApplicationHost appHost, ILogger logger)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _appHost = appHost;
            _logger = logger;
        }

        #region Public methods

        public async Task<MovieInformation> GetMovieInfo(string id, CancellationToken cancellationToken)
        {
            var opts = BaseRequestOptions(cancellationToken);
            opts.Url =new Uri(string.Format(MovieUrl, id, Token),UriKind.Relative).ToString();

            using var response = await _httpClient.GetResponse(opts);

            if (response.ContentLength < 0)
            {
                return null;
            }

            var searchResults = await _jsonSerializer.DeserializeFromStreamAsync<MovieInformation>(response.Content);

            return searchResults;
        }

        public async Task<TvInformation> GetTvInfo(string id, CancellationToken cancellationToken)
        {
            var movie = await SearchMovie(id, cancellationToken);

            if (movie?.tv_episode_results == null || !movie.tv_episode_results.Any())
            {
                return null;
            }

            var opts = BaseRequestOptions(cancellationToken);
            opts.Url =new Uri(string.Format(TvUrl, movie.tv_episode_results.First().show_id, Token),UriKind.Relative).ToString();

            using var response = await _httpClient.GetResponse(opts);
            if (response.ContentLength < 0)
            {
                return null;
            }

            var searchResults = _jsonSerializer.DeserializeFromStream<TvInformation>(response.Content);

            return searchResults;
        }

        #endregion

        #region Private Methods

        private async Task<FindMovie> SearchMovie(string id, CancellationToken cancellationToken)
        {
            var opts = BaseRequestOptions(cancellationToken);
            var type = id.StartsWith("tt") ? MovieSourceType.imdb_id : MovieSourceType.tvdb_id;
            opts.Url =new Uri(string.Format(SearchMovieUrl, id, Token, type.ToString()),UriKind.Relative).ToString();

            using var response = await _httpClient.GetResponse(opts);
            if (response.ContentLength < 0)
            {
                return null;
            }

            var searchResults = _jsonSerializer.DeserializeFromStream<FindMovie>(response.Content);

            return searchResults;
        }

        private HttpRequestOptions BaseRequestOptions(CancellationToken cancellationToken) => new HttpRequestOptions
        {
            Host = MovieDbDomain,
            UserAgent = $"Emby/{_appHost?.ApplicationVersion}",
            //TimeoutMs = 20_000,
            CancellationToken = cancellationToken,
            LogRequestAsDebug = true,
            LogResponseHeaders = true
        };

        #endregion


        
    }
}