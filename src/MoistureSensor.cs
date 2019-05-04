using Tinkerforge;

namespace TinfluxWeatherStation
{
    /*
     * The MoistureSensor class
     * Wraps the Tinkerforge Moisture Bricklet class.
     */
    public class MoistureSensor : ISensor
    {
        private const string SensorTyp = "MOISTURE";
        private const string SensorUnit = "MOISTURE";
        private const string SensorUnitName = "MOISTURE";
        private static Station _tinfluxWeatherStation;

        public MoistureSensor(IPConnection ipConnection, string uid, int sensorCallbackPeriod, Station station)
        {
            _tinfluxWeatherStation = station;
            var bricklet = new BrickletMoisture(uid, ipConnection);
            // Register callback to function BrickletCb
            bricklet.MoistureCallback += BrickletCb;

            // Note: The callback is only called every "sensorCallbackPeriod"
            // if the value has changed since the last call!
            bricklet.SetMoistureCallbackPeriod(sensorCallbackPeriod);
        }

        // Callback function for bricklet callback
        private static async void BrickletCb(BrickletMoisture sender, int rawValue)
        {
            var value = CalculateValue(rawValue);
            _tinfluxWeatherStation.LastMeasuredMoisture = value;
            await _tinfluxWeatherStation.WriteToInfluxDb(SensorTyp, SensorUnit, SensorUnitName, value);
        }

        private static double CalculateValue(int rawValue)
        {
            return (rawValue / 1.0);
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