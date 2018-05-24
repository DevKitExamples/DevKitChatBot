namespace DemoBotApp
{
    public static class Constants
    {
        public static readonly string SpeechRecognitionServiceUrl = @"https://speech.platform.bing.com/speech/recognition/interactive/cognitiveservices/v1?language=en-US";

        public static readonly string ShortPhraseUrl = @"wss://speech.platform.bing.com/api/service/recognition";

        public static readonly string LongPhraseUrl = @"wss://speech.platform.bing.com/api/service/recognition/continuous";

        public static readonly string SpeechSynthesisUrl = "https://speech.platform.bing.com/synthesize";

        public static readonly string SpeechLocale = "en-US";
    }
}