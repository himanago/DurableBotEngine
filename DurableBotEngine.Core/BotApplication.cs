using DurableBotEngine.Core.Configurations;
using DurableBotEngine.Core.Entities;
using DurableBotEngine.Core.Models;
using DurableBotEngine.Core.NaturalLanguage;
using LineDC.Messaging;
using LineDC.Messaging.Messages;
using LineDC.Messaging.Messages.Actions;
using LineDC.Messaging.Webhooks;
using LineDC.Messaging.Webhooks.Events;
using LineDC.Messaging.Webhooks.Messages;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DurableBotEngine.Core
{
    public class BotApplication : WebhookApplication, IDurableWebhookApplication
    {
        public const string SkillOrchestratorFunctionName = "SkillOrchestrator";
        private ILineMessagingClient LineMessagingClient { get; }
        private INaturalLanguageUnderstandingClient NluClient { get; }
        public IDurableClient DurableClient { get; set; }
        public static ISkill[] Skills { get; private set; }
        protected bool ShouldEnd { get; set; }
        protected ILogger Logger { get; }

        public BotApplication(
            ILineMessagingClient lineMessagingClient, LineMessagingApiSettings settings,
            INaturalLanguageUnderstandingClient nluClient, ILogger logger, params ISkill[] skills)
            : base(lineMessagingClient, settings.ChannelSecret)
        {
            LineMessagingClient = lineMessagingClient;
            NluClient = nluClient;
            Skills = skills;
            Logger = logger;
        }

        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            await OnBeforeMessageAsync(ev);
            if (ShouldEnd) return;

            UserQuery query = null;
            switch (ev.Message.Type)
            {
                case EventMessageType.Text:
                    // detect intent
                    query = await NluClient.DetectIntent(((TextEventMessage) ev.Message).Text,
                        ev.Source.UserId);
                    Logger.LogInformation(query.IntentName);

                    var skill = Skills.FirstOrDefault(s => s.IntentName == query.IntentName);
                    if (skill != null)
                    {
                        // コンテキスト確認を行う
                        var entityId = new EntityId(nameof(ContextEntity), $"{query.IntentName}-{ev.Source.UserId}");
                        var state = await DurableClient.ReadEntityStateAsync<ContextEntity>(entityId);

                        Context context = null;

                        if (state.EntityExists && state.EntityState.Context != null)
                        {
                            context = state.EntityState.Context;
                            var savedTimestamp = context.UserQuery.Timestamp;

                            // Execute incomplete sub-skills when restarting from a pause between sub-skills of a batch execution skill
                            var resume = context.UserQuery.SubSkills?.Where((s, idx) => idx != 0 && !s.IsFinished).FirstOrDefault();
                            if (resume != null)
                            {
                                context = new Context
                                {
                                    UserId = ev.Source.UserId,
                                    SkillName = resume.UserQuery.IntentName,
                                    UserQuery = resume.UserQuery
                                };
                            }
                            else
                            {
                                context.UserQuery = query;
                            }
                        }
                        else
                        {
                            context = new Context { UserId = ev.Source.UserId, IsNew = true };
                            query.Timestamp = DateTime.UtcNow.Ticks;
                            context.UserQuery = query;
                        }

                        if (skill is IDurableSkill durableSkill)
                        {
                            durableSkill.DurableClient = DurableClient;
                        }

                        var messages = await skill.GetReplyMessagesAsync(context);

                        // Save context
                        await DurableClient.SignalEntityAsync<IContextEntity>(entityId, proxy => proxy.SetContext(context));

                        if (messages != null)
                        {
                            if (!skill.IsContinued)
                            {
                                var quickReply = await FinishAndGetResumeQuickReplyAsync(context);
                                if (messages.Last().QuickReply != null && messages.Last().QuickReply.Items.Count > 0)
                                {
                                    messages.Last().QuickReply = MergeQuickReply(messages.Last().QuickReply, quickReply);
                                }
                                else
                                {
                                    messages.Last().QuickReply = quickReply;
                                }
                            }
                            await LineMessagingClient.ReplyMessageAsync(ev.ReplyToken, messages);
                        }
                        // Batch execution skill
                        else if (context.UserQuery.SubSkills != null)
                        {
                            await StartBatchSubSkills(ev.ReplyToken, context);
                        }
                    }
                    else if (query.IsFallback)
                    {
                        // TODO connect to knowledge base
                        await LineMessagingClient.ReplyMessageAsync(ev.ReplyToken,
                            query.FulfillmentText ?? "すみません、よくわかりませんでした。");
                    }
                    else
                    {
                        Logger.LogError("Intentに対応するスキル定義がありません。");
                        await LineMessagingClient.ReplyMessageAsync(ev.ReplyToken,
                            query.FulfillmentText ?? "すみません、よくわかりませんでした。");
                    }

                    break;

                case EventMessageType.Sticker:
                case EventMessageType.Image:
                case EventMessageType.Video:
                case EventMessageType.Location:
                case EventMessageType.Audio:
                case EventMessageType.File:
                default:
                    break;
            }

            await OnAfterMessageAsync(ev, query);
        }

        private QuickReply MergeQuickReply(QuickReply a, QuickReply b)
        {
            var items = new List<QuickReplyButtonObject>();
            items.AddRange(a.Items);
            items.AddRange(b.Items);
            return new QuickReply(items);
        }

        protected override async Task OnPostbackAsync(PostbackEvent ev)
        {
            var query = JsonConvert.DeserializeObject<UserQuery>(ev.Postback.Data);

            await OnBeforePostbackAsync(ev, query);
            if (ShouldEnd) return;

            var skill = Skills.FirstOrDefault(s => s.IntentName == query.IntentName);
            if (skill != null)
            {
                var requestedTimestamp = query.Timestamp;

                // コンテキスト確認を行う
                var entityId = new EntityId(nameof(ContextEntity), $"{query.IntentName}-{ev.Source.UserId}");
                var state = await DurableClient.ReadEntityStateAsync<ContextEntity>(entityId);

                Context context = null;

                if (state.EntityExists && state.EntityState.Context != null)
                {
                    context = state.EntityState.Context;
                }
                else
                {
                    context = new Context { UserId = ev.Source.UserId, IsNew = true };
                    query.Timestamp = DateTime.UtcNow.Ticks;
                }

                context.UserQuery = query;

                if (!context.UserQuery.IsSubSkill && !context.UserQuery.AllowExternalCalls &&
                    (context.IsNew || context.UserQuery.Timestamp > requestedTimestamp))
                {
                    await LineMessagingClient.ReplyMessageAsync(ev.ReplyToken, "その操作は現在できません。");
                    return;
                }

                // スキル再確認
                var subSkill = Skills.FirstOrDefault(s => s.IntentName == context.UserQuery.IntentName);
                var targetSkill = subSkill ?? skill;

                if (targetSkill is IDurableSkill durableSkill)
                {
                    durableSkill.DurableClient = DurableClient;
                }

                var messages = await targetSkill.GetReplyMessagesAsync(context);

                // 状態を保存
                await DurableClient.SignalEntityAsync<IContextEntity>(entityId, proxy => proxy.SetContext(context));

                if (messages != null)
                {
                    if (!targetSkill.IsContinued)
                    {
                        var quickReply = await FinishAndGetResumeQuickReplyAsync(context);
                        if (messages.Last().QuickReply != null && messages.Last().QuickReply.Items.Count > 0)
                        {
                            messages.Last().QuickReply = MergeQuickReply(messages.Last().QuickReply, quickReply);
                        }
                        else
                        {
                            messages.Last().QuickReply = quickReply;
                        }
                    }
                    await LineMessagingClient.ReplyMessageAsync(ev.ReplyToken, messages);
                }
                // バッチ実行スキル
                else if (context.UserQuery.SubSkills != null)
                {
                    await StartBatchSubSkills(ev.ReplyToken, context);
                }
            }

            await OnAfterPostbackAsync(ev, query);
        }

        private async Task StartBatchSubSkills(string replyToken, Context context)
        {
            // 子コンテキスト
            var childQuery = context.UserQuery.SubSkills[0].UserQuery;
            childQuery.Timestamp = DateTime.UtcNow.Ticks;
            var childEntityId = new EntityId(nameof(ContextEntity), $"{childQuery.IntentName}-{context.UserId}");
            var childContext = new Context
            {
                UserId = context.UserId,
                SkillName = childQuery.IntentName,
                UserQuery = childQuery
            };
            await DurableClient.SignalEntityAsync<IContextEntity>(childEntityId, proxy => proxy.SetContext(childContext));

            // 先頭スキルの実行
            var childMessages = await Skills.First(s => s.IntentName == context.UserQuery.SubSkills[0].UserQuery.IntentName).GetReplyMessagesAsync(childContext);
            await LineMessagingClient.ReplyMessageAsync(replyToken, childMessages);
        }

        protected virtual Task OnBeforeMessageAsync(MessageEvent ev) => Task.CompletedTask;
        protected virtual Task OnAfterMessageAsync(MessageEvent ev, UserQuery query) => Task.CompletedTask;
        protected virtual Task OnBeforePostbackAsync(PostbackEvent ev, UserQuery query) => Task.CompletedTask;
        protected virtual Task OnAfterPostbackAsync(PostbackEvent ev, UserQuery query) => Task.CompletedTask;

        /// <summary>
        /// 会話を終了し、保存されているコンテキスト情報は削除されます。
        /// 中断中のスキルがあればひとつ前の中断スキルに戻るためのクイックリプライを返します。
        /// </summary>
        /// <returns></returns>
        private async Task<QuickReply> FinishAndGetResumeQuickReplyAsync(Context context)
        {
            var entityId = new EntityId(nameof(ContextEntity), $"{context.UserQuery.IntentName}-{context.UserId}");
            await DurableClient.SignalEntityAsync<IContextEntity>(entityId, proxy => proxy.SetContext(null));

            QuickReply ret = null;

            // 中断スキルへのジャンプおよびバッチ実行の後続スキル呼び出し
            // ユーザーIDで継続中のコンテキストを検索
            var result = await DurableClient.ListEntitiesAsync(
                new EntityQuery { EntityName = nameof(ContextEntity) },
                new System.Threading.CancellationToken());

            var entityQuery = result.Entities
                .Where(e => e.EntityId.EntityKey.EndsWith(context.UserId) && !e.EntityId.EntityKey.StartsWith(context.UserQuery.IntentName))
                .OrderByDescending(e => e.LastOperationTime);

            Context targetContext = null;
            foreach (var e in entityQuery)
            {
                var state = await DurableClient.ReadEntityStateAsync<ContextEntity>(e.EntityId);
                var target = state.EntityState;
                if (state.EntityState.Context != null)
                {
                    targetContext = state.EntityState.Context;
                    break;
                }
            }

            if (targetContext != null)
            {
                // 保存されているコンテキストがバッチ実行中のものかを確認
                var batchSkills = targetContext.UserQuery.SubSkills;

                // バッチ実行の場合
                if (batchSkills != null && batchSkills.Length != 0 &&
                    // 今回完了したスキルがバッチ内のものかを調べる
                    batchSkills.Where((s, idx) =>
                        s.UserQuery.IntentName == context.UserQuery.IntentName &&   // 今回完了したスキルがバッチ実行定義に存在
                        !s.IsFinished &&                                            // 完了していない
                        (idx == 0 || batchSkills[idx - 1].IsFinished)).Any())       // 先頭スキル or 直前が完了
                {
                    var skill = batchSkills.First(s => s.UserQuery.IntentName == context.UserQuery.IntentName);
                    var index = Array.IndexOf(batchSkills, skill);

                    var targetEntityId = new EntityId(nameof(ContextEntity), $"{targetContext.UserQuery.IntentName}-{context.UserId}");

                    if (batchSkills.Length == index + 1)
                    {
                        // 最終スキルなので、バッチ実行コンテキストを削除
                        await DurableClient.SignalEntityAsync<IContextEntity>(targetEntityId, proxy => proxy.SetContext(null));
                    }
                    else
                    {
                        // 終了フラグを更新
                        batchSkills[index].IsFinished = true;
                        await DurableClient.SignalEntityAsync<IContextEntity>(targetEntityId, proxy => proxy.SetContext(targetContext));

                        // 後続へジャンプ
                        var next = batchSkills[index + 1];
                        next.UserQuery.IsSubSkill = true;

                        ret = new QuickReply(new List<QuickReplyButtonObject>
                        {
                            new QuickReplyButtonObject(new PostbackTemplateAction(
                                $"続けて{next.DisplayName}に進む",
                                JsonConvert.SerializeObject(next.UserQuery)))
                        });
                    }
                }
                else
                {
                    var userQuery = targetContext.UserQuery;

                    // 子スキル情報、フルフィルメントテキストは削除（Postbackデータに収まらないため）
                    userQuery.SubSkills = null;
                    userQuery.FulfillmentText = string.Empty;

                    ret = new QuickReply(new List<QuickReplyButtonObject>
                    {
                        new QuickReplyButtonObject(new PostbackTemplateAction(
                            "ひとつ前の対話を再開する",
                            JsonConvert.SerializeObject(userQuery)))
                    });
                }
            }
            return ret;
        }
    }
}


