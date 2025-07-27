using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Subtitle.SubSource.Helpers;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Logging;

namespace Emby.Subtitle.SubSource.Providers
{
    public class DownloadProvider
    {
        private const string DownloadUrl = "https://api.subsource.net/v1/subtitle/download/";

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IApplicationHost _appHost;
        private readonly ILocalizationManager _localizationManager;

        public DownloadProvider(IHttpClient httpClient, ILogger logger, IApplicationHost appHost,
            ILocalizationManager localizationManager)
        {
            _httpClient = httpClient;
            _logger = logger;
            _appHost = appHost;
            _localizationManager = localizationManager;
        }

        private HttpRequestOptions BaseRequestOptions => new HttpRequestOptions
        {
            UserAgent = $"Emby/{_appHost.ApplicationVersion}"
        };

        public async Task<SubtitleResponse> GetSubtitles(string id, string language, CancellationToken cancellationToken)
        {
            _logger?.Debug("SubSource, Downloading subtitle with id={id}", id);

            var opts = BaseRequestOptions;
            opts.Url = $"{DownloadUrl}{id}";

            var ms = new MemoryStream();
            try
            {
                using var response = await _httpClient.GetResponse(opts).ConfigureAwait(false);
                _logger?.Info("SubSource=" + response.ContentType);

                var contentType = response.ContentType.ToLower();
                if (contentType != "application/zip")
                {
                    return new SubtitleResponse()
                    {
                        Stream = ms
                    };
                }

                var archive = new ZipArchive(response.Content);

                var item = (archive.Entries.Count > 1
                    ? archive.Entries.FirstOrDefault(a => a.FullName.ToLower().Contains("utf"))
                    : archive.Entries.First()) ?? archive.Entries.First();

                await item.Open().CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
                ms.Position = 0;

                var fileExt = item.FullName.Split('.').LastOrDefault();

                if (string.IsNullOrWhiteSpace(fileExt))
                {
                    fileExt = "srt";
                }

                return new SubtitleResponse
                {
                    Format = fileExt,
                    Language = _localizationManager.GetIsoLanguage(language),
                    Stream = ms
                };
            }
            catch (Exception ex)
            {
                _logger?.ErrorException("SubSource, Error downloading subtitle", ex);
                throw;
            }
        }
    }
}