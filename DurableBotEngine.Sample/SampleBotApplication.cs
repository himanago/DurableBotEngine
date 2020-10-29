﻿using DurableBotEngine.Core;
using DurableBotEngine.Core.Configurations;
using DurableBotEngine.Core.Entities;
using DurableBotEngine.Core.Models;
using DurableBotEngine.Core.NaturalLanguage;
using LineDC.Messaging;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DurableBotEngine.Sample
{
    public class SampleBotApplication : BotApplication
    {
        private ILineMessagingClient LineMessagingClient { get; }

        public SampleBotApplication(
            ILineMessagingClient lineMessagingClient, LineMessagingApiSettings settings, INaturalLanguageUnderstandingClient dialogflowClient,
            ILoggerFactory loggerFactory, params ISkill[] skills)
            : base(lineMessagingClient, settings, dialogflowClient,
                  loggerFactory.CreateLogger(LogCategories.CreateFunctionUserCategory(nameof(WebhookEndpointFunction))),
                  skills)
        {
            LineMessagingClient = lineMessagingClient;
        }

        [FunctionName(nameof(ContextEntity))]
        public Task ContextEntityFunction([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<ContextEntity>();
    }
}