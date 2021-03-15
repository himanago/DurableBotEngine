using DurableBotEngine.Core;
using LineDC.Messaging.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace DurableBotEngine.Sample
{
    public class WebhookEndpointFunction
    {
        private BotApplication BotApplication { get; }

        public WebhookEndpointFunction(BotApplication botApplication)
        {
            BotApplication = botApplication;
        }

        [FunctionName(nameof(WebhookEndpointFunction))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                await BotApplication.RunAsync(req.Headers["x-line-signature"], body);
            }
            catch (InvalidSignatureException ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);
                return new ObjectResult(ex.Message)
                {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };
            }
            catch (LineResponseException ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);
            }

            return new OkResult();
        }
    }
}
