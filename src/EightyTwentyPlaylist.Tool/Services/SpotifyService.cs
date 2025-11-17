using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EightyTwentyPlaylist.Tool.Services
{
    /// <summary>
    /// Service for interacting with the Spotify Web API.
    /// </summary>
    public class SpotifyService : ISpotifyService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tokenEndpoint;
        private readonly string _searchEndpoint;
        private readonly string _meEndpoint;
        private readonly string _playlistsEndpoint;
        private readonly string _playlistTracksEndpoint;
        private readonly string _authEndpoint;
        private readonly string _redirectUri;
        private readonly ILogger<SpotifyService> _logger;
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for requests.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if required configuration is missing.</exception>
        public SpotifyService(HttpClient httpClient, IConfiguration configuration, ILogger<SpotifyService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientId = configuration["Spotify:ClientId"] ?? throw new ArgumentNullException("Spotify:ClientId not found.");
            _clientSecret = configuration["Spotify:ClientSecret"] ?? throw new ArgumentNullException("Spotify:ClientSecret not found.");
            _tokenEndpoint = configuration["Spotify:TokenEndpoint"] ?? throw new ArgumentNullException("Spotify:TokenEndpoint not found.");
            _searchEndpoint = configuration["Spotify:SearchEndpoint"] ?? throw new ArgumentNullException("Spotify:SearchEndpoint not found.");
            _meEndpoint = configuration["Spotify:MeEndpoint"] ?? throw new ArgumentNullException("Spotify:MeEndpoint not found.");
            _playlistsEndpoint = configuration["Spotify:PlaylistsEndpoint"] ?? throw new ArgumentNullException("Spotify:PlaylistsEndpoint not found.");
            _playlistTracksEndpoint = configuration["Spotify:PlaylistTracksEndpoint"] ?? throw new ArgumentNullException("Spotify:PlaylistTracksEndpoint not found.");
            _authEndpoint = configuration["Spotify:AuthEndpoint"] ?? throw new ArgumentNullException("Spotify:AuthEndpoint not found.");
            _redirectUri = configuration["Spotify:RedirectUri"] ?? throw new ArgumentNullException("Spotify:RedirectUri not found.");
        }

        /// <summary>
        /// Gets the Spotify authorization URL for user login and consent.
        /// </summary>
        public string GetAuthorizationUrl(string scopes)
        {
            return $"{_authEndpoint}?response_type=code&client_id={_clientId}&scope={Uri.EscapeDataString(scopes)}&redirect_uri={Uri.EscapeDataString(_redirectUri)}";
        }

        /// <summary>
        /// Exchanges an authorization code for an access token.
        /// </summary>
        public async Task<string?> ExchangeCodeForTokenAsync(string code)
        {
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", _redirectUri)
            });

            HttpResponseMessage response = await _httpClient.PostAsync(_tokenEndpoint, requestBody);

            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonDocument jsonResponse = JsonDocument.Parse(responseBody);
            if (jsonResponse.RootElement.TryGetProperty("access_token", out JsonElement accessTokenElement))
            {
                return accessTokenElement.GetString();
            }
            return null;
        }

        /// <summary>
        /// Gets a client credentials access token for the Spotify API.
        /// </summary>
        public async Task<string?> GetClientCredentialsAccessTokenAsync()
        {
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            HttpResponseMessage response = await _httpClient.PostAsync(_tokenEndpoint, requestBody);

            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonDocument jsonResponse = JsonDocument.Parse(responseBody);
            if (jsonResponse.RootElement.TryGetProperty("access_token", out JsonElement accessTokenElement))
            {
                return accessTokenElement.GetString();
            }
            return null;
        }

        /// <summary>
        /// Searches for a Spotify track by name and artist.
        /// </summary>
        public async Task<string?> SearchSpotifyTrackAsync(string trackName, string artistName, string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            string query = Uri.EscapeDataString($"track:{trackName} artist:{artistName}");
            string url = $"{_searchEndpoint}?q={query}&type=track&limit=1";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonDocument jsonResponse = JsonDocument.Parse(responseBody);
            JsonElement root = jsonResponse.RootElement;

            if (root.TryGetProperty("tracks", out JsonElement tracks) &&
                tracks.TryGetProperty("items", out JsonElement items))
            {
                foreach (JsonElement item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out JsonElement nameElement) &&
                        item.TryGetProperty("artists", out JsonElement artistsElement) &&
                        item.TryGetProperty("id", out JsonElement idElement))
                    {
                        string foundTrackName = nameElement.GetString() ?? string.Empty;
                        var firstArtist = artistsElement.EnumerateArray().FirstOrDefault();
                        string foundArtistName = firstArtist.ValueKind != JsonValueKind.Undefined
                                                 && firstArtist.TryGetProperty("name", out JsonElement artistNameElement) ? (artistNameElement.GetString() ?? string.Empty) : string.Empty;

                        if (!string.IsNullOrEmpty(foundTrackName) && !string.IsNullOrEmpty(foundArtistName))
                            return idElement.GetString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the current user's Spotify user ID.
        /// </summary>
        public async Task<string?> GetCurrentUserIdAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await _httpClient.GetAsync(_meEndpoint);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonDocument jsonResponse = JsonDocument.Parse(responseBody);
            if (jsonResponse.RootElement.TryGetProperty("id", out JsonElement idElement))
            {
                return idElement.GetString();
            }
            return null;
        }

        /// <summary>
        /// Finds a playlist by name for the current user.
        /// </summary>
        public async Task<string?> FindPlaylistByNameAsync(string playlistName, string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            string url = _meEndpoint + "/playlists";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonDocument jsonResponse = JsonDocument.Parse(responseBody);
            if (jsonResponse.RootElement.TryGetProperty("items", out JsonElement items))
            {
                foreach (JsonElement item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out JsonElement nameElement) &&
                        item.TryGetProperty("id", out JsonElement idElement))
                    {
                        if (nameElement.GetString()?.Equals(playlistName, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return idElement.GetString();
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new playlist for the user.
        /// </summary>
        public async Task<string?> CreatePlaylistAsync(string playlistName, string accessToken, string userId)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            string url = string.Format(_playlistsEndpoint, userId);

            var requestBody = JsonSerializer.Serialize(new
            {
                name = playlistName,
                @public = false,
                description = "Playlist generated by Gemini for running training."
            });

            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonDocument jsonResponse = JsonDocument.Parse(responseBody);
            if (jsonResponse.RootElement.TryGetProperty("id", out JsonElement idElement))
            {
                return idElement.GetString();
            }
            return null;
        }

        /// <summary>
        /// Gets all track URIs from a playlist.
        /// </summary>
        public async Task<List<string>> GetPlaylistTrackUrisAsync(string playlistId, string accessToken)
        {
            List<string> trackUris = new List<string>();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            string url = string.Format(_playlistTracksEndpoint, playlistId);

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonDocument jsonResponse = JsonDocument.Parse(responseBody);
            if (jsonResponse.RootElement.TryGetProperty("items", out JsonElement items))
            {
                foreach (JsonElement item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("track", out JsonElement trackElement) &&
                        trackElement.TryGetProperty("uri", out JsonElement uriElement))
                    {
                        trackUris.Add(uriElement.GetString() ?? string.Empty);
                    }
                }
            }
            return trackUris;
        }

        /// <summary>
        /// Removes tracks from a playlist.
        /// </summary>
        public async Task RemoveTracksFromPlaylistAsync(string playlistId, List<string> trackUrisToRemove, string accessToken)
        {
            if (!trackUrisToRemove.Any())
                return;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            string url = string.Format(_playlistTracksEndpoint, playlistId);

            var tracks = trackUrisToRemove.Select(uri => new { uri = uri }).ToList();
            var requestBody = JsonSerializer.Serialize(new { tracks = tracks });

            using var request = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Adds tracks to a playlist.
        /// </summary>
        public async Task AddTracksToPlaylistAsync(string playlistId, List<string> trackIdsToAdd, string accessToken)
        {
            if (!trackIdsToAdd.Any())
                return;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            string url = string.Format(_playlistTracksEndpoint, playlistId);

            List<string> trackUris = trackIdsToAdd.Select(id => $"spotify:track:{id}").ToList();

            var requestBody = JsonSerializer.Serialize(new { uris = trackUris });
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Manages a playlist: creates or updates it with the provided tracks.
        /// </summary>
        public async Task ManagePlaylistAsync(string playlistName, List<string> trackIdsToAdd, string accessToken)
        {
            string? userId = await GetCurrentUserIdAsync(accessToken);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not retrieve current Spotify user ID.");
                throw new Exception("Unable to get Spotify user ID.");
            }

            string? playlistId = await FindPlaylistByNameAsync(playlistName, accessToken);

            if (string.IsNullOrEmpty(playlistId))
            {
                _logger.LogInformation("Playlist '{PlaylistName}' not found. Creating new playlist.", playlistName);
                playlistId = await CreatePlaylistAsync(playlistName, accessToken, userId);
                if (string.IsNullOrEmpty(playlistId))
                {
                    _logger.LogError("Failed to create playlist '{PlaylistName}'.", playlistName);
                    throw new Exception($"Failed to create playlist '{playlistName}'.");
                }
            }
            else
            {
                _logger.LogInformation("Playlist '{PlaylistName}' found. Clearing existing songs and adding new ones.", playlistName);

                List<string> existingTrackUris = await GetPlaylistTrackUrisAsync(playlistId, accessToken);
                if (existingTrackUris.Any())
                {
                    await RemoveTracksFromPlaylistAsync(playlistId, existingTrackUris, accessToken);
                    _logger.LogInformation("Removed {Count} existing tracks from playlist '{PlaylistName}'.", existingTrackUris.Count, playlistName);
                }
                else
                {
                    _logger.LogInformation("Playlist '{PlaylistName}' is already empty.", playlistName);
                }
            }

            if (trackIdsToAdd.Any())
            {
                await AddTracksToPlaylistAsync(playlistId, trackIdsToAdd, accessToken);
                _logger.LogInformation("Added {Count} tracks to playlist '{PlaylistName}'.", trackIdsToAdd.Count, playlistName);
            }
            else
            {
                _logger.LogWarning("No tracks provided to add to playlist '{PlaylistName}'.", playlistName);
            }
        }

        /// <summary>
        /// Disposes the SpotifyService and its resources.
        /// </summary>
        /// <param name="disposing">Whether called from Dispose().</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
