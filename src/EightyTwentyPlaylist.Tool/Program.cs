using System.Threading.Tasks;
using EightyTwentyPlaylist.Tool.Startup;
using EightyTwentyPlaylist.Tool.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EightyTwentyPlaylist.Tool
{
    /// <summary>
    /// Entry point for the EightyTwentyPlaylist tool.
    /// Configures services and runs the main application logic.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main entry point. Configures DI and runs the playlist app.
        /// </summary>
        static async Task Main()
        {
            var serviceCollection = new ServiceCollection();

            // Use UserSecrets for configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets<Program>()
                .Build();

            // Register services using the new ServiceRegistration
            ServiceRegistration.Register(serviceCollection, configuration);
            serviceCollection.AddTransient<ISongParser, SongParser>();
            serviceCollection.AddTransient<PlaylistApp>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var app = serviceProvider.GetRequiredService<PlaylistApp>();
            await app.RunAsync();
        }
    }
}
