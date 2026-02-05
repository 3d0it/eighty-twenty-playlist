using EightyTwentyPlaylist.Tool.Services;
using Microsoft.Extensions.Configuration;
using Moq;
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
        public async Task SendPromptAsync_ReturnsExpectedText_WhenAdapterReturnsTextAsync()
        {
            // Arrange
            Mock<IGeminiClientAdapter> adapterMock = new Mock<IGeminiClientAdapter>();
            adapterMock.Setup(a => a.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("expected");

            Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Gemini:ApiKey"]).Returns("dummy-key");
            mockConfig.Setup(c => c["Gemini:ApiEndpoint"]).Returns("dummy-endpoint");

            using HttpClient httpClient = new HttpClient();
            using GeminiService gemini = new GeminiService(httpClient, mockConfig.Object, adapterMock.Object);

            // Act
            string result = await gemini.SendPromptAsync("prompt");

            // Assert
            Assert.Equal("expected", result);
        }

        /// <summary>
        /// Verifies that SendPromptAsync returns an error message when Gemini responds with malformed JSON.
        /// </summary>
        [Fact]
        public async Task SendPromptAsync_ReturnsError_WhenAdapterReturnsNullAsync()
        {
            // Arrange
            Mock<IGeminiClientAdapter> adapterMock = new Mock<IGeminiClientAdapter>();
            adapterMock.Setup(a => a.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((string?)null);

            Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Gemini:ApiKey"]).Returns("dummy-key");
            mockConfig.Setup(c => c["Gemini:ApiEndpoint"]).Returns("dummy-endpoint");

            using HttpClient httpClient = new HttpClient();
            using GeminiService gemini = new GeminiService(httpClient, mockConfig.Object, adapterMock.Object);

            // Act
            string result = await gemini.SendPromptAsync("prompt");

            // Assert
            Assert.Contains("Error", result);
        }

        /// <summary>
        /// Verifies that GetPrompt returns a prompt string containing the provided values.
        /// </summary>
        [Fact]
        public void GetPrompt_ReturnsPromptWithCorrectValues()
        {
            Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Gemini:ApiKey"]).Returns("dummy-key");
            mockConfig.Setup(c => c["Gemini:ApiEndpoint"]).Returns("dummy-endpoint");

            using HttpClient httpClient = new HttpClient();
            Mock<IGeminiClientAdapter> adapterMock = new Mock<IGeminiClientAdapter>();
            using GeminiService gemini = new GeminiService(httpClient, mockConfig.Object, adapterMock.Object);

            string desc = "desc";
            string genres = "rock";
            string duration = "60min";
            string prompt = gemini.GetPrompt(desc, genres, duration);

            Assert.Contains(desc, prompt);
            Assert.Contains(genres, prompt);
            Assert.Contains(duration, prompt);
            Assert.Contains("create a running training playlist", prompt);
        }
    }
}
