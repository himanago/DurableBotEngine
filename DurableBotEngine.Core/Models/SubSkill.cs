using Newtonsoft.Json;

namespace DurableBotEngine.Core.Models
{
    [JsonObject("subSkill")]
    public class SubSkill
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("userQuery")]
        public UserQuery UserQuery { get; set; }

        [JsonProperty("isFinished")]
        public bool IsFinished { get; set; }
    }
}
