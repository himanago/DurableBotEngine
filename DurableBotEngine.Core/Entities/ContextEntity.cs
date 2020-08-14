using DurableBotEngine.Core.Models;
using System.Threading.Tasks;

namespace DurableBotEngine.Core.Entities
{
    public class ContextEntity : IContextEntity
    {
        public Context Context { get; set; }

        /// <summary>
        /// Set dialog context data.
        /// </summary>
        public async Task SetContext(Context context) => Context = context;

        /// <summary>
        /// Get dialog context data.
        /// </summary>
        /// <returns></returns>
        public async Task<Context> GetContext() => Context;
    }
}
