using EightyTwentyPlaylist.Tool.Models;
using EightyTwentyPlaylist.Tool.Services;
using EightyTwentyPlaylist.Tool.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EightyTwentyPlaylist.Tool
{
    /// <summary>
    /// Main application logic for generating and managing Spotify playlists using AI and the Spotify API.
    /// </summary>
    public class PlaylistApp
    {
        private readonly IGeminiService _geminiService;
        private readonly ISpotifyService _spotifyService;
        private readonly ISongParser _songParser;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PlaylistApp> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistApp"/> class.
        /// </summary>
        /// <param name="geminiService">The Gemini AI service.</param>
        /// <param name="spotifyService">The Spotify API service.</param>
        /// <param name="songParser">The song parser utility.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger instance.</param>
        public PlaylistApp(
            IGeminiService geminiService,
            ISpotifyService spotifyService,
            ISongParser songParser,
            IConfiguration configuration,
            ILogger<PlaylistApp> logger)
        {
            _geminiService = geminiService;
            _spotifyService = spotifyService;
            _songParser = songParser;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Runs the playlist generation and management workflow.
        /// </summary>
        public async Task RunAsync()
        {
            AnsiConsole.MarkupLine("[bold yellow]EightyTwentyPlaylist Tool Started[/]");
            var playlistRequest = PromptUserForRequest();

            try
            {
                AnsiConsole.MarkupLine("[yellow]Generating song list...[/]");

                var generatedSongs = await GenerateSongsAsync(playlistRequest);
                if (!generatedSongs.Any())
                {
                    AnsiConsole.MarkupLine("[red]No songs could be extracted from Gemini response.[/]");
                    return;
                }
                DisplaySongs(generatedSongs);

                AnsiConsole.MarkupLine("[yellow]Searching for Spotify track IDs...[/]");

                var trackIds = await SearchSpotifyTracksAsync(generatedSongs);
                if (!trackIds.Any())
                {
                    AnsiConsole.MarkupLine("[red]No Spotify track IDs could be found for the generated songs.[/]");
                    return;
                }
                DisplayTrackIds(trackIds);

                var userAccessToken = await AuthorizeAndGetSpotifyTokenAsync();
                if (string.IsNullOrEmpty(userAccessToken))
                {
                    AnsiConsole.MarkupLine("[red]Failed to obtain user-authorized Spotify access token.[/]");
                    return;
                }

                await _spotifyService.ManagePlaylistAsync("MyDailyTrain", trackIds, userAccessToken);
                AnsiConsole.MarkupLine("[green]Playlist 'MyDailyTrain' has been successfully updated on Spotify![/]");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during playlist generation.");
                AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Prompts the user for playlist generation details.
        /// </summary>
        /// <returns>A <see cref="PlaylistRequest"/> with user input.</returns>
        private static PlaylistRequest PromptUserForRequest()
        {
            string duration = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter total duration (e.g. [green]60 minutes[/]):")
                    .DefaultValue("60 minutes")
                    .Validate(x => !string.IsNullOrWhiteSpace(x)));

            string description = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter training session description: (e.g. [green]60 minutes zone2[/]):")
                    .DefaultValue("60 minutes zone2")
                    .Validate(x => !string.IsNullOrWhiteSpace(x)));

            string genres = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter preferred genres (comma separated): (e.g. [green]Rock, metal, blues[/]):")
                    .DefaultValue("Rock, metal, blues")
                    .Validate(x => !string.IsNullOrWhiteSpace(x)));

            return new PlaylistRequest
            {
                Duration = duration,
                Description = description,
                Genres = genres
            };
        }

        /// <summary>
        /// Generates a list of songs using the AI prompt service.
        /// </summary>
        /// <param name="request">The playlist request details.</param>
        /// <returns>A list of generated <see cref="Song"/> objects.</returns>
        private async Task<List<Song>> GenerateSongsAsync(PlaylistRequest request)
        {
            string prompt = _geminiService.GetPrompt(request.Description, request.Genres, request.Duration);
            string geminiResponse = await _geminiService.SendPromptAsync(prompt);
            return _songParser.ExtractSongs(geminiResponse).ToList();
        }

        /// <summary>
        /// Displays the generated songs in a table.
        /// </summary>
        /// <param name="songs">The songs to display.</param>
        private static void DisplaySongs(IEnumerable<Song> songs)
        {
            var table = new Table().Title("Generated Songs").AddColumn("Artist").AddColumn("Title");
            foreach (var song in songs)
                table.AddRow(song.Artist, song.Title);
            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Searches Spotify for track ID's matching the generated songs.
        /// </summary>
        /// <param name="songs">The songs to search for.</param>
        /// <returns>A list of Spotify track ID's.</returns>
        private async Task<List<string>> SearchSpotifyTracksAsync(IEnumerable<Song> songs)
        {
            string? accessToken = await _spotifyService.GetClientCredentialsAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                AnsiConsole.MarkupLine("[red]Failed to obtain Spotify search access token.[/]");
                return new List<string>();
            }

            var trackIds = new List<string>();
            foreach (var song in songs)
            {
                string? trackId = await _spotifyService.SearchSpotifyTrackAsync(song.Title, song.Artist, accessToken);

                if (!string.IsNullOrEmpty(trackId))
                    trackIds.Add(trackId);
                else
                    _logger.LogWarning($"Could not find Spotify Track ID for: {song.Artist} - {song.Title}");
            }
            return trackIds;
        }

        /// <summary>
        /// Displays the found Spotify track IDs in a table.
        /// </summary>
        /// <param name="trackIds">The track IDs to display.</param>
        private static void DisplayTrackIds(IEnumerable<string> trackIds)
        {
            var trackTable = new Table().Title("Spotify Track IDs").AddColumn("Track ID");
            foreach (var id in trackIds)
                trackTable.AddRow(id);
            AnsiConsole.Write(trackTable);
        }

        /// <summary>
        /// Handles Spotify user authorization and retrieves an access token.
        /// </summary>
        /// <returns>The user access token, or null if authorization fails.</returns>
        private async Task<string?> AuthorizeAndGetSpotifyTokenAsync()
        {
            AnsiConsole.MarkupLine("[yellow]To create a playlist, you need to authorize this app with Spotify.[/]");
            string scopes = "playlist-read-private playlist-read-collaborative playlist-modify-private playlist-modify-public";
            string authUrl = _spotifyService.GetAuthorizationUrl(scopes);
            AnsiConsole.MarkupLine($"[green]Opening browser for Spotify authorization:[/]");
            AnsiConsole.MarkupLine($"[link={authUrl}]{authUrl}[/]");
            AnsiConsole.MarkupLine("[grey]If your browser does not open automatically, please click the link above to continue the authorization process.[/]");

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                AnsiConsole.MarkupLine("[red]Unable to automatically open the browser. Please open the URL manually.[/]");
            }

            string? redirectUri = _configuration["Spotify:RedirectUri"];
            if (string.IsNullOrWhiteSpace(redirectUri))
                throw new InvalidOperationException("Spotify:RedirectUri configuration value is missing.");

            using var httpListener = new HttpListener();
            httpListener.Prefixes.Add(redirectUri.EndsWith("/") ? redirectUri : redirectUri + "/");
            httpListener.Start();
            AnsiConsole.MarkupLine($"[yellow]Waiting for Spotify authorization response at [blue]{redirectUri}[/] ...[/]");
            string code = "";
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            try
            {
                var context = await httpListener.GetContextAsync().WaitAsync(cts.Token);
                var request = context.Request;
                var response = context.Response;

                if (request.QueryString["code"] != null)
                {
                    var codeValue = request.QueryString["code"];
                    if (string.IsNullOrEmpty(codeValue))
                    {
                        await WriteHtmlResponseAsync(response, "<html><body>No authorization code received.</body></html>");
                        throw new InvalidOperationException("Authorization code not found in the Spotify callback request.");
                    }
                    code = codeValue;
                    await WriteHtmlResponseAsync(response, "<html><body>Authorization received. You may close this window.</body></html>");
                }
                else
                {
                    string responseString = "<html><body>No authorization code received.</body></html>";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[red]Timed out waiting for Spotify authorization response.[/]");
            }
            finally
            {
                httpListener.Stop();
            }

            if (string.IsNullOrWhiteSpace(code))
                return null;

            return await _spotifyService.ExchangeCodeForTokenAsync(code);
        }

        /// <summary>
        /// Writes an HTML response to the HTTP listener response stream.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="html">The HTML content to write.</param>
        private static async Task WriteHtmlResponseAsync(HttpListenerResponse response, string html)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}
