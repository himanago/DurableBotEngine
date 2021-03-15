using DurableBotEngine.Core.Models;
using LineDC.Messaging.Messages;
using LineDC.Messaging.Messages.Actions;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DurableBotEngine.Sample.Skills
{
    /// <summary>
    /// ユーザーからの入力と対応する処理をいくつかのステップに分けて行うスキルのサンプルです。
    /// 「ステップテスト」の入力で呼び出されます。
    /// </summary>
    public class StepSample : ISkill
    {
        public string IntentName => "Samples.StepSample";
        public bool IsContinued { get; set; }
        private ILogger Logger { get; }

        public StepSample(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(LogCategories.CreateFunctionUserCategory(nameof(WebhookEndpointFunction)));
        }

        public async Task<List<ISendMessage>> GetReplyMessagesAsync(Context context)
        {
            // 途中でキャンセルした場合は終了
            if (context.UserQuery.Parameters.ContainsKey("cancel"))
            {
                IsContinued = false;
                return new List<ISendMessage>
                {
                    new TextMessage("キャンセルしました。")
                };
            }

            // ユーザーの選択に応じた処理を実施
            if (context.UserQuery.Parameters.TryGetValue("next", out var nextObj) && int.TryParse(nextObj.ToString(), out var next))
            {
                switch (next)
                {
                    case 1:
                        return GetMessageForStep1();

                    case 2:
                        return GetMessageForStep2(context);

                    case 3:
                        return GetMessageForStep3(context);
                }
            }
            else
            {
                // 初回はステップ1
                return GetMessageForStep1();
            }
            return null;
        }

        private List<ISendMessage> GetMessageForStep1()
        {
            IsContinued = true;

            return new List<ISendMessage>
                {
                    new TextMessage("ステップ1：AかBを選んでください。", new QuickReply(new List<QuickReplyButtonObject>
                    {
                        new QuickReplyButtonObject(new PostbackTemplateAction("Aを選ぶ", GetPostBackData(1, 2, "A"))),
                        new QuickReplyButtonObject(new PostbackTemplateAction("Bを選ぶ", GetPostBackData(1, 2, "B"))),
                        new QuickReplyButtonObject(new PostbackTemplateAction("やめる", JsonConvert.SerializeObject(new UserQuery
                        {
                            IntentName = "Samples.StepSample",
                            Parameters = new Dictionary<string, object>
                            {
                                {"cancel", true}
                            },
                            AllRequiredParamsPresent = true
                        })))
                    }))
                };
        }

        private List<ISendMessage> GetMessageForStep2(Context context)
        {
            try
            {
                IsContinued = true;

                Logger.LogWarning(string.Join(",", context.UserQuery.Parameters.Select(p => $"{p.Key}:{p.Value}").ToArray()));
                Logger.LogWarning($"context.State is null: {context.State == null}");

                // ステップ1の回答
                context.State["step1"] = context.UserQuery.Parameters["select"];

                Logger.LogWarning("aaa");

                return new List<ISendMessage>
                {
                    new TextMessage("ステップ2：CかDを選んでください。", new QuickReply(new List<QuickReplyButtonObject>
                    {
                        new QuickReplyButtonObject(new PostbackTemplateAction("Cを選ぶ", GetPostBackData(2, 3, "C"))),
                        new QuickReplyButtonObject(new PostbackTemplateAction("Dを選ぶ", GetPostBackData(2, 3, "D"))),
                        new QuickReplyButtonObject(new PostbackTemplateAction("ステップ1に戻る", GetPostBackData(2, 1, null))),
                        new QuickReplyButtonObject(new PostbackTemplateAction("やめる", JsonConvert.SerializeObject(new UserQuery
                        {
                            IntentName = "Samples.StepSample",
                            Parameters = new Dictionary<string, object>
                            {
                                {"cancel", true}
                            },
                            AllRequiredParamsPresent = true
                        })))
                    }))
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                Logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private List<ISendMessage> GetMessageForStep3(Context context)
        {
            IsContinued = false;

            return new List<ISendMessage>
                {
                    new TextMessage("終わりです。"),
                    new TextMessage($"ステップ1：{context.State["step1"]}, ステップ2：{context.UserQuery.Parameters["select"]}")
                };
        }


        private string GetPostBackData(int step, int next, string select)
        {
            return JsonConvert.SerializeObject(new UserQuery
            {
                IntentName = "Samples.StepSample",
                Parameters = new Dictionary<string, object>
                {
                    { "step", step },       // 実行中のステップ
                    { "next", next },       // 次に実行するステップ
                    { "select", select }    // ユーザーの選択した値
                },
                AllRequiredParamsPresent = true
            });
        }

    }
}
