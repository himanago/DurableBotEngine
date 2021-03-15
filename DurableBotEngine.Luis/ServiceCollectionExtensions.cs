using DurableBotEngine.Configurations;
using DurableBotEngine.NaturalLanguage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DurableBotEngine.Luis
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLuis(this IServiceCollection services, string fallbackMessage = null)
        {
            var config = GetConfig();
            var luisSettings = config.GetSection(nameof(LuisSettings)).Get<LuisSettings>();

            return services
                .AddSingleton<INaturalLanguageUnderstandingClient, LuisClient>(_ => new LuisClient(luisSettings, fallbackMessage));
        }

        private static IConfigurationRoot GetConfig()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
