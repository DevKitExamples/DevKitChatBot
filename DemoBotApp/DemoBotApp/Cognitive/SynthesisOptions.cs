namespace DemoBotApp
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class SynthesisOptions
    {
        public SynthesisOptions(Uri requestUrl, string subscriptionKey)
        {
            if (string.IsNullOrEmpty(subscriptionKey))
            {
                throw new ArgumentNullException(subscriptionKey, nameof(subscriptionKey));
            }

            this.RequestUri = requestUrl;
            this.AuthorizationTokenProvider = new CognitiveTokenProvider(subscriptionKey);

            this.Locale = "en-US";
            this.VoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)";
            this.VoiceGender = Gender.Female;

            // Default to Riff16Khz16BitMonoPcm output format.
            this.OutputFormat = AudioOutputFormat.Riff16Khz16BitMonoPcm;
        }

        public Uri RequestUri
        {
            get;
            set;
        }

        public CognitiveTokenProvider AuthorizationTokenProvider
        {
            get;
            set;
        }

        public string Locale
        {
            get;
            set;
        }

        public Gender VoiceGender
        {
            get;
            set;
        }

        public string VoiceName
        {
            get;
            set;
        }

        public AudioOutputFormat OutputFormat
        {
            get;
            set;
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetHeaders()
        {
            List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
            headers.Add(new KeyValuePair<string, string>("Content-Type", "application/ssml+xml"));

            string outputFormat;

            switch (this.OutputFormat)
            {
                case AudioOutputFormat.Raw16Khz16BitMonoPcm:
                    outputFormat = "raw-16khz-16bit-mono-pcm";
                    break;

                case AudioOutputFormat.Raw8Khz8BitMonoMULaw:
                    outputFormat = "raw-8khz-8bit-mono-mulaw";
                    break;

                case AudioOutputFormat.Riff16Khz16BitMonoPcm:
                    outputFormat = "riff-16khz-16bit-mono-pcm";
                    break;

                case AudioOutputFormat.Riff8Khz8BitMonoMULaw:
                    outputFormat = "riff-8khz-8bit-mono-mulaw";
                    break;

                case AudioOutputFormat.Ssml16Khz16BitMonoSilk:
                    outputFormat = "ssml-16khz-16bit-mono-silk";
                    break;

                case AudioOutputFormat.Raw16Khz16BitMonoTrueSilk:
                    outputFormat = "raw-16khz-16bit-mono-truesilk";
                    break;

                case AudioOutputFormat.Ssml16Khz16BitMonoTts:
                    outputFormat = "ssml-16khz-16bit-mono-tts";
                    break;

                case AudioOutputFormat.Audio16Khz128KBitRateMonoMp3:
                    outputFormat = "audio-16khz-128kbitrate-mono-mp3";
                    break;

                case AudioOutputFormat.Audio16Khz64KBitRateMonoMp3:
                    outputFormat = "audio-16khz-64kbitrate-mono-mp3";
                    break;

                case AudioOutputFormat.Audio16Khz32KBitRateMonoMp3:
                    outputFormat = "audio-16khz-32kbitrate-mono-mp3";
                    break;

                case AudioOutputFormat.Audio16Khz16KbpsMonoSiren:
                    outputFormat = "audio-16khz-16kbps-mono-siren";
                    break;

                case AudioOutputFormat.Riff16Khz16KbpsMonoSiren:
                    outputFormat = "riff-16khz-16kbps-mono-siren";
                    break;

                default:
                    outputFormat = "riff-16khz-16bit-mono-pcm";
                    break;
            }

            // The output audio format.
            headers.Add(new KeyValuePair<string, string>("X-Microsoft-OutputFormat", outputFormat));

            // Authorization Header
            string authToken = await this.AuthorizationTokenProvider.GetAuthorizationTokenAsync();
            headers.Add(new KeyValuePair<string, string>("Authorization", "Bearer " + authToken));

            // An ID that uniquely identifies the client application.
            headers.Add(new KeyValuePair<string, string>("X-Search-AppId", "18B0554FFCF948DAA7BDDC36F1A7D1B6"));

            // An ID that uniquely identifies an application instance for each installation
            headers.Add(new KeyValuePair<string, string>("X-Search-ClientID", "CC95F12CC0E64FCBAF0AAAA792C41B3B"));

            // The software originating the request
            headers.Add(new KeyValuePair<string, string>("User-Agent", "DemoBotApp"));

            return headers;
        }
    }
}