namespace DemoBotApp
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    public class SpeechSynthesisClient
    {
        private HttpClient httpClient;
        private HttpClientHandler httpClientHandler;
        private SynthesisOptions synthesisOption;

        public SpeechSynthesisClient(SynthesisOptions options)
        {
            this.httpClientHandler = new HttpClientHandler() { CookieContainer = new CookieContainer(), UseProxy = false };
            this.httpClient = new HttpClient(httpClientHandler);

            this.synthesisOption = options;
            this.httpClient.DefaultRequestHeaders.Clear();
        }

        ~SpeechSynthesisClient()
        {
            this.httpClient.Dispose();
            this.httpClientHandler.Dispose();
        }

        /// <summary>
        /// Called when a TTS request has been completed and audio is available.
        /// </summary>
        public event EventHandler<Stream> OnAudioAvailable;

        public async Task SynthesizeTextAsync(string text, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = await CreateSynthesisRequest(text);
            HttpResponseMessage response = null;

            try
            {
                response = await this.httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = await ReadResponsePayloadAsStringAsync(response);
                    throw new DemoBotServiceException(errorMessage);
                }
                else
                {
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    this.OnAudioAvailable?.Invoke(this, contentStream);
                }
            }
            finally
            {
                response.Dispose();
                response = null;
            }
        }

        public async Task<byte[]> SynthesizeTextToBytesAsync(string text, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = await CreateSynthesisRequest(text);
            HttpResponseMessage response = null;

            try
            {
                response = await this.httpClient.SendAsync(request, CancellationToken.None);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = await ReadResponsePayloadAsStringAsync(response);
                    throw new DemoBotServiceException(errorMessage);
                }
                else
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            finally
            {
                response.Dispose();
                response = null;
            }
        }

        private async Task<HttpRequestMessage> CreateSynthesisRequest(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(text, nameof(text));
            }

            var request = new HttpRequestMessage(HttpMethod.Post, this.synthesisOption.RequestUri)
            {
                Content = new StringContent(GenerateSsml(
                    this.synthesisOption.Locale,
                    this.synthesisOption.VoiceGender.ToString(),
                    this.synthesisOption.VoiceName,
                    text))
            };

            var headers = await this.synthesisOption.GetHeaders();
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return request;
        }

        /// <summary>
        /// Generates SSML.
        /// </summary>
        /// <param name="locale">The locale.</param>
        /// <param name="gender">The gender.</param>
        /// <param name="name">The voice name.</param>
        /// <param name="text">The text input.</param>
        private string GenerateSsml(string locale, string gender, string name, string text)
        {
            var ssmlDoc = new XDocument(
                              new XElement("speak",
                                  new XAttribute("version", "1.0"),
                                  new XAttribute(XNamespace.Xml + "lang", "en-US"),
                                  new XElement("voice",
                                      new XAttribute(XNamespace.Xml + "lang", locale),
                                      new XAttribute(XNamespace.Xml + "gender", gender),
                                      new XAttribute("name", name),
                                      new XElement("prosody",
                                      new XAttribute("rate", "+15.00%"),
                                      new XAttribute("volume", "-20.00%"),
                                      text))));
            return ssmlDoc.ToString();
        }

        protected async Task<string> ReadResponsePayloadAsStringAsync(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (response.Content != null && response.Content.Headers.ContentLength > 0)
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            return null;
        }
    }
}