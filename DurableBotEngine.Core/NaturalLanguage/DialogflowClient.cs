using System;
using System.Threading.Tasks;
using DurableBotEngine.Core.Models;
using Google.Apis.Dialogflow.v2;
using Google.Apis.Dialogflow.v2.Data;

namespace DurableBotEngine.Core.NaturalLanguage
{
    public class DialogflowClient : INaturalLanguageUnderstandingClient
    {
        private DialogflowService _service;
        private string _projectId;

        public DialogflowClient(DialogflowService service, string projectId)
        {
            _service = service;
            _projectId = projectId;
        }

        public async Task<UserQuery> DetectIntent(string message, string sessionId)
        {
            var request = _service.Projects.Agent.Sessions.DetectIntent(new GoogleCloudDialogflowV2DetectIntentRequest
            {
                QueryInput = new GoogleCloudDialogflowV2QueryInput
                {
                    Text = new GoogleCloudDialogflowV2TextInput
                    {
                        Text = message,
                        LanguageCode = "ja"
                    }
                }
            }, $"projects/{_projectId}/agent/sessions/{sessionId}");

            var response = await request.ExecuteAsync();
            return response.QueryResult.ToUserQuery();
        }
    }

    public static class QueryResultEx
    {
        public static UserQuery ToUserQuery(this GoogleCloudDialogflowV2QueryResult queryResult)
        {
            return new UserQuery
            {
                IntentName = queryResult.Intent.DisplayName,
                IsFallback = queryResult.Intent.IsFallback ?? false,
                FulfillmentText = queryResult.FulfillmentText,
                Parameters = queryResult.Parameters,
                AllRequiredParamsPresent = queryResult.AllRequiredParamsPresent ?? false,
                Timestamp = DateTime.UtcNow.Ticks
            };
        }
    }
}
