using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Subtitle.SubSource.Providers
{
    public class MovieProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IApplicationHost _appHost;
        private readonly IJsonSerializer _jsonSerializer;

        public MovieProvider(IHttpClient httpClient, ILogger logger, IApplicationHost appHost,
            IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _logger = logger;
            _appHost = appHost;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<List<RemoteSubtitleInfo>> Search(string title, int? year, string lang, string movieId,
            CancellationToken cancellationToken)
        {
            var res = new List<RemoteSubtitleInfo>();

            if (!string.IsNullOrWhiteSpace(movieId))
            {
                var mDb = new MovieDb(_jsonSerializer, _httpClient, _appHost, _logger);
                var info = await mDb.GetMovieInfo(movieId, cancellationToken);

                if (info != null)
                {
                    year = info.release_date.Year;
                    title = info.Title;
                    _logger?.Info($"SubSource= Original movie title=\"{info.Title}\", year={info.release_date.Year}");
                }
            }

            var html = await SearchSubSourceMovie(title, year, lang, cancellationToken);
            if (string.IsNullOrWhiteSpace(html))
                return res;

            res = ExtractMovieSubtitleLinks(html, lang);

            return res;
        }

        private List<RemoteSubtitleInfo> ExtractMovieSubtitleLinks(string html, string lang)
        {
            var res = new List<RemoteSubtitleInfo>();

            #region Extract subtitle links

            var xml = new XmlDocument();
            xml.LoadXml($"{XmlTag}{html}");

            var repeater = xml.SelectNodes("//table/tbody/tr");

            if (repeater == null)
            {
                return res;
            }

            foreach (XmlElement node in repeater)
            {
                var name = RemoveExtraCharacter(node?.SelectSingleNode(".//a")?.SelectNodes("span")?.Item(1)
                    ?.InnerText);

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var id = (node?
                        .SelectSingleNode(".//a")?
                        .Attributes?["href"].Value + "___" + lang)
                    .Replace("/", "__");

                var item = new RemoteSubtitleInfo
                {
                    Id = id,
                    Name = RemoveExtraCharacter(node?.SelectSingleNode(".//a")?.SelectNodes("span")?.Item(1)
                        ?.InnerText),
                    Author = RemoveExtraCharacter(node?.SelectSingleNode("td[@class='a6']")?.InnerText),
                    ProviderName = RemoveExtraCharacter(node?.SelectSingleNode("td[@class='a5']")?.InnerText),
                    Language = NormalizeLanguage(lang),
                    IsHashMatch = true
                };
                res.Add(item);
            }

            #endregion

            return res;
        }

        private async Task<string> SearchSubSourceMovie(string title, int? year, string lang,
            CancellationToken cancellationToken)
        {
            #region Search SubSource

            _logger?.Debug($"SubSource= Searching for site search \"{title}\"");
            var url = string.Format(SearchUrl, HttpUtility.UrlEncode(title));
            var html = await GetHtml(Domain, url, cancellationToken);

            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var xml = new XmlDocument();
            xml.LoadXml($"{XmlTag}{html}");

            var xNode = xml.SelectSingleNode("//div[@class='search-result']");
            if (xNode == null)
            {
                return string.Empty;
            }

            var ex = xNode.SelectSingleNode("h2[@class='exact']")
                     ?? xNode.SelectSingleNode("h2[@class='close']")
                     ?? xNode.SelectSingleNode("h2[@class='popular']");

            if (ex == null)
            {
                return string.Empty;
            }

            xNode = xNode.SelectSingleNode("ul");

            var sItems = xNode?.SelectNodes(".//a");
            if (sItems == null)
            {
                return string.Empty;
            }

            foreach (XmlNode item in sItems)
            {
                if (item is null)
                {
                    continue;
                }

                var sYear = item.InnerText
                    .Split('(', ')')[1];

                if ((year ?? 0) != Convert.ToInt16(sYear))
                {
                    continue;
                }

                var link = item.Attributes?["href"].Value;
                link += $"/{MapLanguage(lang)}";
                html = await GetHtml(Domain, link, cancellationToken);
                break;
            }

            #endregion

            return html;
        }
    }
}