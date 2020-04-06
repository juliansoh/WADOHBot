using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class Constants
    {
        public const string WelcomeMessage = "Hi, I’m the DOH chatbot and I’m new. Thank you for your patience as we develop this new service. If I am not able to answer your question, or if you find a problem with my system that you’d like to report, please send an email to DOH.information@doh.wa.gov.";
        public const string AckFeedbackNo = "I am sorry that I was not able to provide an adequate answer. I have logged this for someone to follow-up. We appreciate your feedback and will work to improve this service over time.";
        public const string AckFeedbackYes = "Thank you! I am glad I was able to help.";
        public const string SayGoodbye = "Thank you for visiting DOH online and using me. I hope I was able to help you.";
        public const string Instructions = "Type your question or pick one of these most frequently asked questions.";
        public const string ApplicationInsightsKey = "8bbf0d1b-f4d6-400c-9430-7c8f2e165e83";
        public const float ConfidenceScore = 0.5f;
    }
}
