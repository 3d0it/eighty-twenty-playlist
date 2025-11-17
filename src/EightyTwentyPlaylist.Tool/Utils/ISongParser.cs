using System.Collections.Generic;
using EightyTwentyPlaylist.Tool.Models;

namespace EightyTwentyPlaylist.Tool.Utils
{
    /// <summary>
    /// Defines the contract for a song parser utility.
    /// </summary>
    public interface ISongParser
    {
        /// <summary>
        /// Extracts a collection of <see cref="Song"/> objects from a formatted string response.
        /// </summary>
        /// <param name="response">The string containing song entries.</param>
        /// <returns>An enumerable of <see cref="Song"/> objects parsed from the response.</returns>
        IEnumerable<Song> ExtractSongs(string response);
    }
}
