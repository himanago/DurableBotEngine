using Azure.Storage.Blobs;
using DurableBotEngine.Configurations;
using DurableBotEngine.NaturalLanguage;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Dialogflow.v2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace DurableBotEngine.Dialogflow
{
    public static class ServiceCollectionExtensions
    {

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

        private static IConfigurationRoot GetConfig()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
