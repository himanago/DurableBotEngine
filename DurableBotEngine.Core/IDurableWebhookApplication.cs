using LineDC.Messaging.Webhooks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DurableBotEngine.Core
{
    public interface IDurableWebhookApplication : IWebhookApplication
    {
        IDurableClient DurableClient { get; set; }
    }
}
