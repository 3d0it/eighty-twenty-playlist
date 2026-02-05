using System.Threading.Tasks;
using Google.GenAI;

namespace EightyTwentyPlaylist.Tool.Services
{
    public class GeminiClientAdapter : IGeminiClientAdapter
    {
        public async Task<string?> GenerateContentAsync(string prompt, string apiKey)
        {
            using var client = new Client(vertexAI: null, apiKey: apiKey);
            var response = await client.Models.GenerateContentAsync(
                model: "gemini-3-flash-preview",
                contents: prompt
            );
            if (response is { Candidates: [{ Content: { Parts: [var part] } }] } && part.Text is string text)
                return text;
            return null;
        }
    }
}
