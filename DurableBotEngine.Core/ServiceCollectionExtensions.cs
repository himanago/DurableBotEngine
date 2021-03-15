using DurableBotEngine.Configurations;
using DurableBotEngine.Core.Models;
using LineDC.Messaging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DurableBotEngine.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBot<TBotApplication>(this IServiceCollection services)
            where TBotApplication: BotApplication
        {
            var config = GetConfig();
            var lineSettings = config.GetSection(nameof(LineMessagingApiSettings)).Get<LineMessagingApiSettings>();

            return services
                .Configure<DurableClientOptions>(options =>
                {
                    options.ConnectionName = "DurableManagementStorage";
                    options.TaskHub = Environment.GetEnvironmentVariable("TaskHubName");
                    options.IsExternalClient = true;
                })
                .AddDurableClientFactory()
                .AddSingleton(lineSettings)
                .AddSingleton<ILineMessagingClient>(_ => LineMessagingClient.Create(lineSettings.ChannelAccessToken))
                .AddScoped<BotApplication, TBotApplication>();
        }

        public static IServiceCollection AddSkill<T>(this IServiceCollection services)
            where T : class, ISkill
        {
            return services.AddSingleton<ISkill, T>();
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
