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
        public static IServiceCollection AddBot<TBotApplication>(this IServiceCollection services, NaturalLanguageOptions naturalLanguageOption = NaturalLanguageOptions.Dialogflow)
            where TBotApplication: BotApplication
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables()
                .Build();

            var lineSettings = config.GetSection(nameof(LineMessagingApiSettings)).Get<LineMessagingApiSettings>();

            services
                .AddSingleton(lineSettings)
                .AddSingleton<ILineMessagingClient>(_ => LineMessagingClient.Create(lineSettings.ChannelAccessToken));

            if (naturalLanguageOption == NaturalLanguageOptions.Dialogflow)
            {
                // Dialogflow
                var dialogflowSettings = config.GetSection(nameof(DialogflowSettings)).Get<DialogflowSettings>();

                services.AddSingleton<INaturalLanguageUnderstandingClient, DialogflowClient>(_ =>
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
            else
            {
                // LUIS: not implmented
                throw new NotImplementedException();
            }

            services.AddScoped<BotApplication, TBotApplication>();
            return services;
        }

        public static IServiceCollection AddSkill<T>(this IServiceCollection services)
            where T : class, ISkill
        {
            return services.AddSingleton<ISkill, T>();
        }
    }
}
