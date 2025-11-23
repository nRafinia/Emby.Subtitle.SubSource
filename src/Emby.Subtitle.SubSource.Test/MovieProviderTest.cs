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
    public class MovieProviderTest
    {
        private readonly Mock<IHttpClient> _mockHttpClient;
        private readonly Mock<IApplicationHost> _mockAppHost;
        private readonly Mock<IJsonSerializer> _mockJsonSerializer;
        private readonly MovieProvider _movieProvider;

        public MovieProviderTest()
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
        public async Task SearchMovie_WithValidTitle_ReturnsSubtitles()
        {
            // Arrange
            var title = "Superman";
            var year = 2025;
            var lang = "farsi_persian";
            var movieId = "";
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = true,
                results =
                [
                    new SearchResponse.Results
                    {
                        movieId = 147401,
                        title = "Superman",
                        type = "movie",
                        link = "/subtitles/superman-2025",
                        releaseYear = 2025,
                        poster = "https://cdn.subsource.net/posters/147401/26b297f4ef4b803b8a6a171261f3edf6-small.jpg",
                        rating = null,
                        cast = [],
                        genres = []
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
                        subtitleId = 10113419,
                        language = "farsi_persian",
                        release_type = "Trailer",
                        release_info = "Superman.Official.Trailer.Fa",
                        createdAt = "2025-05-14T16:40:12.304Z",
                        hearing_impaired = 0,
                        commentary = "🔵 فرزاد یاقوتی | @DCReporter 🔵",
                        rating = "good",
                        uploader_id = 1337457,
                        uploader_displayname = "rhymeofapoem",
                        uploader_badges = [],
                        link = "superman-2025/farsi_persian/10113419",
                        production_type = "translated",
                        last_subtitle = false
                    },
                    new SubtitlesResponse.Subtitles()
                    {
                        subtitleId = 10100690,
                        language = "farsi_persian",
                        release_type = "Trailer",
                        release_info = "Superman.2025.Official.Sneak.Peak",
                        createdAt = "2025-04-05T09:18:20.206Z",
                        hearing_impaired = 0,
                        commentary = "🔵 فرزاد یاقوتی | @DCReporter 🔵",
                        rating = "good",
                        uploader_id = 1337457,
                        uploader_displayname = "rhymeofapoem",
                        uploader_badges = [],
                        link = "superman-2025/farsi_persian/10100690",
                        production_type = "translated",
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
            var result = await _movieProvider.SearchMovie(title, year, lang, movieId, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("superman-2025|farsi_persian|10113419", result[0].Id);
            Assert.Equal("Superman.Official.Trailer.Fa", result[0].Name);
            Assert.Equal("rhymeofapoem", result[0].Author);
            Assert.Equal("🔵 فرزاد یاقوتی | @DCReporter 🔵", result[0].Comment);
            Assert.Equal("srt", result[0].Format);
        }

        [Fact]
        public async Task SearchMovie_WithMovieId_ReturnsSubtitles()
        {
            // Arrange
            var title = "Superman";
            var year = 2025;
            var lang = "farsi_persian";
            var movieId = "tt5950044";
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = true,
                results =
                [
                    new SearchResponse.Results
                    {
                        movieId = 147401,
                        title = "Superman",
                        type = "movie",
                        link = "/subtitles/superman-2025",
                        releaseYear = 2025
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
                        subtitleId = 10113419,
                        language = "farsi_persian",
                        release_info = "Superman.Official.Trailer.Fa",
                        commentary = "Test Caption",
                        uploader_displayname = "testuser",
                        link = "superman-2025/farsi_persian/10113419"
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

            // Verify that movieId is used in search query instead of title
            _mockJsonSerializer.Setup(s => s.SerializeToString(It.Is<object>(o => 
                o.ToString().Contains(movieId))))
                .Returns("{}");

            // Act
            var result = await _movieProvider.SearchMovie(title, year, lang, movieId, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("superman-2025|farsi_persian|10113419", result[0].Id);
        }

        [Fact]
        public async Task SearchMovie_NoMovieFound_ReturnsEmptyList()
        {
            // Arrange
            var title = "NonExistentMovie";
            var year = 2025;
            var lang = "farsi_persian";
            var movieId = "";
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = false,
                results = new SearchResponse.Results[0],
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
            var result = await _movieProvider.SearchMovie(title, year, lang, movieId, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchMovie_EmptySearchResponse_ReturnsEmptyList()
        {
            // Arrange
            var title = "Superman";
            var year = 2025;
            var lang = "farsi_persian";
            var movieId = "";
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
            var result = await _movieProvider.SearchMovie(title, year, lang, movieId, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchMovie_EmptySubtitlesResponse_ReturnsEmptyList()
        {
            // Arrange
            var title = "Superman";
            var year = 2025;
            var lang = "farsi_persian";
            var movieId = "";
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = true,
                results = new[]
                {
                    new SearchResponse.Results
                    {
                        movieId = 147401,
                        title = "Superman",
                        type = "movie",
                        link = "/subtitles/superman-2025",
                        releaseYear = 2025
                    }
                },
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
            var result = await _movieProvider.SearchMovie(title, year, lang, movieId, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchMovie_WithMultipleResults_FiltersCorrectly()
        {
            // Arrange
            var title = "Superman";
            var year = 2025;
            var lang = "farsi_persian";
            var movieId = "";
            var cancellationToken = CancellationToken.None;

            var searchResponse = new SearchResponse
            {
                success = true,
                results = new[]
                {
                    new SearchResponse.Results
                    {
                        movieId = 147401,
                        title = "Superman",
                        type = "movie",
                        link = "/subtitles/superman-2025",
                        releaseYear = 2025
                    },
                    new SearchResponse.Results
                    {
                        movieId = 147402,
                        title = "Superman",
                        type = "tvseries",
                        link = "/subtitles/superman-series",
                        releaseYear = 2025
                    }
                },
                count = 2,
                source = "imdb_direct"
            };

            var subtitlesResponse = new SubtitlesResponse
            {
                data =
                [
                    new SubtitlesResponse.Subtitles()
                    {
                        subtitleId = 10113419,
                        language = "farsi_persian",
                        release_info = "Superman.Official.Trailer.Fa",
                        commentary = "Test Caption",
                        uploader_displayname = "testuser",
                        link = "superman-2025/farsi_persian/10113419"
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
            var result = await _movieProvider.SearchMovie(title, year, lang, movieId, cancellationToken);

            // Assert - Should select the movie type, not tvseries
            Assert.NotNull(result);
            Assert.Single(result);
        }
    }
}
