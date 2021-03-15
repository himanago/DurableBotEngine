using DurableBotEngine.Core.Models;
using LineDC.Messaging.Messages;
using LineDC.Messaging.Messages.Actions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DurableBotEngine.Sample.Skills
{
    /// <summary>
    /// 時間のかかる処理のサンプルです。
    /// </summary>
    public class LongTimeSample : ISkill
    {
        public string IntentName => "Samples.LongTimeSample";
        public bool IsContinued { get; set; }
        private IDurableClient DurableClient { get; }

        public LongTimeSample(IDurableClientFactory durableClientFactory)
        {
            DurableClient = durableClientFactory.CreateClient();
        }

        public async Task<List<ISendMessage>> GetReplyMessagesAsync(Context context)
        {
            var message = "dummy...";

            var status = await DurableClient.GetStatusAsync(context.UserId);

            if (status == null)
            {
                await DurableClient.StartNewAsync(nameof(RunDurableTimer), context.UserId);
                message = "Orchestrator started.";
            }
            else if (
                status.RuntimeStatus == OrchestrationRuntimeStatus.Running ||
                status.RuntimeStatus == OrchestrationRuntimeStatus.Pending)
            {
                message = "Orchestrator is running. Wait a minute.";
            }
            else if (
                status.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
            {
                await DurableClient.StartNewAsync(nameof(RunDurableTimer), context.UserId);
                message = "Orchestrator is completed and restarted.";
            }

            IsContinued = false;
            return new List<ISendMessage>
            {
                new TextMessage(message)
            };
        }

        [FunctionName(nameof(RunDurableTimer))]
        public static async Task RunDurableTimer(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(10), CancellationToken.None);
        }
    }
}
