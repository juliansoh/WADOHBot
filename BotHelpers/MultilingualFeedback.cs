using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples.Translation;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotBuilderSamples.Translation
{
    public class MultilingualFeedback : CardAction
    {
        private MicrosoftTranslator _translator;
        IConfiguration _configuration;

        private string _language;
        public void MultilingualCardAction(string language)
        {
            _language = language;
            //_translator = new MicrosoftTranslator(<< YOUR TRANSLATION KEY >>);
            //_translator = new MicrosoftTranslator(IConfiguration _configuration);
        }

        public string cardTitle
        {
            get
            {
                return this.Title;
            }

            set
            {
                this.Title = getTranslatedText(value).Result;
            }
        }
        async Task<string> getTranslatedText(string title)
        {
            return await _translator.TranslateAsync(title, _language);
        }
    }
}
