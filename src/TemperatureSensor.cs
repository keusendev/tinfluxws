using System.Diagnostics;
using Tinkerforge;

namespace TinfluxWeatherStation
{
    /*
     * The TemperatureSensor class
     * Wraps the Tinkerforge Temperature Bricklet class.
     */
    public class TemperatureSensor : ISensor
    {
        private const string SensorTyp = "TEMPERATURE";
        private const string SensorUnit = "°C";
        private const string SensorUnitName = "DEGREE CELSIUS";
        private static Station _tinfluxWeatherStation;

        public TemperatureSensor(IPConnection ipConnection, string uid, int sensorCallbackPeriod, Station station)
        {
            _tinfluxWeatherStation = station;
            var bricklet = new BrickletTemperature(uid, ipConnection);
            // Register callback to function BrickletCb
            bricklet.TemperatureCallback += BrickletCb;

            // Note: The callback is only called every "sensorCallbackPeriod"
            // if the value has changed since the last call!
            bricklet.SetTemperatureCallbackPeriod(sensorCallbackPeriod);
        }

        // Callback function for bricklet callback
        private static async void BrickletCb(BrickletTemperature sender, short rawValue)
        {
            var value = CalculateValue(rawValue);

            Debug.Assert(_tinfluxWeatherStation != null, nameof(_tinfluxWeatherStation) + " != null");
            await _tinfluxWeatherStation.WriteToInfluxDb(SensorTyp, SensorUnit, SensorUnitName, value);
        }

        private static double CalculateValue(short rawValue)
        {
            return (rawValue / 100.0);
        }

        string ISensor.SensorTyp()
        {
            return SensorTyp;
        }

        string ISensor.SensorUnit()
        {
            return SensorUnit;
        }

        string ISensor.SensorUnitName()
        {
            return SensorUnitName;
        }
    }
}