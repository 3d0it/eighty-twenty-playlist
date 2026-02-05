using System.Threading.Tasks;

namespace EightyTwentyPlaylist.Tool.Services
{
    public interface IGeminiClientAdapter
    {
        Task<string?> GenerateContentAsync(string prompt, string apiKey);
    }
}
