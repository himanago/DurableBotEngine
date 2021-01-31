using Azure.Storage.Blobs;
using DurableBotEngine.Core.Configurations;
using DurableBotEngine.Core.Models;
using DurableBotEngine.Core.NaturalLanguage;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Dialogflow.v2;
using Google.Apis.Services;
using LineDC.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

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
                .AddSingleton(lineSettings)
                .AddSingleton<ILineMessagingClient>(_ => LineMessagingClient.Create(lineSettings.ChannelAccessToken))
                .AddScoped<BotApplication, TBotApplication>();
        }

        public static IServiceCollection AddLuis(this IServiceCollection services, string fallbackMessage = null)
        {
            var config = GetConfig();
            var luisSettings = config.GetSection(nameof(LuisSettings)).Get<LuisSettings>();

            return services
                .AddSingleton<INaturalLanguageUnderstandingClient, LuisClient>(_ => new LuisClient(luisSettings, fallbackMessage));
        }

        public static IServiceCollection AddDialogflow(this IServiceCollection services)
        {
            var config = GetConfig();
            var dialogflowSettings = config.GetSection(nameof(DialogflowSettings)).Get<DialogflowSettings>();

            return services.AddSingleton<INaturalLanguageUnderstandingClient, DialogflowClient>(_ =>
            {
                var containerClient = new BlobContainerClient(
                    dialogflowSettings.ApiCredentialsStorageConnectionString,
                    dialogflowSettings.ApiCredentialsContainerName);

                var blobClient = containerClient.GetBlobClient(dialogflowSettings.ApiCredentialsJsonName);

                ServiceAccountCredential credential;
                using (var stream = new MemoryStream())
                {
                    blobClient.DownloadTo(stream);
                    stream.Position = 0;
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(DialogflowService.Scope.CloudPlatform)
                        .UnderlyingCredential as ServiceAccountCredential;
                }

                var service = new DialogflowService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                });

                return new DialogflowClient(service, dialogflowSettings.ProjectId);
            });
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
