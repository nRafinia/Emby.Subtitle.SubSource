using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Subtitle.SubSource.Helpers;
using Emby.Subtitle.SubSource.Models;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Subtitle.SubSource
{
    public class MovieProvider
    {
        private const string Domain = "https://api.subsource.net";
        private const string SearchMovieUrl = "/api/v1/movies/search?searchType={0}";
        private const string SubtitlesUrl = "/api/v1/subtitles?movieId={0}&language={1}&sort=rating";
        private const string DownloadUrl = "/api/v1/subtitles/{0}/download";

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IApplicationHost _appHost;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILocalizationManager _localizationManager;
        private readonly PluginConfiguration _configuration;

        public MovieProvider(IHttpClient httpClient, ILogger logger, IApplicationHost appHost,
            IJsonSerializer jsonSerializer, ILocalizationManager localizationManager, PluginConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _appHost = appHost;
            _jsonSerializer = jsonSerializer;
            _localizationManager = localizationManager;
            _configuration = configuration;
        }

        public async Task<List<RemoteSubtitleInfo>> SearchMovie(string title, int? year, string lang, string movieId,
            CancellationToken cancellationToken)
        {
            var foundedMovie = await Search(title, movieId, year, "movie", null, cancellationToken);
            if (foundedMovie == null)
            {
                return new List<RemoteSubtitleInfo>(0);
            }

            var res = await ExtractMovieSubtitles(foundedMovie.movieId, lang, cancellationToken);
            return res;
        }

        public async Task<List<RemoteSubtitleInfo>> SearchSeries(string title, string lang, string movieId,
            int season, int episode, CancellationToken cancellationToken)
        {
            var foundedMovie = await Search(title, movieId, null, "series", season, cancellationToken);
            if (foundedMovie == null)
            {
                return new List<RemoteSubtitleInfo>(0);
            }

            var res = await ExtractSeriesSubtitles(foundedMovie.movieId, lang, season, episode, cancellationToken);
            return res;
        }

        public async Task<SubtitleResponse> DownloadSubtitle(string subtitleId, string language,
            CancellationToken cancellationToken)
        {
            _logger?.Debug($"SubSource, Downloading subtitle with id={subtitleId}");

            var url = string.Format(DownloadUrl, subtitleId);
            var requestOptions = BaseRequestOptions(url, cancellationToken);

            try
            {
                using var response = await _httpClient.GetResponse(requestOptions).ConfigureAwait(false);
                _logger?.Info("SubSource, " + response.ContentType);

                var contentType = response.ContentType.ToLower();
                if (contentType != "application/zip")
                {
                    return new SubtitleResponse()
                    {
                        Stream = new MemoryStream()
                    };
                }

                var archive = new ZipArchive(response.Content);

                var compressedSubtitle = (archive.Entries.Count > 1
                    ? archive.Entries.FirstOrDefault(a => a.FullName.ToLower().Contains("utf"))
                    : archive.Entries.First()) ?? archive.Entries.First();

                var memorySt = new MemoryStream();
                await compressedSubtitle.Open().CopyToAsync(memorySt, cancellationToken).ConfigureAwait(false);
                memorySt.Position = 0;

                var fileExt = compressedSubtitle.FullName.Split('.').LastOrDefault();

                if (string.IsNullOrWhiteSpace(fileExt))
                {
                    fileExt = ".srt";
                }

                return new SubtitleResponse
                {
                    Format = fileExt,
                    Language = _localizationManager.GetIsoLanguage(language),
                    Stream = memorySt
                };
            }
            catch (Exception ex)
            {
                _logger?.ErrorException("SubSource, Error downloading subtitle", ex);
                throw;
            }
        }

        #region private methods

        private async Task<SearchResponse.Results> Search(string title, string movieId, int? year, string type,
            int? season, CancellationToken cancellationToken)
        {
            _logger?.Debug($"SubSource, Searching for '{title}', movie Id {movieId}");

            string searchUrl;

            if (string.IsNullOrWhiteSpace(movieId))
            {
                searchUrl = string.Format(SearchMovieUrl, "text");
                searchUrl += $"&q={title}";
            }
            else
            {
                searchUrl = string.Format(SearchMovieUrl, "imdb");
                searchUrl += $"&imdb={movieId}";
            }

            if (year != null)
            {
                searchUrl += $"&year={year}";
            }

            if (season != null)
            {
                searchUrl += $"&season={season}";
            }

            searchUrl += $"&type={type}";

            var requestOptions = BaseRequestOptions(searchUrl, cancellationToken);

            using var response = await _httpClient.GetResponse(requestOptions);
            if (response.ContentLength < 0)
            {
                return null;
            }

            var searchResponse = _jsonSerializer.DeserializeFromStream<SearchResponse>(response.Content);

            if (!searchResponse.success || searchResponse.data.Length == 0)
            {
                return null;
            }

            if (searchResponse.data.Length == 1)
            {
                return searchResponse.data.First();
            }

            var res = searchResponse.data
                .ToList();

            if (res.Count() > 1 && !string.IsNullOrWhiteSpace(title))
            {
                res = res
                    .Where(s => s.title == title)
                    .ToList();
            }

            if (res.Count() > 1 && year != null)
            {
                res = res
                    .Where(s => s.releaseYear == year)
                    .ToList();
            }

            return res.FirstOrDefault();
        }

        private async Task<List<RemoteSubtitleInfo>> ExtractMovieSubtitles(int movieId, string lang,
            CancellationToken cancellationToken)
        {
            _logger?.Debug($"SubSource, Extracting subtitles for movie link={movieId}, language={lang}");

            var url = string.Format(SubtitlesUrl, movieId, lang.MapFromEmbyLanguage());
            var requestOptions = BaseRequestOptions(url, cancellationToken);

            using var response = await _httpClient.GetResponse(requestOptions);
            if (response.ContentLength < 0)
            {
                return new List<RemoteSubtitleInfo>(0);
            }

            var subtitleResponse = _jsonSerializer.DeserializeFromStream<SubtitlesResponse>(response.Content);

            var res = subtitleResponse.data.Select(s => new RemoteSubtitleInfo()
            {
                Id = $"{s.subtitleId}_{s.language}",
                Name = $"{string.Join(',', s.releaseInfo)}",
                Author = s.contributors.FirstOrDefault()?.displayname,
                ProviderName = Const.PluginName,
                Comment = s.commentary,
                CommunityRating = s.rating["good"],
                DateCreated = s.createdAt,
                Language = s.language,
                DownloadCount = s.downloads,
                Format = "srt"
            }).ToList();

            return res;
        }

        private async Task<List<RemoteSubtitleInfo>> ExtractSeriesSubtitles(int movieId, string lang, int season,
            int episode, CancellationToken cancellationToken)
        {
            var url = string.Format(SubtitlesUrl, movieId, lang.MapFromEmbyLanguage());
            var requestOptions = BaseRequestOptions(url, cancellationToken);

            using var response = await _httpClient.GetResponse(requestOptions);
            if (response.ContentLength < 0)
            {
                return new List<RemoteSubtitleInfo>(0);
            }

            var subtitleResponse = _jsonSerializer.DeserializeFromStream<SubtitlesResponse>(response.Content);

            var episodeCodeF1 = $"S{season.ToString().PadLeft(2, '0')}E{episode.ToString().PadLeft(2, '0')}";
            var episodeCodeF2 = $"S{season}E{episode.ToString().PadLeft(2, '0')}";
            var episodeCodeF3 = $"S{season.ToString().PadLeft(2, '0')}E{episode}";
            var episodeCodeF4 = $"S{season}E{episode}";
            var episodeSubtitles = subtitleResponse.data
                .Select(s => new { subtitle = s, title = string.Join(',', s.releaseInfo) + "," + s.commentary })
                .Where(s =>
                    s.title.Contains(episodeCodeF1, StringComparison.CurrentCultureIgnoreCase) ||
                    s.title.Contains(episodeCodeF2, StringComparison.CurrentCultureIgnoreCase) ||
                    s.title.Contains(episodeCodeF3, StringComparison.CurrentCultureIgnoreCase) ||
                    s.title.Contains(episodeCodeF4, StringComparison.CurrentCultureIgnoreCase)
                );

            var res = episodeSubtitles
                .Select(s => new RemoteSubtitleInfo()
                {
                    Id = $"{s.subtitle.subtitleId}_{s.subtitle.language}",
                    Name = $"{string.Join(',', s.subtitle.releaseInfo)}",
                    Author = s.subtitle.contributors.FirstOrDefault()?.displayname,
                    ProviderName = Const.PluginName,
                    Comment = s.subtitle.commentary,
                    CommunityRating = s.subtitle.rating["good"],
                    DateCreated = s.subtitle.createdAt,
                    Language = s.subtitle.language,
                    DownloadCount = s.subtitle.downloads,
                    Format = "srt"
                }).ToList();

            return res;
        }

        private HttpRequestOptions BaseRequestOptions(string url, CancellationToken cancellationToken) =>
            new HttpRequestOptions
            {
                Url = Domain + url,
                UserAgent = $"Emby/{_appHost?.ApplicationVersion}",
                //TimeoutMs = 20_000,
                CancellationToken = cancellationToken,
                LogRequestAsDebug = true,
                LogResponseHeaders = true,
                RequestHeaders = { { "X-API-Key", _configuration.ApiKey } }
            };

        #endregion
    }
}