namespace DemoBotApp.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using CognitiveServicesAuthorization;
    using CognitiveServicesTTS;
    using DemoBotApp.WebSocket;
    using Microsoft.Bing.Speech;
    using Microsoft.Bot.Connector.DirectLine;
    using NAudio.Wave;

    [RoutePrefix("chat")]
    public class WebsocketController : ApiController
    {
        private static readonly Uri SpeechSynthesisUrl = new Uri(Constants.SpeechSynthesisUrl);
        private static readonly string CognitiveSubscriptionKey = ConfigurationManager.AppSettings["CognitiveSubscriptionKey"];

        private Preferences speechPreference;
        private string speechText = string.Empty;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private DirectLineClient directLineClient;
        private static readonly string DirectLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
        private static readonly string BotId = ConfigurationManager.AppSettings["BotId"];
        private static readonly string FromUserId = "TestUser";

        private WebSocketHandler defaultHandler = new WebSocketHandler();
        private static Dictionary<string, WebSocketHandler> handlers = new Dictionary<string, WebSocketHandler>();

        public WebsocketController()
        {
            // Setup bot client
            this.directLineClient = new DirectLineClient(DirectLineSecret);

            // Setup preference for speech recognition (speech-to-text)
            this.speechPreference = new Preferences(
                Constants.SpeechLocale,
                new Uri(Constants.ShortPhraseUrl),
                new CognitiveServicesAuthorizationProvider(CognitiveSubscriptionKey));
        }

        [Route]
        [HttpGet]
        public async Task<HttpResponseMessage> Connect(string nickName)
        {
            if (string.IsNullOrEmpty(nickName))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            WebSocketHandler webSocketHandler = new WebSocketHandler();

            // Handle the case where client forgot to close connection last time
            if (handlers.ContainsKey(nickName))
            {
                WebSocketHandler origHandler = handlers[nickName];
                handlers.Remove(nickName);

                try
                {
                    await origHandler.Close();
                }
                catch
                {
                    // unexcepted error when trying to close the previous websocket
                }
            }

            handlers[nickName] = webSocketHandler;

            string conversationId = string.Empty;
            string watermark = null;

            webSocketHandler.OnOpened += ((sender, arg) =>
            {
                Conversation conversation = this.directLineClient.Conversations.StartConversation();
                conversationId = conversation.ConversationId;
            });

            webSocketHandler.OnTextMessageReceived += (async (sender, message) =>
            {
                // Do nothing with heartbeat message
                // Send text message to bot service for non-heartbeat message
                if (!string.Equals(message, "heartbeat", StringComparison.OrdinalIgnoreCase))
                {
                    await OnTextMessageReceived(webSocketHandler, message, conversationId, watermark);
                }
            });

            webSocketHandler.OnBinaryMessageReceived += (async (sender, bytes) =>
            {
                await OnBinaryMessageReceived(webSocketHandler, bytes, conversationId, watermark);
            });

            webSocketHandler.OnClosed += (sender, arg) =>
            {
                handlers.Remove(nickName);
            };

            HttpContext.Current.AcceptWebSocketRequest(webSocketHandler);
            return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
        }

        private async Task OnTextMessageReceived(WebSocketHandler handler, string message, string conversationId, string watermark)
        {
            await handler.SendMessage($"You said: {message}");
        }

        private async Task OnBinaryMessageReceived(WebSocketHandler handler, byte[] bytes, string conversationId, string watermark)
        {
            string replyMessage = null;

            // Convert speech to text
            try
            {
                using (var speechClient = new SpeechClient(this.speechPreference))
                {
                    speechClient.SubscribeToRecognitionResult(this.OnRecognitionResult);

                    // create an audio content and pass it a stream.
                    using (MemoryStream audioStream = new MemoryStream(bytes))
                    {
                        var deviceMetadata = new DeviceMetadata(DeviceType.Near, DeviceFamily.Desktop, NetworkType.Wifi, OsName.Windows, "1607", "Dell", "T3600");
                        var applicationMetadata = new ApplicationMetadata("SampleApp", "1.0.0");
                        var requestMetadata = new RequestMetadata(Guid.NewGuid(), deviceMetadata, applicationMetadata, "SampleAppService");

                        await speechClient.RecognizeAsync(new SpeechInput(PCMToWAV(audioStream), requestMetadata), this.cts.Token).ConfigureAwait(false);
                    }

                }
            }
            catch (Exception e)
            {
                //throw new DemoBotServiceException($"Convert text to speech failed: {e.Message}");
            }

            // await handler.SendMessage($"You said: {this.speechText}");

            if (!string.IsNullOrEmpty(speechText))
            {
                // Send text message to Bot Service
                await BotClientHelper.SendBotMessageAsync(this.directLineClient, conversationId, FromUserId, speechText);
                BotMessage botResponse = await BotClientHelper.ReceiveBotMessagesAsync(this.directLineClient, conversationId, watermark);
                replyMessage = botResponse.Text;
            }
            else
            {
                replyMessage = "Sorry, I don't understand.";
            }

            // Convert text to speech
            byte[] totalBytes;
            if (replyMessage.Contains("Music.Play"))
            {
                totalBytes = ((MemoryStream)SampleMusic.GetStream()).ToArray();
                await handler.SendBinary(totalBytes);
            }
            else
            {
                try
                {
                    var authorizationProvider = new CognitiveServicesAuthorizationProvider(CognitiveSubscriptionKey);
                    string accessToken = await authorizationProvider.GetAuthorizationTokenAsync();

                    var cortana = new Synthesize();
                    totalBytes = await cortana.Speak(CancellationToken.None, new Synthesize.InputOptions()
                    {
                        RequestUri = new Uri(Constants.SpeechSynthesisUrl),
                        Text = replyMessage,
                        VoiceType = Gender.Female,
                        Locale = "en-US",
                        VoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)",

                        // Service can return audio in different output format.
                        OutputFormat = AudioOutputFormat.Riff16Khz16BitMonoPcm,
                        AuthorizationToken = "Bearer " + accessToken,
                    });

                    // convert the audio format and send back to client
                    WaveFormat target = new WaveFormat(8000, 16, 2);
                    MemoryStream outStream = new MemoryStream();
                    using (WaveFormatConversionStream conversionStream = new WaveFormatConversionStream(target, new WaveFileReader(new MemoryStream(totalBytes))))
                    {
                        WaveFileWriter.WriteWavFileToStream(outStream, conversionStream);
                        outStream.Position = 0;
                    }

                    handler.SendBinary(outStream.ToArray()).Wait();
                    outStream.Dispose();
                }
                catch (Exception e)
                {
                    throw new DemoBotServiceException($"Convert text to speech failed: {e.Message}");
                }

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
            this.speechText = string.Empty;

            var response = args;

            if (response.RecognitionStatus == RecognitionStatus.Success)
            {
                this.speechText = response.Phrases[0].DisplayText;
            }

            return Task.FromResult(true);
        }

        private Stream PCMToWAV(Stream stream)
        {
            int length = (int)stream.Length;
            ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] bytes = new byte[44 + length];
            byte[] header = encoding.GetBytes("RIFF0000WAVEfmt 00000000000000000000data0000");
            WriteIntToByteArray(header, 4, length + 36);
            WriteIntToByteArray(header, 16, 16);
            WriteIntToByteArray(header, 20, (2 << 0x10) + 1);
            WriteIntToByteArray(header, 24, 8000);
            WriteIntToByteArray(header, 28, 32000);
            WriteIntToByteArray(header, 32, (16 << 0x10) + 4);
            WriteIntToByteArray(header, 40, length);
            Buffer.BlockCopy(header, 0, bytes, 0, 44);
            stream.Read(bytes, 44, length);
            stream.Seek(0, SeekOrigin.Begin);
            return new MemoryStream(bytes);
        }

        void WriteIntToByteArray(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 0x10);
            buffer[offset + 3] = (byte)(value >> 0x18);
        }
    }
}
