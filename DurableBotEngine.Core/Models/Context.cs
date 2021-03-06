﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace DurableBotEngine.Core.Models
{
    public class Context
    {
        [JsonProperty("id")]
        public string Id => $"{SkillName}-{UserId}";
        [JsonProperty("userId")]
        public string UserId { get; set; }
        [JsonProperty("skillName")]
        public string SkillName { get; set; }
        [JsonProperty("state")]
        public Dictionary<string, object> State { get; set; } = new Dictionary<string, object>();
        [JsonProperty("userQuery")]
        public UserQuery UserQuery { get; set; }
        [JsonIgnore]
        public bool IsNew { get; set; }

        [JsonProperty("isWaiting")]
        public bool IsWaiting { get; private set; }
        [JsonProperty("expectedIntentNames")]
        public string[] ExpectedIntentNames { get; private set; }


        public void WaitForText()
        {
            IsWaiting = true;
        }

        public void WaitForIntents(params string[] intentNames)
        {
            IsWaiting = true;
            ExpectedIntentNames = intentNames;
        }
    }
}
