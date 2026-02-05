using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace EightyTwentyPlaylist.Tool.Services
{
    /// <summary>
    /// Service for interacting with the Gemini generative language API.
    /// </summary>
    public class GeminiService : IGeminiService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiEndpoint;
        private readonly IGeminiClientAdapter _geminiClientAdapter;
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeminiService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for requests.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="geminiClientAdapter">Gemini client adapter for API calls.</param>
        /// <exception cref="ArgumentNullException">Thrown if required configuration is missing.</exception>
        public GeminiService(HttpClient httpClient, IConfiguration configuration, IGeminiClientAdapter geminiClientAdapter)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey not found in configuration.");
            _apiEndpoint = configuration["Gemini:ApiEndpoint"] ?? throw new ArgumentNullException("Gemini:ApiEndpoint not found in configuration.");
            _geminiClientAdapter = geminiClientAdapter ?? throw new ArgumentNullException(nameof(geminiClientAdapter));
        }

        /// <inheritdoc />
        public virtual string GetPrompt(string description, string genres, string duration)
        {
            return $@"create a running training playlist with a total duration of {duration}.   
Training Session Breakdown:
{description}
Music Preferences:
Genres: {genres}.
Explicit Content: Avoid songs with explicit lyrics.
The total playlist duration should be as close as possible to the specified duration.
Understanding Training Zones for Music Selection:
To help you select the most appropriate songs, here's a description of each training zone and the desired musical feel:
Zone1 (Very Low Intensity): This zone is about very low intensity, requiring the runner to actively hold back to a slower-than-natural pace.It's almost impossible to go too slow.
Music Selection for Zone1: Choose songs with a very steady, mellow, and consistent tempo.Think of background music that encourages a relaxed, easy pace. Avoid anything too energetic or with strong, sudden tempo changes.
Zone2 (Fairly Broad / Conversational Pace): This zone is fairly broad, where runners go by feel.It's a comfortable, conversational pace. If the runner feels strong, they'll be at the top end; if tired, at the bottom end.
Music Selection for Zone2: Select songs with a consistent, driving, but not overwhelming energy. The tempo should encourage a steady, rhythmic stride without pushing for speed.It should feel like something you can comfortably run to for an extended period without feeling rushed.
Zone X (Moderate-Intensity Rut / Race Intensity Overlap): Generally avoided in training as it's a gap between Zones2 and3. It can overlap with race intensity for half-marathons and marathons.
Music Selection for Zone X: If a future request includes Zone X, select songs with a moderately hard, consistent push.The music should feel like sustained effort, but not overwhelming.
Zone3 (Lactate Threshold / 'Comfortably Hard'): This corresponds to lactate threshold intensity, often described as 'comfortably hard' or the fastest speed that still feels relaxed.The runner should stay one or two steps back from the feeling of strain.
Music Selection for Zone3: Choose songs with a strong, pushing, and energetic tempo that feels sustainable for a significant effort.The music should have a sense of urgency but still allow for a 'relaxed' feeling despite the effort.
Zone Y (Gap Zone): A gap between zones, too fast for threshold and too slow for high-intensity intervals in Zones4 and5.
Music Selection for Zone Y: If a future request includes Zone Y, select music that feels like a transitional, slightly elevated intensity but doesn't quite hit peak effort.
Zone4 (Higher Intensity / Interval Start): Mastering this zone involves connecting numbers with feel, and it's okay to get it wrong initially. It's about consistently hitting the right intensity for shorter, harder efforts.
Music Selection for Zone4: Select songs with a very high energy and a strong, driving beat suitable for sustained hard efforts.The music should feel invigorating and help maintain a quick pace.
Zone5 (Maximal Effort / Sprints): Almost always used in interval workouts, ranging from the highest speed for a few minutes to a full sprint. Pace is tailored to the interval length.
Music Selection for Zone5: Choose songs with extremely high energy, very fast tempos, and potentially explosive or powerful sections. These are for maximal effort bursts, so the music should be motivating for short, intense pushes.
Output Format:
DO NOT include any conversational text, explanations, or additional characters. Your response must be ONLY a single, continuous string of songs in the format: Artist,Song;Artist,Song;Artist3,SongZ;)";
        }

        /// <inheritdoc />
        public virtual async Task<string> SendPromptAsync(string prompt)
        {
            var result = await _geminiClientAdapter.GenerateContentAsync(prompt, _apiKey);
            return result ?? "Error: Could not extract text from the Gemini response.";
        }

        /// <summary>
        /// Disposes the GeminiService and its resources.
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
