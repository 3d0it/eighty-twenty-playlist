using System.Net;
using EightyTwentyPlaylist.Tool.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace EightyTwentyPlaylist.Tool.Tests
{
    /// <summary>
    /// Unit tests for the SpotifyService implementation of ISpotifyService.
    /// </summary>
    public class SpotifyServiceTests
    {
        /// <summary>
        /// Verifies that SearchSpotifyTrackAsync returns null when the response is empty.
        /// </summary>
        [Fact]
        public async Task SearchSpotifyTrack_ReturnsNull_OnEmptyResponseAsync()
        {
            Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}")
                    };
                    return response;
                });
            using HttpClient httpClient = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("https://dummy-base/")
            };
            Mock<IConfiguration> config = new Mock<IConfiguration>();
            config.Setup(c => c[It.IsAny<string>()]).Returns("test");
            Mock<ILogger<SpotifyService>> logger = new Mock<ILogger<SpotifyService>>();
            using SpotifyService service = new SpotifyService(httpClient, config.Object, logger.Object);
            string? result = await service.SearchSpotifyTrackAsync("title", "artist", "token");
            Assert.Null(result);
        }

        /// <summary>
        /// Helper to create a SpotifyService with a mocked HTTP response and configuration.
        /// </summary>
        /// <param name="responseJson">The JSON string to return in the HTTP response.</param>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <returns>A SpotifyService instance for testing.</returns>
        private static SpotifyService CreateSpotifyService(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    var response = new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent(responseJson)
                    };
                    // The HttpClient will dispose the response, so we do not dispose it here.
                    return response;
                });

            // HttpClient is disposed by SpotifyService, do not dispose here.
#pragma warning disable CA2000
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://dummy-base/")
            };
#pragma warning restore CA2000

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Spotify:ClientId"]).Returns("dummy-client-id");
            configMock.Setup(c => c["Spotify:ClientSecret"]).Returns("dummy-client-secret");
            configMock.Setup(c => c["Spotify:TokenEndpoint"]).Returns("dummy-token-endpoint");
            configMock.Setup(c => c["Spotify:SearchEndpoint"]).Returns("dummy-search-endpoint");
            configMock.Setup(c => c["Spotify:MeEndpoint"]).Returns("dummy-me-endpoint");
            configMock.Setup(c => c["Spotify:PlaylistsEndpoint"]).Returns("dummy-playlists-endpoint");
            configMock.Setup(c => c["Spotify:PlaylistTracksEndpoint"]).Returns("dummy-playlist-tracks-endpoint");
            configMock.Setup(c => c["Spotify:AuthEndpoint"]).Returns("dummy-auth-endpoint");
            configMock.Setup(c => c["Spotify:RedirectUri"]).Returns("http://localhost:8888/SpotifyCallback");

            var loggerMock = new Mock<ILogger<SpotifyService>>();

            // Ensure HttpClient is disposed with SpotifyService
            return new SpotifyService(httpClient, configMock.Object, loggerMock.Object);
        }

        /// <summary>
        /// Provides test data for GetClientCredentialsAccessToken_WorksAsExpectedAsync.
        /// </summary>
        public static IEnumerable<object?[]> GetClientCredentialsAccessTokenData()
        {
            yield return new object[] { "{\"access_token\":\"abc123\"}", "abc123" };
            yield return new object?[] { "{}", null };
        }

        /// <summary>
        /// Verifies that GetClientCredentialsAccessTokenAsync returns the expected token or null.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetClientCredentialsAccessTokenData))]
        public async Task GetClientCredentialsAccessToken_WorksAsExpectedAsync(string json, string expected)
        {
            using var service = CreateSpotifyService(json);
            var token = await service.GetClientCredentialsAccessTokenAsync();
            Assert.Equal(expected, token);
        }

        /// <summary>
        /// Provides test data for ExchangeCodeForToken_WorksAsExpectedAsync.
        /// </summary>
        public static IEnumerable<object[]> ExchangeCodeForTokenData()
        {
            yield return new object[] { "{\"access_token\":\"xyz789\"}", "xyz789" };
        }

        /// <summary>
        /// Verifies that ExchangeCodeForTokenAsync returns the expected token.
        /// </summary>
        [Theory]
        [MemberData(nameof(ExchangeCodeForTokenData))]
        public async Task ExchangeCodeForToken_WorksAsExpectedAsync(string json, string expected)
        {
            using var service = CreateSpotifyService(json);
            var token = await service.ExchangeCodeForTokenAsync("code");
            Assert.Equal(expected, token);
        }

        /// <summary>
        /// Verifies that GetCurrentUserIdAsync returns the expected user ID.
        /// </summary>
        [Fact]
        public async Task GetCurrentUserIdReturnsIdAsync()
        {
            var json = "{\"id\":\"user123\"}";
            using var service = CreateSpotifyService(json);
            var id = await service.GetCurrentUserIdAsync("token");
            Assert.Equal("user123", id);
        }

        /// <summary>
        /// Verifies that FindPlaylistByNameAsync returns the expected playlist ID.
        /// </summary>
        [Fact]
        public async Task FindPlaylistByNameReturnsPlaylistIdAsync()
        {
            var json = "{\"items\":[{\"name\":\"MyPlaylist\",\"id\":\"plid\"}]}";
            using var service = CreateSpotifyService(json);
            var id = await service.FindPlaylistByNameAsync("MyPlaylist", "token");
            Assert.Equal("plid", id);
        }

        /// <summary>
        /// Verifies that FindPlaylistByNameAsync returns null when the playlist is not found.
        /// </summary>
        [Fact]
        public async Task FindPlaylistByNameReturnsNullWhenNotFoundAsync()
        {
            var json = "{\"items\":[]}";
            using var service = CreateSpotifyService(json);
            var id = await service.FindPlaylistByNameAsync("OtherPlaylist", "token");
            Assert.Null(id);
        }

        /// <summary>
        /// Verifies that GetPlaylistTrackUrisAsync returns the expected track URIs.
        /// </summary>
        [Fact]
        public async Task GetPlaylistTrackUrisReturnsUrisAsync()
        {
            var json = "{\"items\":[{\"track\":{\"uri\":\"uri1\"}},{\"track\":{\"uri\":\"uri2\"}}]}";
            using var service = CreateSpotifyService(json);
            var uris = await service.GetPlaylistTrackUrisAsync("plid", "token");
            Assert.Equal(new List<string> { "uri1", "uri2" }, uris);
        }

        /// <summary>
        /// Verifies that SearchSpotifyTrackAsync returns the expected track ID when found.
        /// </summary>
        [Fact]
        public async Task SearchSpotifyTrackAsync_ReturnsTrackId_WhenTrackFoundAsync()
        {
            // Arrange
            var json = "{\"tracks\":{\"items\":[{\"name\":\"Test Song\",\"artists\":[{\"name\":\"Test Artist\"}],\"id\":\"track123\"}]}}";
            using var service = CreateSpotifyService(json);

            // Act
            var result = await service.SearchSpotifyTrackAsync("Test Song", "Test Artist", "dummy-access-token");

            // Assert
            Assert.Equal("track123", result);
        }

        /// <summary>
        /// Verifies that SearchSpotifyTrackAsync returns null when the track is not found.
        /// </summary>
        [Fact]
        public async Task SearchSpotifyTrackAsync_ReturnsNull_WhenTrackNotFoundAsync()
        {
            // Arrange
            var json = "{\"tracks\":{\"items\":[]}}";
            using var service = CreateSpotifyService(json);

            // Act
            var result = await service.SearchSpotifyTrackAsync("Nonexistent Song", "Nonexistent Artist", "dummy-access-token");

            // Assert
            Assert.Null(result);
        }
    }
}
