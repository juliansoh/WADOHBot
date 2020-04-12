// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QnABot.Model;
using static Microsoft.Bot.Builder.Dialogs.Choices.Channel;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class QnABot<T> : ActivityHandler where T : Microsoft.Bot.Builder.Dialogs.Dialog
    {
        protected readonly BotState _conversationState;
        protected readonly Microsoft.Bot.Builder.Dialogs.Dialog Dialog;
        protected readonly BotState _userState;
        IConfiguration _configuration;
        static public string[] SupportedLanguages = new string[] { "en", "es", "fr", "ja", "zh", "de" };
        public static string QuestionAsked;
        public static string Email;
        //public string AnswerProvided;
        
        public QnABot(ConversationState conversationState, UserState userState, T dialog, IConfiguration configuration)
        {
            _conversationState = conversationState;
            this._userState = userState;
            Dialog = dialog;
            _configuration = configuration;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.WelcomeMessage), cancellationToken);
                    //Send Suggested actions
                    await SendSuggestedActionsCardAsync(turnContext, cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Grab the conversation data
            var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());
            string utterance = null;


            // Extract the text from the message activity the user sent.
            var text = turnContext.Activity.Text.ToLower();

            //If language change needed (and assuming it is zh for now)
            if(text == "zh")
            {
                string language = utterance;
                var fullWelcomePrompt = _configuration["WelcomeCardTitle"] + ". " + _configuration["WelcomePrompt"];
                string detection_re_welcomeMessage = $"{_configuration["LanguageTransitionPrompt"]}\r\n\r\n{fullWelcomePrompt}\r\n\r\n{_configuration["QuestionSegue"]}";
                var dectection_re_welcomePrompt = MessageFactory.Text(detection_re_welcomeMessage);
                var languageChange_re_welcomePrompt = MessageFactory.Text(fullWelcomePrompt);

                conversationData.LanguageChangeRequested = false;

                // Re-welcome user in their language
                await turnContext.SendActivityAsync(languageChange_re_welcomePrompt, cancellationToken);
                
            }
            else
                await Dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);

            //Check to see if the user just responded to a feedback, said bye, or anything that we may not send to QnAMaker. If no
            //conditions met, then assume it's a question destined for the QnAMaker channel.
            switch (text)
            {
                //Visitor answered "No"
                case "no":
                    //Respond to the visitor that we acknowledge their "No" feedback
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.AckFeedbackNo), cancellationToken);

                    //Uncomment the next line if you want to activate option to have customer request for follow-up via email
                    //await SendAskForFollowUpAsync(turnContext, cancellationToken);

                    //Record the Question in ApplicationInsights
                    var properties = new Dictionary<string, string>
                    { {"Question",QuestionAsked}, {"Email",Email }};
                    TelemetryClient client = new TelemetryClient();
                    client.TrackEvent("NotCorrectAnswerGiven", properties);

                    //The next line starts the conversation again with options and instructions.
                    //await SendSuggestedActionsCardAsync(turnContext, cancellationToken);
                    break;

                case "yes":
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.AckFeedbackYes), cancellationToken);
                    //The next line starts the conversation again with options and instructions.
                    //await SendSuggestedActionsCardAsync(turnContext, cancellationToken);
                    break;

                case "bye":
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.SayGoodbye), cancellationToken);
                    break;

                case "goodbye":
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.SayGoodbye), cancellationToken);
                    break;

                case "follow-up":
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Follow-up requested"), cancellationToken);
                    break;

                case "no follow-up":
                    await turnContext.SendActivityAsync(MessageFactory.Text($"NO follow-up requested"), cancellationToken);
                    break;

                default:
                    // Run the Dialog with the new message Activity through QnAMaker
                    //await Dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                    await Dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                    //Capture the question that was sent to QnAMaker
                    QuestionAsked = turnContext.Activity.Text;
                    await SendAskForFeedbackAsync(turnContext, cancellationToken);
                    break;
            }

        }

        private string ConvertToUtterance(ITurnContext<IMessageActivity> turnContext)
        {
            string utterance = null;

            // If this is a postback, check to see if its a "preferred language" choice
            if (turnContext.Activity.Value != null)
            {
                // Split out the language choice
                string[] tokens = turnContext.Activity.Value
                                                        .ToString()
                                                        .Replace('{', ' ')
                                                        .Replace('}', ' ')
                                                        .Replace('"', ' ')
                                                        .Trim()
                                                        .Split(':');

                // If postback is a language choice then grab that choice
                if (tokens.Count() == 2 && tokens[0].Trim() == "LanguagePreference")
                    turnContext.Activity.Text = utterance = tokens[1].Trim();
            }
            else
            {
                utterance = turnContext.Activity.Text.ToLower();
            }

            return utterance;
        }

        private bool IsLanguageChangeRequested(string utterance)
        {
            if (string.IsNullOrEmpty(utterance))
            {
                return false;
            }

            // If the user requested a language change through the suggested actions with values "es" or "en",
            // simply change the user's language preference in the conversation state.
            // The translation middleware will catch this setting and translate both ways to the user's
            // selected language.
            return SupportedLanguages.Contains(utterance);
        }
 
        private static async Task SendSuggestedActionsCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var welcomeCard = CreateAdaptiveCardAttachment();
            var response = MessageFactory.Attachment(welcomeCard);
            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        // Load attachment from file.
        private static Attachment CreateAdaptiveCardAttachment()
        {
            // combine path for cross platform support
            string[] paths = { ".", "Cards", "welcomeCard.json" };
            var fullPath = Path.Combine(paths);
            var adaptiveCard = File.ReadAllText(fullPath);
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
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

        private static async Task SendAskForFollowUpAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text("Do you want us to follow-up with you via email?");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Title = "Yes, follow-up", Type = ActionTypes.ImBack, Value = "Follow-up" },
                    new CardAction() { Title = "No, just fix it", Type = ActionTypes.ImBack, Value = "No Follow-up" },
                },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

 
    }
}
