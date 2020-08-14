using LineDC.Messaging.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DurableBotEngine.Core.Models
{
    public interface ISkill
    {
        string IntentName { get; }
        bool IsContinued { get; }
        Task<List<ISendMessage>> GetReplyMessagesAsync(Context context);
    }
}
