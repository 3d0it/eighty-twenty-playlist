using EightyTwentyPlaylist.Tool.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace EightyTwentyPlaylist.Tool.Startup
{
    /// <summary>
    /// Provides extension methods for registering application services and dependencies.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all required services, configuration, and logging for the application.
        /// </summary>
        /// <param name="services">The service collection to add dependencies to.</param>
        /// <param name="configuration">The application configuration instance.</param>
        public static void Register(IServiceCollection services, IConfiguration configuration)
        {
            // Set log level from appsettings.json
            var logLevel = configuration["Logging:LogLevel:Default"];
            LogLevel minLogLevel = LogLevel.Warning;
            if (!string.IsNullOrWhiteSpace(logLevel) &&
                Enum.TryParse<LogLevel>(logLevel, true, out LogLevel parsedLevel))
            {
                minLogLevel = parsedLevel;
            }
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(config => config.AddConsole().SetMinimumLevel(minLogLevel));
            services.AddHttpClient();
            services.AddTransient<IGeminiClientAdapter, GeminiClientAdapter>();
            services.AddTransient<IGeminiService, GeminiService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                var config = sp.GetRequiredService<IConfiguration>();
                var adapter = sp.GetRequiredService<IGeminiClientAdapter>();
                return new GeminiService(httpClient, config, adapter);
            });
            services.AddTransient<ISpotifyService, SpotifyService>();
        }
    }
}
