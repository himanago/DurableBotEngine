using DurableBotEngine.Core.Models;
using System.Threading.Tasks;

namespace DurableBotEngine.Core.Entities
{
    public interface IContextEntity
    {
        /// <summary>
        /// Set dialog context data.
        /// </summary>
        Task SetContext(Context context);

        /// <summary>
        /// Get dialog context data.
        /// </summary>
        /// <returns></returns>
        Task<Context> GetContext();
    }
}
