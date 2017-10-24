namespace DemoBotApp.Controllers
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Bing.Speech;
    using Microsoft.Bot.Connector.DirectLine;
    using NAudio.Wave;

    [RoutePrefix("conversation")]
    public class VoiceCommandController : ApiController
    {
        private static readonly Uri ShortPhraseUrl = new Uri(Constants.ShortPhraseUrl);
        private static readonly Uri LongDictationUrl = new Uri(Constants.LongPhraseUrl);
        private static readonly Uri SpeechSynthesisUrl = new Uri(Constants.SpeechSynthesisUrl);
        private static readonly string CognitiveSubscriptionKey = ConfigurationManager.AppSettings["CognitiveSubscriptionKey"];

        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly Task completedTask = Task.FromResult(true);

        private SpeechClient speechClient;
        private SpeechSynthesisClient ttsClient;
        private string speechLocale = Constants.SpeechLocale;
        private string commandText;

        private DirectLineClient directLineClient;
        private static readonly string DirectLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
        private static readonly string BotId = ConfigurationManager.AppSettings["BotId"];
        private static readonly string FromUserId = "TestUser";

        public VoiceCommandController()
        {
            // Setup speech recognition client
            Preferences speechPreference = new Preferences(speechLocale, ShortPhraseUrl, new CognitiveTokenProvider(CognitiveSubscriptionKey));
            this.speechClient = new SpeechClient(speechPreference);
            speechClient.SubscribeToRecognitionResult(this.OnRecognitionResult);

            // Setup bot client
            this.directLineClient = new DirectLineClient(DirectLineSecret);

            // Setup speech synthesis client
            SynthesisOptions synthesisOption = new SynthesisOptions(SpeechSynthesisUrl, CognitiveSubscriptionKey);
            this.ttsClient = new SpeechSynthesisClient(synthesisOption);
        }

        [HttpPost]
        [Route("")]
        public async Task<HttpResponseMessage> StartConversation()
        {
            Conversation conversation = await this.directLineClient.Conversations.StartConversationAsync();

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(conversation.ConversationId);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

            return response;
        }

        [HttpPost]
        [Route("{conversationId}")]
        public async Task<HttpResponseMessage> SendVoiceCommand(string conversationId, string watermark = null)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            // Convert speech to text
            string speechText;
            using (Stream audio = await Request.Content.ReadAsStreamAsync())
            {
                using (SpeechRecognitionClient client = new SpeechRecognitionClient(CognitiveSubscriptionKey))
                {
                    speechText = await client.ConvertSpeechToTextAsync(audio);
                }
            }

            // Send text message to bot service
            if (!string.IsNullOrEmpty(speechText))
            {
                Activity userMessage = new Activity
                {
                    From = new ChannelAccount(FromUserId),
                    Text = speechText,
                    Type = ActivityTypes.Message
                };

                await directLineClient.Conversations.PostActivityAsync(conversationId, userMessage);
                var botResult = await BotClientHelper.ReceiveBotMessagesAsync(this.directLineClient, conversationId, watermark);

                PostVoiceCommandResponse botResponse = new PostVoiceCommandResponse
                {
                    Command = speechText,
                    Text = botResult.Text,
                    Watermark = botResult.Watermark
                };

                // Convert text to speech
                HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
                MemoryStream outStream = new MemoryStream();

                if (botResponse.Text.Contains("Music.Play"))
                {
                    outStream = (MemoryStream)SampleMusic.GetStream();
                }
                else
                {
                    this.ttsClient.OnAudioAvailable += (sender, stream) =>
                    {
                        WaveFormat target = new WaveFormat(8000, 16, 2);
                        using (WaveFormatConversionStream conversionStream = new WaveFormatConversionStream(target, new WaveFileReader(stream)))
                        {
                            WaveFileWriter.WriteWavFileToStream(outStream, conversionStream);
                            outStream.Position = 0;
                        }

                        stream.Dispose();
                    };

                    await ttsClient.SynthesizeTextAsync(botResponse.Text, CancellationToken.None);
                }

                response.Content = new StreamContent(outStream);
                response.Content.Headers.ContentLength = outStream.Length;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/x-wav");

                return response;
            }
            else
            {
                throw new InvalidOperationException("Voice command cannot be recognized.");
            }
        }

        /// <summary>
        /// Invoked when the speech client receives a phrase recognition result(s) from the server.
        /// </summary>
        /// <param name="args">The recognition result.</param>
        /// <returns>
        /// A task
        /// </returns>
        private Task OnRecognitionResult(RecognitionResult args)
        {
            var response = args;

            if (response.RecognitionStatus == RecognitionStatus.Success)
            {
                this.commandText = response.Phrases[0].DisplayText;
            }

            return this.completedTask;
        }
    }
}
