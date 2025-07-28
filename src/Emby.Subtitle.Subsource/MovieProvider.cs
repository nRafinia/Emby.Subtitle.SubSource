using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
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
        private const string SearchMovieUrl = "/v1/movie/search";
        private const string SubtitlesUrl = "/v1/subtitles{0}?language={1}&sort_by_date=false";
        private const string DownloadPageUrl = "/v1/subtitle/{0}";
        private const string DownloadUrl = "/v1/subtitle/download/";

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IApplicationHost _appHost;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILocalizationManager _localizationManager;

        public MovieProvider(IHttpClient httpClient, ILogger logger, IApplicationHost appHost,
            IJsonSerializer jsonSerializer, ILocalizationManager localizationManager)
        {
            _httpClient = httpClient;
            _logger = logger;
            _appHost = appHost;
            _jsonSerializer = jsonSerializer;
            _localizationManager = localizationManager;
        }

        public async Task<List<RemoteSubtitleInfo>> SearchMovie(string title, int? year, string lang, string movieId,
            CancellationToken cancellationToken)
        {
            var foundedMovie = await Search(title, movieId, year, "movie", cancellationToken);
            if (foundedMovie == null)
            {
                return new List<RemoteSubtitleInfo>(0);
            }

            var res = await ExtractMovieSubtitles(foundedMovie.link, lang, cancellationToken);
            return res;
        }

        public async Task<List<RemoteSubtitleInfo>> SearchSeries(string title, string lang, string movieId,
            int season, int episode, CancellationToken cancellationToken)
        {
            var foundedMovie = await Search(title, movieId, null, "tvseries", cancellationToken);
            if (foundedMovie == null)
            {
                return new List<RemoteSubtitleInfo>(0);
            }

            var link = $"{foundedMovie.link}/season-{season}";
            var res = await ExtractSeriesSubtitles(link, lang, season, episode, cancellationToken);
            return res;
        }

        public async Task<(string Token, string Language)> GetDownloadLink(string id,
            CancellationToken cancellationToken)
        {
            var subtitleData = id.Split('|');
            var movieName = subtitleData[0];
            var language = subtitleData[1];

            _logger?.Debug($"SubSource, Downloading subtitle with name={movieName}, language{language}");

            var url = string.Format(DownloadPageUrl, id.Replace('|', '/'));
            var requestOptions = BaseRequestOptions(url, cancellationToken);

            using var response = await _httpClient.GetResponse(requestOptions);
            if (response.ContentLength < 0)
            {
                return (string.Empty, string.Empty);
            }

            var downloadPageResponse = _jsonSerializer.DeserializeFromStream<DownloadResponse>(response.Content);

            return (downloadPageResponse?.subtitle?.download_token, language);
        }

        public async Task<SubtitleResponse> DownloadSubtitle(string downloadToken, string language,
            CancellationToken cancellationToken)
        {
            _logger?.Debug($"SubSource, Downloading subtitle with id={downloadToken}");

            var url = $"{DownloadUrl}{downloadToken}";
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
            CancellationToken cancellationToken)
        {
            _logger?.Debug($"SubSource, Searching for '{title}', movie Id {movieId}");

            var requestOptions = BaseRequestOptions(SearchMovieUrl, cancellationToken);

            var searchText = !string.IsNullOrWhiteSpace(movieId)
                ? movieId
                : title;

            var request = new
            {
                query = searchText,
                signal = new { },
                includeSeasons = false,
                limit = 10
            };

            requestOptions.RequestHttpContent = new StringContent(_jsonSerializer.SerializeToString(request),
                Encoding.UTF8, "application/json");

            using var response = await _httpClient.Post(requestOptions);
            if (response.ContentLength < 0)
            {
                return null;
            }

            var searchResponse = _jsonSerializer.DeserializeFromStream<SearchResponse>(response.Content);

            if (searchResponse.success == false || searchResponse.results.Length == 0)
            {
                return null;
            }

            if (searchResponse.results.Length == 1)
            {
                return searchResponse.results.First();
            }

            var res = searchResponse.results
                .Where(s => s.type == type)
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

        private async Task<List<RemoteSubtitleInfo>> ExtractMovieSubtitles(string link, string lang,
            CancellationToken cancellationToken)
        {
            var url = string.Format(SubtitlesUrl, link, lang.MapFromEmbyLanguage());
            var requestOptions = BaseRequestOptions(url, cancellationToken);

            using var response = await _httpClient.GetResponse(requestOptions);
            if (response.ContentLength < 0)
            {
                return new List<RemoteSubtitleInfo>(0);
            }

            var subtitleResponse = _jsonSerializer.DeserializeFromStream<SubtitlesResponse>(response.Content);

            var res = subtitleResponse.subtitles.Select(s => new RemoteSubtitleInfo()
            {
                Id = s.link.Replace('/', '|'),
                Name = s.release_info,
                Author = s.uploader_displayname,
                ProviderName = Const.PluginName,
                Comment = s.caption,
                Format = "srt"
            }).ToList();

            return res;
        }

        private async Task<List<RemoteSubtitleInfo>> ExtractSeriesSubtitles(string link, string lang, int season,
            int episode, CancellationToken cancellationToken)
        {
            var url = string.Format(SubtitlesUrl, link, lang.MapFromEmbyLanguage());
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
            var episodeSubtitles = subtitleResponse.subtitles
                .Where(s =>
                    s.release_info.Contains(episodeCodeF1, StringComparison.CurrentCultureIgnoreCase) ||
                    s.release_info.Contains(episodeCodeF2, StringComparison.CurrentCultureIgnoreCase) ||
                    s.release_info.Contains(episodeCodeF3, StringComparison.CurrentCultureIgnoreCase) ||
                    s.release_info.Contains(episodeCodeF4, StringComparison.CurrentCultureIgnoreCase)
                );

            var res = episodeSubtitles.Select(s => new RemoteSubtitleInfo()
            {
                Id = s.link.Replace('/', '|'),
                Name = s.release_info,
                Author = s.uploader_displayname,
                ProviderName = Const.PluginName,
                Comment = s.caption,
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
                LogResponseHeaders = true
            };

        #endregion
    }
}