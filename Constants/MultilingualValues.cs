using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Translation
{
    public class MultilingualValues
    {
        //English
        public const string enWelcome = "Hi, I’m the DOH chatbot and I’m new. Thank you for your patience as we develop this new service. If I am not able to answer your question, or if you find a problem with my system that you’d like to report, please send an email to [DOH.information@doh.wa.gov](mailto:DOH.information@doh.wa.gov).";
        public const string enInstructions = "Type your message or pick a frequently asked question.";
        public const string enButton1 = "What is Covid-19?";
        public const string enButton2 = "What are the symptoms?";
        public const string enButton3 = "How does Covid-19 spread?";
        public const string enButton4 = "What are the total cases in Washington State?";
        public const string enNotCorrectAnswerGiven = "This did not answer my question";
        public const string enNotCorrectAnswerGivenValue = "No";

        //Spanish
        public const string esWelcome = "Hola, soy el chatbot del DOH y soy nuevo. Gracias por su paciencia mientras desarrollamos este nuevo servicio. Si no puedo responder a su pregunta, o si encuentra un problema con mi sistema que le gustaría informar, envíe un correo electrónico a [DOH.information@doh.wa.gov](mailto:DOH.information@doh.wa.gov)";
        public const string esInstructions = "Escriba su mensaje de elegir una pregunta frecuente.";
        public const string esButton1 = "¿Qué es Covid-19?";
        public const string esButton2 = "¿Cuáles son los síntomas?";
        public const string esButton3 = "¿Cómo se propaga Covid-19?";
        public const string esButton4 = "¿Cuáles son los casos totales en el estado de Washington?";
        public const string esNotCorrectAnswerGiven = "Esto no respondió a mi pregunta.";
        public const string esNotCorrectAnswerGivenValue = "Incorrecto";

        //Chinese
        public const string zhWelcome = "嗨，我是DOH聊天机器人，我是新的。感谢您的耐心，因为我们开发这项新服务。如果我无法回答您的问题，或者您发现我的系统有问题，您要报告，请发送电子邮件至[DOH.information@doh.wa.gov](mailto:DOH.information@doh.wa.gov)。";
        public const string zhInstructions = "键入选择常见问题的消息。";
        public const string zhButton1 = "什么是科维德-19？";
        public const string zhButton2 = "有哪些症状？";
        public const string zhButton3 = "科维德-19是如何传播的？";
        public const string zhButton4 = "华盛顿州的病例总数是多少？";
        public const string zhNotCorrectAnswerGiven = "这并没有回答我的问题。";
        public const string zhNotCorrectAnswerGivenValue = "错";

        //Korean
        public const string koWelcome = "안녕하세요, 저는 DOH 챗봇이고 저는 새로운 것입니다.이 새로운 서비스를 개발해 주셔서 감사합니다.질문에 대답할 수 없거나 신고하려는 시스템에 문제가 있는 경우[DOH.information@doh.wa.gov](mailto:DOH.information @doh.wa.gov) 이메일을 보내주십시오.";
        public const string koInstructions = "메시지를 입력하거나 자주 묻는 질문을 선택합니다.";
        public const string koButton1 = "코비드-19는 무엇인가";
        public const string koButton2 = "증상은 무엇입니까?";
        public const string koButton3 = "코비드-19는 어떻게 퍼질까요?";
        public const string koButton4 = "코비드-19는 어떻게 퍼질까요?";
        public const string koNotCorrectAnswerGiven = "이것은 내 질문에 대답하지 않았다.";
        public const string koNotCorrectAnswerGivenValue = "잘못";

        //Japanese
        public const string jaWelcome = "こんにちは、私はDOHチャットボットであり、私は新しいです。私たちはこの新しいサービスを開発する際に、あなたの忍耐をありがとう。質問に回答できない場合、または報告したいシステムに問題がある場合は、[DOH.information@doh.wa.gov](mailto:doh.information@dog.wa.gov)にメールを送信してください。";
        public const string jaInstructions = "メッセージを入力するか、よく寄せられる質問を選択します。";
        public const string jaButton1 = "コヴィッド-19とは何ですか?";
        public const string jaButton2 = "증상은 무엇입니까?";
        public const string jaButton3 = "Covid-19はどのように広がりましたか?";
        public const string jaButton4 = "ワシントン州の総症例数は?";
        public const string jaNotCorrectAnswerGiven = "これは私の質問に答えませんでした。";
        public const string jaNotCorrectAnswerGivenValue = "間違って";

        //Vietnamese
        public const string viWelcome = "Hi, I'm the DOH chatbot và tôi mới. Cảm ơn bạn đã kiên nhẫn của bạn khi chúng tôi phát triển dịch vụ mới này. Nếu tôi không thể trả lời câu hỏi của bạn, hoặc nếu bạn tìm thấy một vấn đề với hệ thống của tôi mà bạn muốn báo cáo, xin vui lòng gửi email đến [DOH.information@doh.wa.gov](mailto:DOH.information@doh.wa.gov).";
        public const string viInstructions = "Nhập tin nhắn của bạn hoặc chọn câu hỏi thường gặp.";
        public const string viButton1 = "Covid-19 là gì?";
        public const string viButton2 = "Các triệu chứng là gì?";
        public const string viButton3 = "Covid-19 lây lan như thế nào?";
        public const string viButton4 = "Tổng số trường hợp trong tiểu bang Washington là gì?";
        public const string viNotCorrectAnswerGiven = "Điều này không trả lời câu hỏi của tôi.";
        public const string viNotCorrectAnswerGivenValue = "Không";

        //Punjabi
        public const string paWelcome = "ਹੈਲੋ, ਮੈਂ DOH ਚੈਟਬੋਟ ਹਾਂ ਅਤੇ ਮੈਂ ਨਵਾਂ ਹਾਂ। ਜਦ ਅਸੀਂ ਇਸ ਨਵੀਂ ਸੇਵਾ ਦਾ ਵਿਕਾਸ ਕਰਦੇ ਹਾਂ ਤਾਂ ਤੁਹਾਡੇ ਸਬਰ ਵਾਸਤੇ ਤੁਹਾਡਾ ਧੰਨਵਾਦ। ਜੇ ਮੈਂ ਤੁਹਾਡੇ ਸਵਾਲ ਦਾ ਜਵਾਬ ਦੇਣ ਦੇ ਯੋਗ ਨਹੀਂ ਹਾਂ, ਜਾਂ ਜੇ ਤੁਹਾਨੂੰ ਮੇਰੇ ਸਿਸਟਮ ਵਿੱਚ ਕੋਈ ਸਮੱਸਿਆ ਹੈ ਜਿਸਦੀ ਤੁਸੀਂ ਰਿਪੋਰਟ ਕਰਨੀ ਚਾਹੁੰਦੇ ਹੋ, ਤਾਂ ਕਿਰਪਾ ਕਰਕੇ [DOH.information@doh.wa.gov](mailto:DOH.information@doh.wa.gov) 'ਤੇ ਇੱਕ ਈਮੇਲ ਭੇਜੋ।";
        public const string paInstructions = "ਆਪਣਾ ਸੁਨੇਹਾ ਟਾਈਪ ਕਰੋ ਜਾਂ ਬਾਰ ਬਾਰ ਪੁੱਛੇ ਗਏ ਸਵਾਲ ਨੂੰ ਚੁਣੋ।";
        public const string paButton1 = "ਕੋਵਿਡ-19 ਕੀ ਹੈ?";
        public const string paButton2 = "ਲੱਛਣ ਕੀ ਹਨ?";
        public const string paButton3 = "ਕੋਵਿਡ-19 ਕਿਵੇਂ ਫੈਲਦਾ ਹੈ?";
        public const string paButton4 = "ਵਾਸ਼ਿੰਗਟਨ ਪ੍ਰਾਂਤ ਵਿੱਚ ਕੁੱਲ ਮਾਮਲੇ ਕੀ ਹਨ?";
        public const string paNotCorrectAnswerGiven = "ਇਸ ਨੇ ਮੇਰੇ ਸਵਾਲ ਦਾ ਜਵਾਬ ਨਹੀਂ ਦਿੱਤਾ।";
        public const string paNotCorrectAnswerGivenValue = "ਨਹੀਂ";

        //Russian
        public const string ruWelcome = "Привет, я DOH чат-бот, и я новичок. Спасибо за ваше терпение, как мы разрабатываем эту новую услугу. Если я не могу ответить на ваш вопрос, или если вы обнаружите проблемы с моей системой, что вы хотели бы сообщить, пожалуйста, отправьте по электронной почте на [DOH.information@doh.wa.gov](mailto:DOH.information@doh.wa.gov).";
        public const string ruInstructions = "Введите сообщение или выберите часто задаваемый вопрос.";
        public const string ruButton1 = "Что такое Ковид-19?";
        public const string ruButton2 = "Каковы симптомы?";
        public const string ruButton3 = "Яким Covid-19 Спред?";
        public const string ruButton4 = "Каковы общие случаи заболевания в штате Вашингтон?";
        public const string ruNotCorrectAnswerGiven = "Это не ответило на мой вопрос.";
        public const string ruNotCorrectAnswerGivenValue = "Нет";

        //Ukrainian
        public const string ukWelcome = "Привіт, я DOH чат-ботів, і я новачок. Дякуємо вам за терпіння, як ми розвиваємо цю нову послугу. Якщо я не можу відповісти на ваше запитання, або якщо ви знайдете проблеми з моєю системою, яку ви хотіли б повідомити, будь ласка, надішліть листа на [DOH.information@doh.wa.gov](mailto:DOH.information@doh.wa.gov).";
        public const string ukInstructions = "Введіть своє повідомлення або виберіть запитання, яке часто задаються.";
        public const string ukButton1 = "Що таке Covid-19?";
        public const string ukButton2 = "Які симптоми?";
        public const string ukButton3 = "Яким Covid-19 Спред?";
        public const string ukButton4 = "Які загальні випадки в штаті Вашингтон?";
        public const string ukNotCorrectAnswerGiven = "Це не відповіли на моє запитання.";
        public const string ukNotCorrectAnswerGivenValue = "Неправильно";

    }
}
