// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.BotBuilderSamples.Translation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger, TranslationMiddleware translationMiddleware, ConversationState conversationState = null)
            : base(configuration, logger)
        {
            if (translationMiddleware == null)
            {
                throw new NullReferenceException(nameof(translationMiddleware));
            }

            // Add translation middleware to the adapter's middleware pipeline
            Use(translationMiddleware);

            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

                // Send a message to the user
                //await SendWithoutMiddleware("The bot encounted an error or bug.");
                //await SendWithoutMiddleware("To continue to run this bot, please fix the bot source code.");
                await SendWithoutMiddleware(turnContext, configuration["InternalErrorResponse"]);

                if (conversationState != null)
                {
                    try
                    {
                        // Delete the conversationState for the current conversation to prevent the
                        // bot from getting stuck in a error-loop caused by being in a bad state.
                        // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
                        await conversationState.DeleteAsync(turnContext);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Exception caught on attempting to Delete ConversationState : {e.Message}");
                    }
                }

                // Send a trace activity, which will be displayed in the Bot Framework Emulator
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.Message, "https://www.botframework.com/schemas/error", "TurnError");
            };
        }

        private static async Task SendWithoutMiddleware(ITurnContext turnContext, string message)
        {
            // Sending the Activity directly through the Adapter rather than through the TurnContext skips the middleware processing
            // this might be important in this particular case because it might have been the TranslationMiddleware that is actually failing!
            var activity = MessageFactory.Text(message);

            // If we are skipping the TurnContext we must address the Activity manually here before sending it.
            activity.ApplyConversationReference(turnContext.Activity.GetConversationReference());

            // Send the actual Activity through the Adapter.
            await turnContext.Adapter.SendActivitiesAsync(turnContext, new[] { activity }, CancellationToken.None);
        }
    }
}
