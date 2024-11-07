using RealtimeInteractiveConsole.Utilities.RealtimeInteractiveConsole.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealtimeInteractiveConsole
{
    public class WeatherService
    {
        [Description("Gets the weather for a given location. ")]
        public string GetWeather(
            [Description("The city and state, e.g. 'Eindhoven' or 'Amsterdam' or 'Best'")] string location
            //[Description("The unit for temperature, either 'c' or 'f'")] string unit
            )
        {
            var rand         = new Random();
            int temp         = rand.Next(-5, 30 );
            int humidity     = rand.Next(30, 100);
            int windSpeed    = rand.Next( 0, 20 );
            bool isPrecip    = rand.Next(100) < 40;
            string precip    = isPrecip ? $"{new[] { "light", "moderate", "heavy" }[rand.Next(3)]} {(temp <= 0 ? "snowfall" : "rainfall")}" : "";
            string condition = string.IsNullOrEmpty(precip) ? new[] { "sunny", "partly cloudy", "overcast", "foggy" }[rand.Next(4)]:"";
            var weatherReport = $"{temp}°C with {condition}, humidity at {humidity}%, wind speed of {windSpeed} km/h in {location}";
            Output.WriteLine(weatherReport);
            return weatherReport;
        }
    }
}
