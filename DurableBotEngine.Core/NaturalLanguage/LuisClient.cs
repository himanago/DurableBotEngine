using DurableBotEngine.Core.Configurations;
using DurableBotEngine.Core.Models;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DurableBotEngine.Core.NaturalLanguage
{
    public class LuisClient : INaturalLanguageUnderstandingClient
    {        
        private string AppId { get; }
        private string PredictionEndpoint { get; }
        private string PredictionSubscriptionKey { get; }
        private string SlotName { get; }
        private string VersionId { get; }
        private string FallbackMessage { get; }

        public LuisClient(LuisSettings settings, string fallbackMessage = "Sorry, I couldn't understand.")
        {
            AppId = settings.AppId;
            PredictionEndpoint = settings.PredictionEndpoint;
            PredictionSubscriptionKey = settings.PredictionSubscriptionKey;
            SlotName = settings.SlotName;
            VersionId = settings.VersionId;
            FallbackMessage = fallbackMessage;
        }

        public async Task<UserQuery> DetectIntent(string message, string sessionId)
        {
            var credentials = new ApiKeyServiceClientCredentials(PredictionSubscriptionKey);
            var runtimeClient = new LUISRuntimeClient(credentials) { Endpoint = PredictionEndpoint };
            var request = new PredictionRequest { Query = message };
            var response = await runtimeClient.Prediction.GetSlotPredictionAsync(Guid.Parse(AppId), SlotName, request);
            var prediction = response.Prediction;

            var isFallback = prediction.Intents.Values.Max(i => i.Score) < 0.6 || prediction.TopIntent == "None";

            return new UserQuery
            {
                IntentName = prediction.TopIntent,
                IsFallback = isFallback,
                Parameters = prediction.Entities,
                AllRequiredParamsPresent = true,
                FulfillmentText = isFallback ? FallbackMessage : null,
                Timestamp = DateTime.UtcNow.Ticks
            };
        }
    }
}
