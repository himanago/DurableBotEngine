﻿using DurableBotEngine.Core;
using DurableBotEngine.Luis;
using DurableBotEngine.Sample;
using DurableBotEngine.Sample.Skills;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace DurableBotEngine.Sample
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services
                .AddLuis()
                .AddBot<SampleBotApplication>()
                .AddSkill<WeatherSample>()
                .AddSkill<StepSample>()
                .AddSkill<BatchSample>()
                .AddSkill<LongTimeSample>();
        }
    }
}
