using DurableBotEngine.Core.Models;
using LineDC.Messaging.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DurableBotEngine.Sample.Skills
{
    /// <summary>
    /// 複数のスキルを一度に呼び出すスキルのサンプルです。WeatherSampleとStepSampleを連続で呼び出します。
    /// 「バッチテスト」の入力で呼び出されます。
    /// </summary>
    public class BatchSample : ISkill
    {
        public string IntentName => "Samples.BatchSample";
        public bool IsContinued => false;

        public async Task<List<ISendMessage>> GetReplyMessagesAsync(Context context)
        {
            // 一括実行スキルを定義
            context.UserQuery.SubSkills = new[]
            {
                new SubSkill
                {
                    DisplayName = "天気",
                    UserQuery = new UserQuery
                    {
                        IntentName = "Samples.WeatherSample",
                        FulfillmentText = "まずは天気予報です。いつの天気が知りたいですか？",
                        AllRequiredParamsPresent = false
                    }
                },
                new SubSkill
                {
                    DisplayName = "ステップテスト",
                    UserQuery = new UserQuery
                    {
                        IntentName = "Samples.StepSample",
                        AllRequiredParamsPresent = true
                    }
                }
            };
            return null;
        }
    }
}
