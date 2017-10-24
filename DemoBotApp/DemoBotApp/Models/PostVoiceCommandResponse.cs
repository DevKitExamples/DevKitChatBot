using Newtonsoft.Json;

namespace DemoBotApp
{
    public class PostVoiceCommandResponse
    {
        [JsonProperty("Command")]
        public string Command { get; set; }

        [JsonProperty("Text")]
        public string Text { get; set; }

        [JsonProperty("Watermark")]
        public string Watermark { get; set; }
    }
}