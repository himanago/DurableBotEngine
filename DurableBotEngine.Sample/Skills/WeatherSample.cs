using DurableBotEngine.Core.Models;
using LineDC.Messaging.Messages;
using LineDC.Messaging.Messages.Actions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DurableBotEngine.Sample.Skills
{
    /// <summary>
    /// 天気を確認するスキルのサンプルです。
    /// </summary>
    public class WeatherSample : ISkill
    {
        public string IntentName => "Samples.WeatherSample";
        public bool IsContinued { get; set; }

        public async Task<List<ISendMessage>> GetReplyMessagesAsync(Context context)
        {
            if (context.UserQuery.AllRequiredParamsPresent && context.UserQuery.Parameters.TryGetValue("date", out var date))
            {
                var val = date is Newtonsoft.Json.Linq.JArray arr
                    ? string.Join(',', arr.Select(s => s.ToString()))   // for LUIS
                    : date.ToString();                                  // for Dialogflow

                // dateパラメータがある場合はパラメータを使った返信を行う
                IsContinued = false;
                return new List<ISendMessage>
                {
                    new TextMessage($"{val}はたぶん晴れますよ。")
                };
            }
            else
            {
                // 聞き返すメッセージが設定されていればそのまま返す（Dialogflowのみ）
                var reprompt = context.UserQuery.FulfillmentText ?? "いつの天気ですか？";

                // サンプルでPostbackでdateが飛ばせるようにする
                var data = JsonConvert.SerializeObject(new UserQuery
                {
                    IntentName = "Samples.WeatherSample",
                    Parameters = new Dictionary<string, object>
                    {
                        {"date", "明日"}
                    },
                    AllRequiredParamsPresent = true
                });

                IsContinued = true;
                return new List<ISendMessage>
                {
                    new TextMessage(reprompt, new QuickReply(new List<QuickReplyButtonObject>
                    {
                        new QuickReplyButtonObject(new PostbackTemplateAction("明日", data))
                    }))
                };
            }
        }
    }
}
