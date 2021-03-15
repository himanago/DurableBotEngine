using DurableBotEngine.Core.Models;
using LineDC.Messaging.Messages;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DurableBotEngine.Sample.Skills
{
    /// <summary>
    /// 雑談スキルのサンプルです。
    /// </summary>
    public class ProfileFormSample : ISkill
    {
        public string IntentName => "Samples.ProfileFormSample";
        public bool IsContinued { get; set; }

        private readonly string[] allowedIntents =
        {
            "Samples.ProfileFormSample.Name",   // 名前：私の名前は○○です
            "Samples.ProfileFormSample.Age",    // 年齢：私は□歳です
            "Samples.ProfileFormSample.Hobby",  // 趣味：趣味は××です
            "Common.Finish",                    // 完了（共通インテント）
            "Common.Cancel"                     // キャンセル（共通インテント）
        };

        public async Task<List<ISendMessage>> GetReplyMessagesAsync(Context context)
        {
            var message = string.Empty;

            switch (context.UserQuery.IntentName)
            {
                case "Samples.ProfileFormSample.Name":
                    if (context.UserQuery.Parameters.ContainsKey("name"))
                    {
                        context.State["name"] = context.UserQuery.Parameters["name"];
                    }
                    IsContinued = true;
                    message = "ほかにも教えてください。";
                    break;

                case "Samples.ProfileFormSample.Age":
                    if (context.UserQuery.Parameters.ContainsKey("age_num"))
                    {
                        context.State["age"] = context.UserQuery.Parameters["age_num"]; // LUIS: 'age' is reserved
                    }
                    IsContinued = true;
                    message = "ほかにも教えてください。";
                    break;

                case "Samples.ProfileFormSample.Hobby":
                    if (context.UserQuery.Parameters.ContainsKey("hobby"))
                    {
                        context.State["hobby"] = context.UserQuery.Parameters["hobby"];
                    }
                    IsContinued = true;
                    message = "ほかにも教えてください。";
                    break;

                case "Common.Finish":
                    var sb = new StringBuilder();
                    if (context.State.ContainsKey("name"))
                    {
                        sb.AppendLine($"あなたの名前は{context.State["name"]}");
                    }
                    if (context.State.ContainsKey("age"))
                    {
                        sb.AppendLine($"あなたの年齢は{context.State["age"]}");
                    }
                    if (context.State.ContainsKey("hobby"))
                    {
                        sb.AppendLine($"あなたの趣味は{context.State["hobby"]}");
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append("教えてくれてありがとう！");
                        message = sb.ToString();
                    }
                    else
                    {
                        message = "また今度教えてくださいね。";
                    }

                    IsContinued = false;
                    break;

                case "Common.Cancel":
                    message = "キャンセルされました。";
                    IsContinued = false;
                    break;

                default:
                    message = "あなたのことを教えてください。";
                    IsContinued = true;
                    break;
            }

            // インテント待機
            if (IsContinued)
            {
                context.WaitForIntents(allowedIntents);
            }

            return new List<ISendMessage>
            {
                new TextMessage(message)
            };
        }
    }
}
