using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AskDB.Test
{
    public class WeatherPlugin
    {
        public static string GetWeatherFunctionName => nameof(GetWeather);

        [KernelFunction, Description("Get the current weather of a specific city")]
        public string GetWeather([Description("city_name")] string city)
        {
            return $"30 degree celsius, sunny, very hot";
        }
    }
}
