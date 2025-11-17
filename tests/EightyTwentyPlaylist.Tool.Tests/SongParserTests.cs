using EightyTwentyPlaylist.Tool.Models;
using EightyTwentyPlaylist.Tool.Utils;
using Xunit;

namespace EightyTwentyPlaylist.Tool.Tests
{
    /// <summary>
    /// Unit tests for the SongParser utility class, which parses song information from formatted text responses.
    /// </summary>
    public class SongParserTests
    {
        private readonly SongParser _parser = new SongParser();

        /// <summary>
        /// Verifies that ExtractSongs returns the correct Song objects when given valid input.
        /// </summary>
        [Fact]
        public void ExtractSongs_ReturnsSongs_WhenValidInput()
        {
            // Arrange
            var input = "Artist1, Song1;Artist2, Song2;";

            // Act
            var result = _parser.ExtractSongs(input);

            // Assert
            var songs = new List<Song>(result);
            Assert.Equal(2, songs.Count);
            Assert.Equal("Artist1", songs[0].Artist);
            Assert.Equal("Song1", songs[0].Title);
            Assert.Equal("Artist2", songs[1].Artist);
            Assert.Equal("Song2", songs[1].Title);
        }

        /// <summary>
        /// Verifies that ExtractSongs returns an empty collection when no valid songs are present.
        /// </summary>
        [Fact]
        public void ExtractSongs_ReturnsEmpty_WhenNoValidSongs()
        {
            // Arrange
            var input = "No valid song lines here";

            // Act
            var result = _parser.ExtractSongs(input);

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Verifies that ExtractSongs ignores malformed entries and only returns valid songs.
        /// </summary>
        [Fact]
        public void ExtractSongs_IgnoresMalformedEntries()
        {
            // Arrange
            var input = "Artist1, Song1;MalformedEntry;Artist2, Song2;";

            // Act
            var result = _parser.ExtractSongs(input);

            // Assert
            var songs = new List<Song>(result);
            Assert.Equal(2, songs.Count);
            Assert.Equal("Artist1", songs[0].Artist);
            Assert.Equal("Song1", songs[0].Title);
            Assert.Equal("Artist2", songs[1].Artist);
            Assert.Equal("Song2", songs[1].Title);
        }

        /// <summary>
        /// Verifies that ExtractSongs returns an empty collection when given an empty string.
        /// </summary>
        [Fact]
        public void ExtractSongs_Handles_EmptyString()
        {
            // Arrange
            var input = string.Empty;

            // Act
            var result = _parser.ExtractSongs(input);

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Verifies that ExtractSongs returns an empty collection when given a whitespace-only string.
        /// </summary>
        [Fact]
        public void ExtractSongs_Handles_WhitespaceOnly()
        {
            // Arrange
            var input = " ";

            // Act
            var result = _parser.ExtractSongs(input);

            // Assert
            Assert.Empty(result);
        }
    }
}
