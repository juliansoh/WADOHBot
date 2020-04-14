// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples.Dialog;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using QnABot.Model;



namespace Microsoft.BotBuilderSamples.Translation
{
    /// <summary>
    /// Middleware for translating text between the user and bot.
    /// Uses the Microsoft Translator Text API.
    /// </summary>
    public class TranslationMiddleware : IMiddleware
    {
        private readonly MicrosoftTranslator _translator;
        private static string prevPrompt;
        ConversationState _conversationState;
        IConfiguration _configuration;
        static public string[] SupportedLanguages = new string[] { "en", "es", "ko", "ja", "zh", "vi" };

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationMiddleware"/> class.
        /// </summary>
        /// <param name="translator">Translator implementation to be used for text translation.</param>
        /// <param name="languageStateProperty">State property for current language.</param>
        public TranslationMiddleware(MicrosoftTranslator translator, ConversationState conversationState, IConfiguration configuration)
        {
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
            _conversationState = conversationState;
            _configuration = configuration;
        }

        /// <summary>
        /// Processes an incoming activity.
        /// </summary>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="next">The delegate to call to continue the bot middleware pipeline.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Grab the conversation data
            var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());
            string utterance = null;
            string detectedLanguage = null;


            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                utterance = ConvertToUtterance(turnContext);
                if (IsLanguageChangeRequested(utterance))
                {
                    //Before converting the language, first dynamically build the ActivityCard then display it
                    //await AskMultilingualActivityCardAsync(utterance, turnContext, cancellationToken);
                    await AskDynamicMultilingualActivityCardAsync(utterance, turnContext, cancellationToken);
                    
                    conversationData.LanguageChangeRequested = true;
                    conversationData.LanguagePreference = utterance;

                }
                else
                {
                    // Detect language unless its been optimized out after initial language choice
                    if (_configuration["SkipLanguageDetectionAfterInitialChoice"].ToLower() == "false" ||
                    conversationData.LanguagePreference.ToLower() == _configuration["TranslateTo"].ToLower())
                    {
                        detectedLanguage = await GetDetectedLanguageAsync(utterance, conversationData.LanguagePreference);
                        if (detectedLanguage != null)
                        {
                            if (detectedLanguage != conversationData.LanguagePreference)
                            {
                                conversationData.LanguageChangeDetected = true;
                                conversationData.LanguagePreference = detectedLanguage;
                            }
                        }
                    }
                }

                var translate = ShouldTranslateAsync(turnContext, conversationData.LanguagePreference, cancellationToken);

                if (!conversationData.LanguageChangeRequested && translate)
                {
                    if (turnContext.Activity.Type == ActivityTypes.Message)
                    {
                        turnContext.Activity.Text = await _translator.TranslateAsync(turnContext.Activity.Text, _configuration["TranslateTo"], cancellationToken);
                    }
                }

                turnContext.OnSendActivities(async (newContext, activities, nextSend) =>
                {
                // Grab the conversation data
                var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
                    var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

                    string userLanguage = conversationData.LanguagePreference;
                    bool shouldTranslate = userLanguage != _configuration["TranslateTo"];

                    // Translate messages sent to the user to user language
                    if (shouldTranslate)
                    {
                        List<Task> tasks = new List<Task>();
                        foreach (Activity currentActivity in activities.Where(a => a.Type == ActivityTypes.Message))
                        {
                            tasks.Add(TranslateMessageActivityAsync(currentActivity.AsMessageActivity(), userLanguage));
                        }

                        if (tasks.Any())
                        {
                            await Task.WhenAll(tasks).ConfigureAwait(false);
                        }
                    }

                    return await nextSend();
                });

                turnContext.OnUpdateActivity(async (newContext, activity, nextUpdate) =>
                {
                // Grab the conversation data
                    var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
                    var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

                    string userLanguage = conversationData.LanguagePreference;
                    bool shouldTranslate = userLanguage != _configuration["TranslateTo"];
                    
                    // Translate messages sent to the user to user language
                    if (activity.Type == ActivityTypes.Message)
                    {
                        if (shouldTranslate)
                        {
                            await TranslateMessageActivityAsync(activity.AsMessageActivity(), userLanguage);
                        }
                    }

                    return await nextUpdate();
                });
            }

            await next(cancellationToken).ConfigureAwait(false);
        }

        private async Task TranslateMessageActivityAsync(IMessageActivity activity, string targetLocale, CancellationToken cancellationToken = default(CancellationToken))
        {

            if (activity.Type == ActivityTypes.Message)
            {
                activity.Text = await _translator.TranslateAsync(activity.Text, targetLocale);
            }
        }

        private bool ShouldTranslateAsync(ITurnContext turnContext, string usersLanguage, CancellationToken cancellationToken = default(CancellationToken))
        {
            return turnContext.Activity.Text != null && !Microsoft.BotBuilderSamples.Bots.QnABot<RootDialog>.SupportedLanguages.Contains(turnContext.Activity.Text.ToLower()) && usersLanguage != _configuration["TranslateTo"];
        }

        async private Task<string> GetDetectedLanguageAsync(string utterance, string currentLanguage)
        {
            string detectedLanguage = null;

            if (string.IsNullOrEmpty(utterance))
            {
                return null;
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, _configuration["TextAnalyticsEndpoint"]))
            {
                string body = $"{{ \"documents\": [ {{ \"id\": \"1\", \"text\": \"{utterance}\" }} ] }}";

                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                Startup.HttpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _configuration["TextAnalyticsKey"]);
                Startup.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var response = await Startup.HttpClient.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();

                        var detectionResult = JsonConvert.DeserializeObject<LanguageDetectionResponse>(content);

                        if (detectionResult.documents.Count() > 0 &&
                            detectionResult.documents[0].detectedLanguages.Count() > 0 &&
                            detectionResult.documents[0].detectedLanguages[0].score > 0.5)
                        {
                            detectedLanguage = detectionResult.documents[0].detectedLanguages[0].iso6391Name.Substring(0,2);
                        }
                    }
                    else
                    {
                        // ToDo: Log error
                    }
                }
            }

            return detectedLanguage;
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

        private string ConvertToUtterance(ITurnContext turnContext)
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

        private static async Task AskMultilingualActivityCardAsync(string lang, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            string displayThisLanguageCard = lang + "SelectActivityCard.json";
            var languageActivityCard = CreateAdaptiveCardAttachment(displayThisLanguageCard);
            var response = MessageFactory.Attachment(languageActivityCard);
            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        private static Attachment CreateAdaptiveCardAttachment(string cardType)
        {
            // combine path for cross platform support
            string[] paths = { ".", "Cards", cardType };
            var fullPath = Path.Combine(paths);
            var adaptiveCard = File.ReadAllText(fullPath);
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }

        private static async Task AskDynamicMultilingualActivityCardAsync(string lang, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            //string displayThisLanguageCard = lang + "SelectActivityCard.json";
            var languageActivityCard = CreateMultilingualCard(lang);
            var response = MessageFactory.Attachment(languageActivityCard);
            await turnContext.SendActivityAsync(response, cancellationToken);
        }
        private static Attachment CreateMultilingualCard(string cardlanguage)
        {
            string buttonLanguage = cardlanguage + "Buttons";

            // combine path for cross platform support (Read
            string[] paths = { ".", "Cards", "MultilingualCardTemplate.json" };
            string adaptiveCardTemplate = File.ReadAllText(Path.Combine(paths));

            //Get the right language values to use in the creation of the card (see MultilingualValues.cs in the Constants folder)
            var Welcome = getMultilingualValues(cardlanguage, "Welcome");
            var Instructions = getMultilingualValues(cardlanguage, "Instructions");
            var Button1 = getMultilingualValues(cardlanguage, "Button1");
            var Button2 = getMultilingualValues(cardlanguage, "Button2");
            var Button3 = getMultilingualValues(cardlanguage, "Button3");
            var Button4 = getMultilingualValues(cardlanguage, "Button4");

            string welcomeAdaptiveCard = adaptiveCardTemplate
                .Replace("{Welcome}", Welcome, true, CultureInfo.CurrentCulture)
                .Replace("{Instructions}", Instructions, true, CultureInfo.CurrentCulture)
                .Replace("{Button1Text}", Button1, true, CultureInfo.CurrentCulture)
                .Replace("{Button1Data}", Button1, true, CultureInfo.CurrentCulture)
                .Replace("{Button2Text}", Button2, true, CultureInfo.CurrentCulture)
                .Replace("{Button2Data}", Button2, true, CultureInfo.CurrentCulture)
                .Replace("{Button3Text}", Button3, true, CultureInfo.CurrentCulture)
                .Replace("{Button3Data}", Button3, true, CultureInfo.CurrentCulture)
                .Replace("{Button4Text}", Button4, true, CultureInfo.CurrentCulture)
                .Replace("{Button4Data}", Button4, true, CultureInfo.CurrentCulture);


            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(welcomeAdaptiveCard),
            };
        }
        private static string getMultilingualValues(string lang, string type)
        {
            string result = "";

            switch(lang)
            {
                case "es":
                    if (type == "Welcome")
                        result = MultilingualValues.esWelcome;
                    if (type == "Instructions")
                        result = MultilingualValues.esInstructions;
                    if (type == "Button1")
                        result = MultilingualValues.esButton1;
                    if (type == "Button2")
                        result = MultilingualValues.esButton2;
                    if (type == "Button3")
                        result = MultilingualValues.esButton3;
                    if (type == "Button4")
                        result = MultilingualValues.esButton4;
                    break;

                case "zh":
                    if (type == "Welcome")
                        result = MultilingualValues.zhWelcome;
                    if (type == "Instructions")
                        result = MultilingualValues.zhInstructions;
                    if (type == "Button1")
                        result = MultilingualValues.zhButton1;
                    if (type == "Button2")
                        result = MultilingualValues.zhButton2;
                    if (type == "Button3")
                        result = MultilingualValues.zhButton3;
                    if (type == "Button4")
                        result = MultilingualValues.zhButton4;
                    break;

                case "vi":
                    if (type == "Welcome")
                        result = MultilingualValues.viWelcome;
                    if (type == "Instructions")
                        result = MultilingualValues.viInstructions;
                    if (type == "Button1")
                        result = MultilingualValues.viButton1;
                    if (type == "Button2")
                        result = MultilingualValues.viButton2;
                    if (type == "Button3")
                        result = MultilingualValues.viButton3;
                    if (type == "Button4")
                        result = MultilingualValues.viButton4;
                    break;

                case "ko":
                    if (type == "Welcome")
                        result = MultilingualValues.koWelcome;
                    if (type == "Instructions")
                        result = MultilingualValues.koInstructions;
                    if (type == "Button1")
                        result = MultilingualValues.koButton1;
                    if (type == "Button2")
                        result = MultilingualValues.koButton2;
                    if (type == "Button3")
                        result = MultilingualValues.koButton3;
                    if (type == "Button4")
                        result = MultilingualValues.koButton4;
                    break;

                case "ja":
                    if (type == "Welcome")
                        result = MultilingualValues.jaWelcome;
                    if (type == "Instructions")
                        result = MultilingualValues.jaInstructions;
                    if (type == "Button1")
                        result = MultilingualValues.jaButton1;
                    if (type == "Button2")
                        result = MultilingualValues.jaButton2;
                    if (type == "Button3")
                        result = MultilingualValues.jaButton3;
                    if (type == "Button4")
                        result = MultilingualValues.jaButton4;
                    break;

                case "en":
                    if (type == "Welcome")
                        result = MultilingualValues.enWelcome;
                    if (type == "Instructions")
                        result = MultilingualValues.enInstructions;
                    if (type == "Button1")
                        result = MultilingualValues.enButton1;
                    if (type == "Button2")
                        result = MultilingualValues.enButton2;
                    if (type == "Button3")
                        result = MultilingualValues.enButton3;
                    if (type == "Button4")
                        result = MultilingualValues.enButton4;
                    break;
            }
            return result;



        }

    }
}
