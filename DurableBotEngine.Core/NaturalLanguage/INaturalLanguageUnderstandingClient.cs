using System.Threading.Tasks;
using DurableBotEngine.Core.Models;

namespace DurableBotEngine.NaturalLanguage
{
    public interface INaturalLanguageUnderstandingClient
    {
        Task<UserQuery> DetectIntent(string message, string sessionId);
    }
}
