using Tinkerforge;

namespace TinfluxWeatherStation
{
    public class HumiditySensor : ISensor
    {
        private const string SensorTyp = "HUMIDITY";
        private const string SensorUnit = "%RH";
        private const string SensorUnitName = "RELATIVE HUMIDIT";
        private static Station _tinfluxWeatherStation;

        public HumiditySensor(IPConnection ipConnection, string uid, int sensorCallbackPeriod, Station station)
        {
            _tinfluxWeatherStation = station;
            var bricklet = new BrickletHumidity(uid, ipConnection);
            // Register callback to function BrickletCb
            bricklet.HumidityCallback += BrickletCb;

            // Note: The callback is only called every "sensorCallbackPeriod"
            // if the value has changed since the last call!
            bricklet.SetHumidityCallbackPeriod(sensorCallbackPeriod);
        }

        // Callback function for bricklet callback
        private static async void BrickletCb(BrickletHumidity sender, int rawValue)
        {
            var value = CalculateValue(rawValue);
            _tinfluxWeatherStation.LastMeasuredHumidity = value;
            await _tinfluxWeatherStation.WriteToInfluxDb(SensorTyp, SensorUnit, SensorUnitName, value);
        }

        private static double CalculateValue(int rawValue)
        {
            return (rawValue / 10.0);
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