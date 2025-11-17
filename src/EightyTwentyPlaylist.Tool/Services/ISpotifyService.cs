using System.Collections.Generic;
using System.Threading.Tasks;

namespace EightyTwentyPlaylist.Tool.Services
{
    /// <summary>
    /// Defines the contract for interacting with the Spotify Web API for playlist and track management.
    /// </summary>
    public interface ISpotifyService
    {
        /// <summary>
        /// Gets the Spotify authorization URL for user login and consent.
        /// </summary>
        /// <param name="scopes">A space-separated list of Spotify authorization scopes.</param>
        /// <returns>The authorization URL.</returns>
        string GetAuthorizationUrl(string scopes);

        /// <summary>
        /// Exchanges an authorization code for an access token.
        /// </summary>
        /// <param name="code">The authorization code received from Spotify.</param>
        /// <returns>The access token, or null if unsuccessful.</returns>
        Task<string?> ExchangeCodeForTokenAsync(string code);

        /// <summary>
        /// Gets a client credentials access token for the Spotify API.
        /// </summary>
        /// <returns>The access token, or null if unsuccessful.</returns>
        Task<string?> GetClientCredentialsAccessTokenAsync();

        /// <summary>
        /// Searches for a Spotify track by name and artist.
        /// </summary>
        /// <param name="trackName">The name of the track.</param>
        /// <param name="artistName">The name of the artist.</param>
        /// <param name="accessToken">A valid Spotify access token.</param>
        /// <returns>The track ID, or null if not found.</returns>
        Task<string?> SearchSpotifyTrackAsync(string trackName, string artistName, string accessToken);

        /// <summary>
        /// Gets the current user's Spotify user ID.
        /// </summary>
        /// <param name="accessToken">A valid Spotify access token.</param>
        /// <returns>The user ID, or null if unsuccessful.</returns>
        Task<string?> GetCurrentUserIdAsync(string accessToken);

        /// <summary>
        /// Finds a playlist by name for the current user.
        /// </summary>
        /// <param name="playlistName">The name of the playlist.</param>
        /// <param name="accessToken">A valid Spotify access token.</param>
        /// <returns>The playlist ID, or null if not found.</returns>
        Task<string?> FindPlaylistByNameAsync(string playlistName, string accessToken);

        /// <summary>
        /// Creates a new playlist for the user.
        /// </summary>
        /// <param name="playlistName">The name of the new playlist.</param>
        /// <param name="accessToken">A valid Spotify access token.</param>
        /// <param name="userId">The Spotify user ID.</param>
        /// <returns>The new playlist ID, or null if unsuccessful.</returns>
        Task<string?> CreatePlaylistAsync(string playlistName, string accessToken, string userId);

        /// <summary>
        /// Gets all track URIs from a playlist.
        /// </summary>
        /// <param name="playlistId">The playlist ID.</param>
        /// <param name="accessToken">A valid Spotify access token.</param>
        /// <returns>A list of track URIs.</returns>
        Task<List<string>> GetPlaylistTrackUrisAsync(string playlistId, string accessToken);

        /// <summary>
        /// Removes tracks from a playlist.
        /// </summary>
        /// <param name="playlistId">The playlist ID.</param>
        /// <param name="trackUrisToRemove">A list of track URIs to remove.</param>
        /// <param name="accessToken">A valid Spotify access token.</param>
        Task RemoveTracksFromPlaylistAsync(string playlistId, List<string> trackUrisToRemove, string accessToken);

        /// <summary>
        /// Adds tracks to a playlist.
        /// </summary>
        /// <param name="playlistId">The playlist ID.</param>
        /// <param name="trackIdsToAdd">A list of track IDs to add.</param>
        /// <param name="accessToken">A valid Spotify access token.</param>
        Task AddTracksToPlaylistAsync(string playlistId, List<string> trackIdsToAdd, string accessToken);

        /// <summary>
        /// Manages a playlist: creates or updates it with the provided tracks.
        /// </summary>
        /// <param name="playlistName">The name of the playlist.</param>
        /// <param name="trackIdsToAdd">A list of track IDs to add.</param>
        /// <param name="accessToken">A valid Spotify access token.</param>
        Task ManagePlaylistAsync(string playlistName, List<string> trackIdsToAdd, string accessToken);
    }
}
