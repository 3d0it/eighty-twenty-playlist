namespace EightyTwentyPlaylist.Tool.Models
{
    /// <summary>
    /// Represents a song with a title and artist.
    /// </summary>
    /// <param name="Title">The title of the song.</param>
    /// <param name="Artist">The artist of the song.</param>
    public record Song(string Title, string Artist);
}
