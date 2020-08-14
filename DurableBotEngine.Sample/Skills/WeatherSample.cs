using DurableBotEngine.Core.Models;
using LineDC.Messaging.Messages;
using LineDC.Messaging.Messages.Actions;
using Newtonsoft.Json;
using System.Collections.Generic;
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
            // dateパラメータが来たかどうか確認
            if (context.UserQuery.AllRequiredParamsPresent && context.UserQuery.Parameters.TryGetValue("date", out var date))
            {
                // ある場合はパラメータを使った返信を行う
                // コンテキストを終了
                IsContinued = false;
                return new List<ISendMessage>
                {
                    // 適当なメッセージを返す
                    new TextMessage($"{date}はたぶん晴れますよ。")
                };
            }
            else
            {
                // ない場合はDialogflowのメッセージをそのまま返す
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
                    new TextMessage(context.UserQuery.FulfillmentText, new QuickReply(new List<QuickReplyButtonObject>
                    {
                        new QuickReplyButtonObject(new PostbackTemplateAction("明日", data))
                    }))
                };
            }
        }
    }
}
