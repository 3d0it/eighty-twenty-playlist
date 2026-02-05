using System.Reflection;
using EightyTwentyPlaylist.Tool.Models;
using EightyTwentyPlaylist.Tool.Services;
using EightyTwentyPlaylist.Tool.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EightyTwentyPlaylist.Tool.Tests
{
    /// <summary>
    /// Tests for the PlaylistApp class, focusing on song generation from GeminiService.
    /// </summary>
    public class PlaylistAppTests
    {
        [Fact]
        public async Task GenerateSongsAsync_ReturnsSongs_WhenGeminiReturnsValidResponseAsync()
        {
            // Arrange
            var geminiServiceMock = new Mock<IGeminiService>();
            geminiServiceMock.Setup(g => g.GetPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("prompt");
            geminiServiceMock.Setup(g => g.SendPromptAsync(It.IsAny<string>()))
                .ReturnsAsync("Artist1, Song1;Artist2, Song2;");

            var spotifyServiceMock = new Mock<ISpotifyService>();
            Mock<IConfiguration> configMock = new Mock<IConfiguration>();
            Mock<ILogger<PlaylistApp>> loggerMock = new Mock<ILogger<PlaylistApp>>();
            var songParserMock = new Mock<ISongParser>();
            songParserMock.Setup(p => p.ExtractSongs(It.IsAny<string>()))
                .Returns((string s) => new List<Song> { new Song("Song1", "Artist1"), new Song("Song2", "Artist2") });

            PlaylistApp app = new PlaylistApp(
                geminiServiceMock.Object,
                spotifyServiceMock.Object,
                songParserMock.Object,
                configMock.Object,
                loggerMock.Object);
            PlaylistRequest request = new PlaylistRequest { Description = "desc", Genres = "genres", Duration = "60" };

            // Act
            MethodInfo? method = typeof(PlaylistApp).GetMethod("GenerateSongsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method); // Ensure method is found to avoid CS8602
            Task<List<Song>>? task = (Task<List<Song>>?)method.Invoke(app, new object[] { request });
            Assert.NotNull(task); // Ensure task is not null to avoid CS8602
            List<Song> result = await task;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Artist1", result[0].Artist);
            Assert.Equal("Song1", result[0].Title);
        }

        [Theory]
        [InlineData("My Playlist", true)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData("A", true)]
        [InlineData("A very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very long title", false)]
        [InlineData("Valid!@#", true)]
        [InlineData("Invalid\u0001", false)]
        public void IsValidSpotifyPlaylistTitle_Works(string title, bool expected)
        {
            // Arrange
            var method = typeof(EightyTwentyPlaylist.Tool.PlaylistApp).GetMethod("IsValidSpotifyPlaylistTitle", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            object?[] parameters = new object?[] { title, null };

            // Act
            bool result = (bool)method.Invoke(null, parameters)!;

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
