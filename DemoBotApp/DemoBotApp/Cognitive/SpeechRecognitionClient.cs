namespace DemoBotApp
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class SpeechRecognitionClient : IDisposable
    {
        private string cognitiveSubscriptionKey;
        private HttpClient httpClient;

        private bool disposed;

        public SpeechRecognitionClient(string subscriptionKey)
        {
            if (string.IsNullOrEmpty(subscriptionKey))
            {
                throw new ArgumentNullException(subscriptionKey);
            }

            this.cognitiveSubscriptionKey = subscriptionKey;
            this.httpClient = new HttpClient()
            {
                BaseAddress = new Uri(Constants.SpeechRecognitionServiceUrl)
            };
        }

        ~SpeechRecognitionClient()
        {
            Dispose(false);
        }

        public async Task<string> ConvertSpeechToTextAsync(Stream contentStream)
        {
            CognitiveTokenProvider tokenProvider = new CognitiveTokenProvider(this.cognitiveSubscriptionKey);
            string token = await tokenProvider.GetAuthorizationTokenAsync();

            this.httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-type", @"audio/wav; codec=""audio/pcm""; samplerate=8000");
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json;text/xml");
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Host", "speech.platform.bing.com");
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Transfer-Encoding", "chunked");
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Expect", "100-continue");

            using (var binaryContent = new StreamContent(contentStream))
            {
                var response = await this.httpClient.PostAsync(Constants.SpeechRecognitionServiceUrl, binaryContent);
                var responseString = await response.Content.ReadAsStringAsync();

                try
                {
                    SpeechRecognitionResult result = JsonConvert.DeserializeObject<SpeechRecognitionResult>(responseString);
                    return result.DisplayText;
                }
                catch (JsonReaderException ex)
                {
                    throw new InvalidDataException(responseString, ex);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                if (this.httpClient != null)
                {
                    this.httpClient.Dispose();
                }
            }

            this.disposed = true;
        }
    }
}
