using Tinkerforge;

namespace TinfluxWeatherStation
{
    /*
     * The UVSensor class 
     * Wraps the Tinkerforge Temperature Bricklet class.
     */
    public class UVLightSensor : ISensor
    {
        private const string SensorTyp = "UV";
        private const string SensorUnit = "μW/cm²";
        private const string SensorUnitName = "MICROWATTS PER SQUARE CEMETER";
        private static Station _tinfluxWeatherStation;

        public UVLightSensor(IPConnection ipConnection, string uid, int sensorCallbackPeriod, Station station)
        {
            _tinfluxWeatherStation = station;
            var bricklet = new BrickletUVLight(uid, ipConnection);
            // Register callback to function BrickletCb
            bricklet.UVLightCallback += BrickletCb;

            // Note: The callback is only called every "sensorCallbackPeriod"
            // if the value has changed since the last call!
            bricklet.SetUVLightCallbackPeriod(sensorCallbackPeriod);
        }

        // Callback function for bricklet callback
        private static async void BrickletCb(BrickletUVLight sender, long rawValue)
        {
            var value = CalculateValue(rawValue);
            _tinfluxWeatherStation.LastMeasuredUVLight = value;
            await _tinfluxWeatherStation.WriteToInfluxDb(SensorTyp, SensorUnit, SensorUnitName, value);
        }

        private static double CalculateValue(long rawValue)
        {
            //return (rawValue / 100.0);
            return (rawValue);
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