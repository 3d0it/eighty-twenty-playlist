using System.Net;
using EightyTwentyPlaylist.Tool.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace EightyTwentyPlaylist.Tool.Tests
{
    /// <summary>
    /// Unit tests for the GeminiService implementation of IGeminiService.
    /// </summary>
    public class GeminiServiceTests
    {
        /// <summary>
        /// Verifies that SendPromptAsync returns the expected text when Gemini responds with valid JSON.
        /// </summary>
        [Fact]
        public async Task SendPromptAsync_ReturnsExpectedText_WhenValidJsonAsync()
        {
            // Arrange
            var validJson = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"expected\"}]}}]}";
            var gemini = CreateGeminiService(validJson);

            // Act
            var result = await gemini.SendPromptAsync("prompt");

            // Assert
            Assert.Equal("expected", result);
        }

        /// <summary>
        /// Verifies that SendPromptAsync returns an error message when Gemini responds with malformed JSON.
        /// </summary>
        [Fact]
        public async Task SendPromptAsync_ReturnsError_WhenMalformedJsonAsync()
        {
            // Arrange
            var malformedJson = "{}";
            var gemini = CreateGeminiService(malformedJson);

            // Act
            var result = await gemini.SendPromptAsync("prompt");

            // Assert
            Assert.Contains("Error", result);
        }

        /// <summary>
        /// Verifies that GetPrompt returns a prompt string containing the provided values.
        /// </summary>
        [Fact]
        public void GetPrompt_ReturnsPromptWithCorrectValues()
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Gemini:ApiKey"]).Returns("key");
            mockConfig.Setup(c => c["Gemini:ApiEndpoint"]).Returns("endpoint");
            using var httpClient = new HttpClient();
            using var gemini = new GeminiService(httpClient, mockConfig.Object);
            string desc = "desc";
            string genres = "rock";
            string duration = "60min";
            string prompt = gemini.GetPrompt(desc, genres, duration);

            Assert.Contains(desc, prompt);
            Assert.Contains(genres, prompt);
            Assert.Contains(duration, prompt);
            Assert.Contains("create a running training playlist", prompt);
        }

        /// <summary>
        /// Verifies that SendPromptAsync returns the correct text for another valid JSON response.
        /// </summary>
        [Fact]
        public async Task SendPromptAsync_ReturnsTextWhenValidJsonAsync()
        {
            // Arrange
            var validJson = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"result\"}]}}]}";
            var gemini = CreateGeminiService(validJson);

            // Act
            var result = await gemini.SendPromptAsync("prompt");

            // Assert
            Assert.Equal("result", result);
        }

        /// <summary>
        /// Verifies that SendPromptAsync returns an error message for another malformed JSON response.
        /// </summary>
        [Fact]
        public async Task SendPromptAsync_ReturnsErrorWhenMalformedJsonAsync()
        {
            // Arrange
            var malformedJson = "{}";
            var gemini = CreateGeminiService(malformedJson);

            // Act
            var result = await gemini.SendPromptAsync("prompt");

            // Assert
            Assert.Contains("Error", result);
        }

        /// <summary>
        /// Helper to create a GeminiService with a mocked HTTP response and configuration.
        /// </summary>
        /// <param name="responseJson">The JSON string to return in the HTTP response.</param>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <returns>An IGeminiService instance for testing.</returns>
        private static IGeminiService CreateGeminiService(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            Mock<HttpMessageHandler> handlerMock = new Mock<HttpMessageHandler>();
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
                    return response;
                });

            IGeminiService CreateService()
            {
                HttpClient? httpClient = null;
                try
                {
                    httpClient = new HttpClient(handlerMock.Object)
                    {
                        BaseAddress = new Uri("https://dummy-base/")
                    };

                    Mock<IConfiguration> configMock = new Mock<IConfiguration>();
                    configMock.Setup(c => c["Gemini:ApiKey"]).Returns("dummy-key");
                    configMock.Setup(c => c["Gemini:ApiEndpoint"]).Returns("dummy-endpoint");

                    return new GeminiServiceWithClient(httpClient, configMock.Object);
                }
                catch
                {
                    if (httpClient != null)
                        httpClient.Dispose();
                    throw;
                }
            }

            return CreateService();
        }

        /// <summary>
        /// Wrapper for GeminiService to ensure proper disposal in tests.
        /// </summary>
        private class GeminiServiceWithClient : IGeminiService, IDisposable
        {
            private readonly GeminiService _service;
            private readonly HttpClient _client;

            public GeminiServiceWithClient(HttpClient client, IConfiguration config)
            {
                _client = client;
                _service = new GeminiService(client, config);
            }

            public string GetPrompt(string description, string genres, string duration) =>
                _service.GetPrompt(description, genres, duration);

            public Task<string> SendPromptAsync(string prompt) =>
                _service.SendPromptAsync(prompt);

            public void Dispose()
            {
                _service.Dispose();
                _client.Dispose();
            }
        }
    }
}
