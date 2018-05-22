using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Bot.Sample.LuisBot
{
    public class OpenWeatherResponse
    {
        public string Name { get; set; }

        public IEnumerable<WeatherDescription> Weather { get; set; }

        public Main Main { get; set; }
    }

    public class WeatherDescription
    {
        public string Main { get; set; }
        public string Description { get; set; }
    }

    public class Main
    {
        public string Temp { get; set; }
        public string Humidity { get; set; }

        [JsonProperty("temp_min")]
        public string TempMin { get; set; }

        [JsonProperty("temp_max")]
        public string TempMax { get; set; }
    }

    public class GetCurrentWeatherResponse
    {
        public string Summary { get; set; }
        public string Temp { get; set; }
        public string City { get; set; }
    }
}
