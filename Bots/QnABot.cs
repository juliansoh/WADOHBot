// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Web;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using static Microsoft.Bot.Builder.Dialogs.Choices.Channel;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using QnABot.Model;
using AdaptiveCards;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class QnABot<T> : ActivityHandler where T : Microsoft.Bot.Builder.Dialogs.Dialog
    {
        protected readonly BotState _conversationState;
        protected readonly Microsoft.Bot.Builder.Dialogs.Dialog Dialog;
        protected readonly UserState _userState;
        private readonly IStatePropertyAccessor<string> _languagePreference;
        IConfiguration _configuration;
        public static string QuestionAsked;
        public static string Email;

        //Languages supported
        private const string EnglishEnglish = "en";
        private const string EnglishSpanish = "es";
        private const string SpanishEnglish = "in";
        private const string SpanishSpanish = "it";
        private const string EnglishChineseSim = "zh-hans";
        private const string EnglishVietnamese = "vi";
        private const string EnglishKorean = "ko";
        private const string EnglishJapanese = "ja";

        //public string AnswerProvided;

        public QnABot(ConversationState conversationState, UserState userState, T dialog, IConfiguration configuration)
        {
            _conversationState = conversationState;
            _userState = userState ?? throw new NullReferenceException(nameof(userState));
            Dialog = dialog;
            _configuration = configuration;
            _languagePreference = userState.CreateProperty<string>("LanguagePreference");
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            // Extract the text from the message activity the user sent.
            var text = turnContext.Activity.Text.ToLower();

            if (IsLanguageChangeRequested(turnContext.Activity.Text))
            {
                var currentLang = turnContext.Activity.Text.ToLower();
                //var lang = currentLang;
                var lang = currentLang == EnglishEnglish || currentLang == SpanishEnglish ? EnglishEnglish : EnglishSpanish;
                await _languagePreference.SetAsync(turnContext, lang, cancellationToken);
                var reply = MessageFactory.Text($"Your current language code is: {lang}");
                await turnContext.SendActivityAsync(reply, cancellationToken);

                // Save the user profile updates into the user state.
                await _userState.SaveChangesAsync(turnContext, false, cancellationToken);

                // Run the Dialog with the new message Activity through QnAMaker
                //await Dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
            }
            else
            /*{
                // Show the user the possible options for language. If the user chooses a different language
                // than the default, then the translation middleware will pick it up from the user state and
                // translate messages both ways, i.e. user to bot and bot to user.
                var reply = MessageFactory.Text("Choose your language:");
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                        {
                            new CardAction() { Title = "Español", Type = ActionTypes.PostBack, Value = EnglishSpanish },
                            new CardAction() { Title = "English", Type = ActionTypes.PostBack, Value = EnglishEnglish },
                        },
                };
            }*/
            //Check to see if the user just responded to a feedback, said bye, or anything that we may not send to QnAMaker. If no
            //conditions met, then assume it's a question destined for the QnAMaker channel.
            {
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
                        await SendSuggestedActionsAsync(turnContext, cancellationToken);
                        break;

                    case "yes":
                        await turnContext.SendActivityAsync(MessageFactory.Text(Constants.AckFeedbackYes), cancellationToken);
                        await SendSuggestedActionsAsync(turnContext, cancellationToken);
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
                        await Dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                        //Capture the question that was sent to QnAMaker
                        QuestionAsked = turnContext.Activity.Text;
                        await SendAskForFeedbackAsync(turnContext, cancellationToken);
                        break;
                }
            }
        }
    
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            //Pre-multilingual and Adaptive Card addition
            /*foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.WelcomeMessage), cancellationToken);
                    //Send Suggested actions
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);
                }
            }*/

            foreach (var member in turnContext.Activity.MembersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var welcomeCard = CreateAdaptiveCardAttachment();
                    var response = MessageFactory.Attachment(welcomeCard);
                    await turnContext.SendActivityAsync(response, cancellationToken);
                    await turnContext.SendActivityAsync(MessageFactory.Text(Constants.WelcomeMessage), cancellationToken);
                    //Send Suggested actions - Replaced with AdaptiveCardAttachment()
                    //await SendSuggestedActionsAsync(turnContext, cancellationToken);
                    var reply = MessageFactory.Text("Choose your language:");
                    //Ask for language
                    reply.SuggestedActions = new SuggestedActions()
                    {
                        Actions = new List<CardAction>()
                        {
                            new CardAction() { Title = "English", Type = ActionTypes.PostBack, Value = EnglishEnglish },
                            new CardAction() { Title = "Chinese Simplified", Type = ActionTypes.PostBack, Value = EnglishChineseSim },
                            new CardAction() { Title = "Español", Type = ActionTypes.PostBack, Value = EnglishSpanish },
                            new CardAction() { Title = "Japanese", Type = ActionTypes.PostBack, Value = EnglishJapanese },
                            new CardAction() { Title = "Korean", Type = ActionTypes.PostBack, Value = EnglishKorean },
                            new CardAction() { Title = "Vietnamese", Type = ActionTypes.PostBack, Value = EnglishVietnamese },
                        },
                
                    };
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            }
        }

        private static async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text(Constants.Instructions);

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

        private static bool IsLanguageChangeRequested(string utterance)
        {
            if (string.IsNullOrEmpty(utterance))
            {
                return false;
            }

            utterance = utterance.ToLower().Trim();
            return utterance == EnglishSpanish || utterance == EnglishEnglish
                || utterance == SpanishSpanish || utterance == SpanishEnglish
                || utterance == EnglishChineseSim || utterance == EnglishVietnamese
                || utterance == EnglishKorean || utterance == EnglishJapanese;
        }
    }
}
