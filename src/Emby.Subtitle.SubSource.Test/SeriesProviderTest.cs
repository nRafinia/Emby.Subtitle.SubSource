using Emby.Subtitle.SubSource.Models;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using Moq;
using HttpRequestOptions = MediaBrowser.Common.Net.HttpRequestOptions;

namespace Emby.Subtitle.SubSource.Test
{
    public class SeriesProviderTest
    {
        private readonly Mock<IHttpClient> _mockHttpClient;
        private readonly Mock<IApplicationHost> _mockAppHost;
        private readonly Mock<IJsonSerializer> _mockJsonSerializer;
        private readonly MovieProvider _movieProvider;

        public SeriesProviderTest()
        {
            _mockHttpClient = new Mock<IHttpClient>();
            var mockLogger = new Mock<ILogger>();
            _mockAppHost = new Mock<IApplicationHost>();
            _mockJsonSerializer = new Mock<IJsonSerializer>();
            var mockLocalizationManager = new Mock<ILocalizationManager>();

            var config = new PluginConfiguration()
            {
                ApiKey = "test-api-key"
            };
            
            _movieProvider = new MovieProvider(
                _mockHttpClient.Object,
                mockLogger.Object,
                _mockAppHost.Object,
                _mockJsonSerializer.Object,
                mockLocalizationManager.Object,
                config
            );
        }

        [Fact]
        public async Task SearchSeries_WithValidTitle_ReturnsSubtitles()
        {
            // Arrange
            var title = "Game of Thrones";
            var lang = "farsi_persian";
            var movieId = "";
            var season = 3;
            var episode = 9;
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = true,
                results =
                [
                    new SearchResponse.Results
                    {
                        movieId = 38948,
                        title = "Game of Thrones",
                        type = "tvseries",
                        link = "/series/game-of-thrones",
                        releaseYear = 2011,
                        poster = "https://cdn.subsource.net/posters/38948/a788d127af5e8633e3a593be578ad096-small.jpg",
                        rating = 9.2,
                        cast = ["Emilia Clarke", "Peter Dinklage"],
                        genres = ["Action", "Adventure"]
                    }
                ],
                count = 1,
                source = "imdb_direct"
            };

            var subtitlesResponse = new SubtitlesResponse
            {
                data =
                [
                    new SubtitlesResponse.Subtitles()
                    {
                        subtitleId = 739744,
                        language = "farsi_persian",
                        release_type = "Other",
                        release_info = "Game.of.Thrones.S3E09.All.HDTV",
                        createdAt = "2013-06-03T15:34:00.000Z",
                        hearing_impaired = null,
                        commentary = "The Rains of Castamere - باران هاي کاستامر",
                        rating = "unrated",
                        uploader_id = 718292,
                        uploader_displayname = "lvlr",
                        uploader_badges = [],
                        link = "game-of-thrones-season-3/farsi_persian/739744",
                        production_type = null,
                        last_subtitle = false
                    },
                    new SubtitlesResponse.Subtitles()
                    {
                        subtitleId = 734679,
                        language = "farsi_persian",
                        release_type = "Other",
                        release_info = "Game.of.Thrones.S3E08.All.HDTV  IranFilm",
                        createdAt = "2013-05-22T16:42:00.000Z",
                        hearing_impaired = null,
                        commentary = "Second Sons - پسران دوم",
                        rating = "unrated",
                        uploader_id = 702064,
                        uploader_displayname = "taktaz",
                        uploader_badges = [],
                        link = "game-of-thrones-season-3/farsi_persian/734679",
                        production_type = null,
                        last_subtitle = false
                    }
                ]
            };

            // Setup search response
            var searchResponseStream = new MemoryStream("search response"u8.ToArray());
            var searchHttpResponse = new HttpResponseInfo()
            {
                Content = searchResponseStream,
                ContentLength = searchResponseStream.Length
            };

            // Setup subtitles response  
            var subtitlesResponseStream = new MemoryStream("subtitles response"u8.ToArray());
            var subtitlesHttpResponse = new HttpResponseInfo()
            {
                Content = subtitlesResponseStream,
                ContentLength = subtitlesResponseStream.Length
            };

            _mockHttpClient.SetupSequence(c => c.Post(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(searchHttpResponse);

            _mockHttpClient.SetupSequence(c => c.GetResponse(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(subtitlesHttpResponse);

            _mockJsonSerializer.Setup(s => s.DeserializeFromStream<SearchResponse>(It.IsAny<Stream>()))
                .Returns(searchResponse);

            _mockJsonSerializer.Setup(s => s.DeserializeFromStream<SubtitlesResponse>(It.IsAny<Stream>()))
                .Returns(subtitlesResponse);

            _mockJsonSerializer.Setup(s => s.SerializeToString(It.IsAny<object>()))
                .Returns("{}");

            _mockAppHost.Setup(a => a.ApplicationVersion).Returns(new Version(1,0,0));

            // Act
            var result = await _movieProvider.SearchSeries(title, lang, movieId, season, episode, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Only S03E09 should match
            Assert.Equal("game-of-thrones-season-3|farsi_persian|739744", result[0].Id);
            Assert.Equal("Game.of.Thrones.S3E09.All.HDTV", result[0].Name);
            Assert.Equal("lvlr", result[0].Author);
            Assert.Equal("The Rains of Castamere - باران هاي کاستامر", result[0].Comment);
            Assert.Equal("srt", result[0].Format);
        }

        [Fact]
        public async Task SearchSeries_WithSeriesId_ReturnsSubtitles()
        {
            // Arrange
            var title = "Game of Thrones";
            var lang = "farsi_persian";
            var movieId = "tt0944947";
            var season = 3;
            var episode = 8;
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = true,
                results =
                [
                    new SearchResponse.Results
                    {
                        movieId = 38948,
                        title = "Game of Thrones",
                        type = "tvseries",
                        link = "/series/game-of-thrones",
                        releaseYear = 2011
                    }
                ],
                count = 1,
                source = "imdb_direct"
            };

            var subtitlesResponse = new SubtitlesResponse
            {
                data =
                [
                    new SubtitlesResponse.Subtitles()
                    {
                        subtitleId = 734679,
                        language = "farsi_persian",
                        release_info = "Game.of.Thrones.S3E08.All.HDTV  IranFilm",
                        commentary = "Second Sons - پسران دوم",
                        uploader_displayname = "taktaz",
                        link = "game-of-thrones-season-3/farsi_persian/734679"
                    }
                ]
            };

            // Setup mocks
            var searchResponseStream = new MemoryStream();
            var searchHttpResponse = new HttpResponseInfo()
            {
                Content = searchResponseStream,
                ContentLength = 1
            };
            
            var subtitlesResponseStream = new MemoryStream();
            var subtitlesHttpResponse = new HttpResponseInfo()
            {
                Content = subtitlesResponseStream,
                ContentLength = 1
            };

            _mockHttpClient.Setup(c => c.Post(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(searchHttpResponse);

            _mockHttpClient.Setup(c => c.GetResponse(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(subtitlesHttpResponse);

            _mockJsonSerializer.Setup(s => s.DeserializeFromStream<SearchResponse>(It.IsAny<Stream>()))
                .Returns(searchResponse);

            _mockJsonSerializer.Setup(s => s.DeserializeFromStream<SubtitlesResponse>(It.IsAny<Stream>()))
                .Returns(subtitlesResponse);

            _mockJsonSerializer.Setup(s => s.SerializeToString(It.IsAny<object>()))
                .Returns("{}");

            // Act
            var result = await _movieProvider.SearchSeries(title, lang, movieId, season, episode, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("game-of-thrones-season-3|farsi_persian|734679", result[0].Id);
        }

        [Fact]
        public async Task SearchSeries_NoSeriesFound_ReturnsEmptyList()
        {
            // Arrange
            var title = "NonExistentSeries";
            var lang = "farsi_persian";
            var movieId = "";
            var season = 1;
            var episode = 1;
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = false,
                results = [],
                count = 0,
                source = "imdb_direct"
            };

            var searchResponseStream = new MemoryStream();
            var searchHttpResponse = new HttpResponseInfo()
            {
                Content = searchResponseStream,
                ContentLength = 1
            };

            _mockHttpClient.Setup(c => c.Post(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(searchHttpResponse);

            _mockJsonSerializer.Setup(s => s.DeserializeFromStream<SearchResponse>(It.IsAny<Stream>()))
                .Returns(searchResponse);

            _mockJsonSerializer.Setup(s => s.SerializeToString(It.IsAny<object>()))
                .Returns("{}");

            // Act
            var result = await _movieProvider.SearchSeries(title, lang, movieId, season, episode, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchSeries_EmptySearchResponse_ReturnsEmptyList()
        {
            // Arrange
            var title = "Game of Thrones";
            var lang = "farsi_persian";
            var movieId = "";
            var season = 1;
            var episode = 1;
            var cancellationToken = CancellationToken.None;

            var searchHttpResponse = new HttpResponseInfo()
            {
                ContentLength = -1
            };

            _mockHttpClient.Setup(c => c.Post(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(searchHttpResponse);

            _mockJsonSerializer.Setup(s => s.SerializeToString(It.IsAny<object>()))
                .Returns("{}");

            // Act
            var result = await _movieProvider.SearchSeries(title, lang, movieId, season, episode, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchSeries_EmptySubtitlesResponse_ReturnsEmptyList()
        {
            // Arrange
            var title = "Game of Thrones";
            var lang = "farsi_persian";
            var movieId = "";
            var season = 1;
            var episode = 1;
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = true,
                results =
                [
                    new SearchResponse.Results
                    {
                        movieId = 38948,
                        title = "Game of Thrones",
                        type = "tvseries",
                        link = "/series/game-of-thrones",
                        releaseYear = 2011
                    }
                ],
                count = 1,
                source = "imdb_direct"
            };

            var searchResponseStream = new MemoryStream();
            var searchHttpResponse = new HttpResponseInfo()
            {
                Content = searchResponseStream,
                ContentLength = 1
            };

            var subtitlesHttpResponse = new HttpResponseInfo()
            {
                ContentLength = -1
            };

            _mockHttpClient.Setup(c => c.Post(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(searchHttpResponse);

            _mockHttpClient.Setup(c => c.GetResponse(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(subtitlesHttpResponse);

            _mockJsonSerializer.Setup(s => s.DeserializeFromStream<SearchResponse>(It.IsAny<Stream>()))
                .Returns(searchResponse);

            _mockJsonSerializer.Setup(s => s.SerializeToString(It.IsAny<object>()))
                .Returns("{}");

            // Act
            var result = await _movieProvider.SearchSeries(title, lang, movieId, season, episode, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchSeries_WithMultipleResults_FiltersCorrectly()
        {
            // Arrange
            var title = "Game of Thrones";
            var lang = "farsi_persian";
            var movieId = "";
            var season = 3;
            var episode = 9;
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = true,
                results =
                [
                    new SearchResponse.Results
                    {
                        movieId = 38948,
                        title = "Game of Thrones",
                        type = "tvseries",
                        link = "/series/game-of-thrones",
                        releaseYear = 2011
                    },
                    new SearchResponse.Results
                    {
                        movieId = 38945,
                        title = "Game of Thrones Conquest & Rebellion: An Animated History of the Seven Kingdoms",
                        type = "movie",
                        link = "/subtitles/game-of-thrones-conquest",
                        releaseYear = 2017
                    }
                ],
                count = 2,
                source = "imdb_direct"
            };

            var subtitlesResponse = new SubtitlesResponse
            {
                data =
                [
                    new SubtitlesResponse.Subtitles()
                    {
                        subtitleId = 739744,
                        language = "farsi_persian",
                        release_info = "Game.of.Thrones.S3E09.All.HDTV",
                        commentary = "The Rains of Castamere",
                        uploader_displayname = "lvlr",
                        link = "game-of-thrones-season-3/farsi_persian/739744"
                    }
                ]
            };

            var searchResponseStream = new MemoryStream();
            var searchHttpResponse = new HttpResponseInfo()
            {
                Content = searchResponseStream,
                ContentLength = 1
            };
            
            var subtitlesResponseStream = new MemoryStream();
            var subtitlesHttpResponse = new HttpResponseInfo()
            {
                Content = subtitlesResponseStream,
                ContentLength = 1
            };

            _mockHttpClient.Setup(c => c.Post(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(searchHttpResponse);

            _mockHttpClient.Setup(c => c.GetResponse(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(subtitlesHttpResponse);

            _mockJsonSerializer.Setup(s => s.DeserializeFromStream<SearchResponse>(It.IsAny<Stream>()))
                .Returns(searchResponse);

            _mockJsonSerializer.Setup(s => s.DeserializeFromStream<SubtitlesResponse>(It.IsAny<Stream>()))
                .Returns(subtitlesResponse);

            _mockJsonSerializer.Setup(s => s.SerializeToString(It.IsAny<object>()))
                .Returns("{}");

            // Act
            var result = await _movieProvider.SearchSeries(title, lang, movieId, season, episode, cancellationToken);

            // Assert - Should select the tvseries type, not movie
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task SearchSeries_EpisodeFiltering_ReturnsCorrectSubtitles()
        {
            // Arrange
            var title = "Game of Thrones";
            var lang = "farsi_persian";
            var movieId = "";
            var season = 3;
            var episode = 2;
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = true,
                results =
                [
                    new SearchResponse.Results
                    {
                        movieId = 38948,
                        title = "Game of Thrones",
                        type = "tvseries",
                        link = "/series/game-of-thrones",
                        releaseYear = 2011
                    }
                ],
                count = 1,
                source = "imdb_direct"
            };

            var subtitlesResponse = new SubtitlesResponse
            {
                data =
                [
                    new SubtitlesResponse.Subtitles()
                    {
                        subtitleId = 713369,
                        language = "farsi_persian",
                        release_info = "S03E02",
                        commentary = "Preza",
                        uploader_displayname = "mrjp_subtrans",
                        link = "game-of-thrones-season-3/farsi_persian/713369"
                    },
                    new SubtitlesResponse.Subtitles()
                    {
                        subtitleId = 739744,
                        language = "farsi_persian",
                        release_info = "Game.of.Thrones.S3E09.All.HDTV",
                        commentary = "The Rains of Castamere",
                        uploader_displayname = "lvlr",
                        link = "game-of-thrones-season-3/farsi_persian/739744"
                    }
                ]
            };

            var searchResponseStream = new MemoryStream();
            var searchHttpResponse = new HttpResponseInfo()
            {
                Content = searchResponseStream,
                ContentLength = 1
            };
            
            var subtitlesResponseStream = new MemoryStream();
            var subtitlesHttpResponse = new HttpResponseInfo()
            {
                Content = subtitlesResponseStream,
                ContentLength = 1
            };

            _mockHttpClient.Setup(c => c.Post(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(searchHttpResponse);

            _mockHttpClient.Setup(c => c.GetResponse(It.IsAny<HttpRequestOptions>()))
                .ReturnsAsync(subtitlesHttpResponse);

            _mockJsonSerializer.Setup(s => s.DeserializeFromStream<SearchResponse>(It.IsAny<Stream>()))
                .Returns(searchResponse);

            _mockJsonSerializer.Setup(s => s.DeserializeFromStream<SubtitlesResponse>(It.IsAny<Stream>()))
                .Returns(subtitlesResponse);

            _mockJsonSerializer.Setup(s => s.SerializeToString(It.IsAny<object>()))
                .Returns("{}");

            // Act
            var result = await _movieProvider.SearchSeries(title, lang, movieId, season, episode, cancellationToken);

            // Assert - Should only return S03E02, not S3E09
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("game-of-thrones-season-3|farsi_persian|713369", result[0].Id);
            Assert.Equal("S03E02", result[0].Name);
            Assert.Equal("mrjp_subtrans", result[0].Author);
        }
    }
}
