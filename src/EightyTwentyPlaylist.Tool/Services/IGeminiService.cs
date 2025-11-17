using System.Threading.Tasks;

namespace EightyTwentyPlaylist.Tool.Services
{
    /// <summary>
    /// Defines the contract for a service that generates prompts and sends them to the Gemini AI model.
    /// </summary>
    public interface IGeminiService
    {
        /// <summary>
        /// Generates a prompt string based on the provided description, genres, and duration.
        /// </summary>
        /// <param name="description">The training session description.</param>
        /// <param name="genres">The preferred music genres.</param>
        /// <param name="duration">The total duration for the playlist.</param>
        /// <returns>A formatted prompt string for the AI model.</returns>
        string GetPrompt(string description, string genres, string duration);

        /// <summary>
        /// Sends a prompt to the Gemini AI model and returns the response.
        /// </summary>
        /// <param name="prompt">The prompt string to send.</param>
        /// <returns>The AI model's response as a string.</returns>
        Task<string> SendPromptAsync(string prompt);
    }
}
