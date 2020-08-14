using System.Threading.Tasks;
using DurableBotEngine.Core.Models;

namespace DurableBotEngine.Core.NaturalLanguage
{
    public interface INaturalLanguageUnderstandingClient
    {
        Task<UserQuery> DetectIntent(string message, string sessionId);
    }
}
