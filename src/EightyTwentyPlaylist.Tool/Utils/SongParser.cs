using System.Collections.Generic;
using System.Text.RegularExpressions;
using EightyTwentyPlaylist.Tool.Models;

namespace EightyTwentyPlaylist.Tool.Utils
{
    /// <summary>
    /// Provides utilities for parsing song information from text responses.
    /// </summary>
    public class SongParser : ISongParser
    {
        /// <summary>
        /// Extracts a collection of <see cref="Song"/> objects from a formatted string response.
        /// Expected format: "Artist, SongTitle;Artist2, SongTitle2;..."
        /// Ignores malformed entries.
        /// </summary>
        /// <param name="response">The string containing song entries.</param>
        /// <returns>An enumerable of <see cref="Song"/> objects parsed from the response.</returns>
        public IEnumerable<Song> ExtractSongs(string response)
        {
            var songs = new List<Song>();
            if (string.IsNullOrWhiteSpace(response))
                return songs;
            // Only match entries with exactly one comma and no extra semicolons or commas in either field
            string pattern = @"([^,;]+),\s*([^,;]+);";
            var regex = new Regex(pattern, RegexOptions.Multiline);
            var matches = regex.Matches(response);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3 && match.Groups[1].Success && match.Groups[2].Success)
                {
                    string artist = match.Groups[1].Value.Trim();
                    string songTitle = match.Groups[2].Value.Trim();
                    songs.Add(new Song(songTitle, artist));
                }
            }
            return songs;
        }
    }
}
