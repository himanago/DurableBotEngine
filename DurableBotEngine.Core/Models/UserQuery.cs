using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DurableBotEngine.Core.Models
{
    [JsonObject("userQuery")]
    public class UserQuery
    {
        /// <summary>
        /// Intent name for NLU.
        /// </summary>
        [JsonProperty("intentName")]
        public string IntentName { get; set; }

        /// <summary>
        /// Determines whether the query object is a fallback intent.
        /// </summary>
        [JsonProperty("IsFallback")]
        public bool IsFallback { get; set; }

        /// <summary>
        /// Fulfillment text from a NLU service.
        /// </summary>
        [JsonProperty("fulfillmentText")]
        public string FulfillmentText { get; set; } = string.Empty;
        
        /// <summary>
        /// Parameters for the skill execution.
        /// </summary>
        [JsonProperty("parameters")]
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Determines whether all required parameters are set.
        /// </summary>
        [JsonProperty("allRequiredParamsPresent")]
        public bool AllRequiredParamsPresent { get; set; }

        /// <summary>
        /// Timestamp of the query.
        /// </summary>
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; } = DateTime.UtcNow.Ticks;

        /// <summary>
        /// Definition of sub-skill.
        /// </summary>
        [JsonProperty("subSkills")]
        public SubSkill[] SubSkills { get; set; }

        /// <summary>
        /// Determines whether this is called as a sub-skill.
        /// </summary>
        [JsonProperty("isSubSkill")]
        public bool IsSubSkill { get; set; }

        /// <summary>
        /// Determines whether this query is allowed to call after the dialog finished.
        /// </summary>
        [JsonProperty("allowExternalCalls")]
        public bool AllowExternalCalls { get; set; }

        /// <summary>
        /// Matched text in expected texts.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
