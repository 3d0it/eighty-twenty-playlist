namespace EightyTwentyPlaylist.Tool.Models
{
    /// <summary>
    /// Represents a request for generating a playlist, including duration, description, preferred genres, and playlist title.
    /// </summary>
    public class PlaylistRequest
    {
        /// <summary>
        /// Gets or sets the total duration of the playlist (e.g., "60 minutes").
        /// </summary>
        public string Duration { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the training session (e.g., "60 minutes zone2").
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the preferred music genres (comma separated, e.g., "Rock, metal, blues").
        /// </summary>
        public string Genres { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the playlist title (optional).
        /// </summary>
        public string? PlaylistTitle { get; set; }
    }
}
