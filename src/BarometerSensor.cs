using Tinkerforge;

namespace TinfluxWeatherStation
{
    public class BarometerSensor : ISensor
    {
        private const string SensorTyp = "BAROMETER";
        private const string SensorUnit = "hPa";
        private const string SensorUnitName = "Air Pressure";
        private static Station _tinfluxWeatherStation;

        public BarometerSensor(IPConnection ipConnection, string uid, int sensorCallbackPeriod, Station station)
        {
            _tinfluxWeatherStation = station;
            var bricklet = new BrickletBarometer(uid, ipConnection);
            // Register callback to function BrickletCb
            bricklet.AirPressureCallback += BrickletCb;

            // Note: The callback is only called every "sensorCallbackPeriod"
            // if the value has changed since the last call!
            bricklet.SetAirPressureCallbackPeriod(sensorCallbackPeriod);
        }

        // Callback function for bricklet callback
        private static async void BrickletCb(BrickletBarometer sender, int rawValue)
        {
            var value = CalculateValue(rawValue);
            _tinfluxWeatherStation.LastMeasuredAirPressure = value;
            await _tinfluxWeatherStation.WriteToInfluxDb(SensorTyp, SensorUnit, SensorUnitName, value);
        }

        private static double CalculateValue(int rawValue)
        {
            return (rawValue / 1000.0);
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