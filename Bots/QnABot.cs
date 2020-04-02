// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class QnABot<T> : ActivityHandler where T : Microsoft.Bot.Builder.Dialogs.Dialog
    {
        protected readonly BotState ConversationState;
        protected readonly Microsoft.Bot.Builder.Dialogs.Dialog Dialog;
        protected readonly BotState UserState;

        public QnABot(ConversationState conversationState, UserState userState, T dialog)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Extract the text from the message activity the user sent.
            var text = turnContext.Activity.Text.ToLower();

            //Check to see if the user just responded to a feedback.
            switch(text)
            {
                //Visitor answered "No"
                case "no":
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.AckFeedback), cancellationToken);
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);
                    break;

                case "yes":
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.AckFeedback), cancellationToken);
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);
                    break;

                case "bye":
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.SayGoodbye), cancellationToken);
                    break;

                case "goodbye":
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.SayGoodbye), cancellationToken);
                    break;

                default:
                    // Run the Dialog with the new message Activity through QnAMaker
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                    await SendAskForFeedbackAsync(turnContext, cancellationToken);
                    break;
            }




        }
       
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.WelcomeMessage), cancellationToken);
                    //Send Suggested actions
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);
                }
        }
        }

        private static async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text("Type you question or pick one of these most frequently asked questions.");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Title = "What is Covid-19", Type = ActionTypes.ImBack, Value = "What is Covid-19" },
                    new CardAction() { Title = "Symptoms of Covid-19", Type = ActionTypes.ImBack, Value = "Symptoms of Covid-19" },
                    new CardAction() { Title = "How does Covid-19 spread", Type = ActionTypes.ImBack, Value = "How does Covid-19 spread" },
                },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private static async Task SendAskForFeedbackAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text("Did this answer your question?");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Title = "Yes", Type = ActionTypes.ImBack, Value = "Yes" },
                    new CardAction() { Title = "No", Type = ActionTypes.ImBack, Value = "No" },
                },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
