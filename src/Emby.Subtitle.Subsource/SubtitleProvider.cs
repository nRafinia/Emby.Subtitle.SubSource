using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Subtitle.SubSource.Models;
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

        private readonly ILogger _logger;
        private readonly MovieProvider _provider;
        private PluginConfiguration Configuration => Plugin.Instance.GetConfiguration();

        public SubtitleProvider(IHttpClient httpClient, ILogger logger, IApplicationHost appHost
            , ILocalizationManager localizationManager, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _provider = new MovieProvider(httpClient, logger, appHost, jsonSerializer, localizationManager,
                Configuration);
            logger.Debug("SubSource, apiKey={0}", Configuration?.ApiKey);
        }

        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request,
            CancellationToken cancellationToken)
        {
            var title = request.ContentType == VideoContentType.Movie
                ? request.Name
                : request.SeriesName;

            _logger?.Info(
                $"SubSource, Request subtitle for '{title}', language={request.Language}, year={request.ProductionYear}, Season={request.ParentIndexNumber}, Episode={request.IndexNumber}");

            var imdbRecognitionData = request.ProviderIds?
                .FirstOrDefault(p =>
                    string.Equals(p.Key, "imdb", StringComparison.CurrentCultureIgnoreCase));
            var imdbCode = imdbRecognitionData?.Value;

            var foundedSubtitles = request.ContentType == VideoContentType.Movie
                ? await SearchMovie(request, imdbCode, cancellationToken)
                : await SearchSeries(request, imdbCode, cancellationToken);

            _logger?.Debug($"SubSource, result found={foundedSubtitles.Count()}");

            foundedSubtitles.RemoveAll(l => string.IsNullOrWhiteSpace(l.Name));

            return foundedSubtitles;
        }

        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            var data = id.Split('_');
            return await _provider.DownloadSubtitle(data[0],data[1], cancellationToken);
        }

        #region private methods

        private Task<List<RemoteSubtitleInfo>> SearchMovie(SubtitleSearchRequest request, string imdbCode,
            CancellationToken cancellationToken)
        {
            return _provider.SearchMovie(request.Name, request.ProductionYear, request.Language, imdbCode,
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

            return _provider.SearchSeries(request.Name, request.Language, imdbCode,
                request.ParentIndexNumber ?? 0, request.IndexNumber ?? 0, cancellationToken);
        }

        #endregion
    }
}