using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DurableBotEngine.Core.Models
{
    public interface IDurableSkill : ISkill
    {
        IDurableClient DurableClient { get; set; }
    }
}
