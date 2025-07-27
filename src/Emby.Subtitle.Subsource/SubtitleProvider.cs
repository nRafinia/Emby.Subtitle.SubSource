using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Subtitle.SubSource.Helpers;
using Emby.Subtitle.SubSource.Providers;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

// ReSharper disable StringLiteralTypo

namespace Emby.Subtitle.SubSource
{
    public class SubtitleProvider : ISubtitleProvider, IHasOrder
    {
        #region Constants

        public string Name => Const.PluginName;

        public IEnumerable<VideoContentType> SupportedMediaTypes => new List<VideoContentType>()
        {
            VideoContentType.Movie,
            VideoContentType.Episode
        };

        public int Order => 0;

        #endregion

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IApplicationHost _appHost;
        private readonly ILocalizationManager _localizationManager;
        private readonly IJsonSerializer _jsonSerializer;

        public SubtitleProvider(IHttpClient httpClient, ILogger logger, IApplicationHost appHost
            , ILocalizationManager localizationManager, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _logger = logger;
            _appHost = appHost;
            _localizationManager = localizationManager;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request,
            CancellationToken cancellationToken)
        {
            var title = request.ContentType == VideoContentType.Movie
                ? request.Name
                : request.SeriesName;

            _logger?.Info(
                "SubSource, Request subtitle for '{title}', language={lang}, year={year}, movie Id={movieId}, Season={season}, Episode={episode}",
                title, request.Language, request.ProductionYear, request.ParentIndexNumber, request.IndexNumber);

            var imdbRecognitionData = request.ProviderIds?
                .FirstOrDefault(p =>
                    string.Equals(p.Key, "imdb", StringComparison.CurrentCultureIgnoreCase));
            var imdbCode = imdbRecognitionData?.Value;

            var foundedSubtitles = request.ContentType == VideoContentType.Movie
                ? await SearchMovie(request, imdbCode, cancellationToken)
                : await SearchSeries(request, imdbCode, cancellationToken);

            _logger?.Debug($"SubSource, result found={foundedSubtitles.Count()}");

            foundedSubtitles.RemoveAll(l => string.IsNullOrWhiteSpace(l.Name));

            var result = foundedSubtitles.GroupBy(s => s.Id)
                .Select(s => new RemoteSubtitleInfo()
                {
                    Id = s.First().Id,
                    Name = $"{s.First().ProviderName} ({s.First().Author})",
                    Author = s.First().Author,
                    ProviderName = Name,
                    Comment = string.Join("<br/>", s.Select(n => n.Name)),
                    Format = s.First().Format
                }).ToList();
            
            return result
                .OrderBy(s => s.Name)
                .ToList();
        }

        public Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            var subtitleData = id.Split('|');
            var subtitleId = subtitleData.First();
            var subtitleLanguage = subtitleData.Last();

            var provider = new DownloadProvider(_httpClient, _logger, _appHost, _localizationManager);
            return provider.GetSubtitles(subtitleId, subtitleLanguage, cancellationToken);
        }

        #region private methods

        private Task<List<RemoteSubtitleInfo>> SearchMovie(SubtitleSearchRequest request, string imdbCode,
            CancellationToken cancellationToken)
        {
            var provider = new MovieProvider(_httpClient, _logger, _appHost, _jsonSerializer);
            return provider.Search(request.Name, request.ProductionYear, request.Language, imdbCode,
                cancellationToken);
        }

        private Task<List<RemoteSubtitleInfo>> SearchSeries(SubtitleSearchRequest request, string imdbCode,
            CancellationToken cancellationToken)
        {
            if (request.ContentType == VideoContentType.Episode &&
                (request.ParentIndexNumber == null || request.IndexNumber == null))
            {
                return Task.FromResult(new List<RemoteSubtitleInfo>(0));
            }

            var provider = new TvProvider(_httpClient, _logger, _appHost, _jsonSerializer);
            return provider.Search(request.Name, request.ProductionYear, request.Language, imdbCode,
                request.ParentIndexNumber ?? 0, request.IndexNumber ?? 0, cancellationToken);
        }

        #endregion
    }
}