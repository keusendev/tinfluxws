using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using Tinkerforge;

namespace TinfluxWeatherStation
{
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
    public class Station
    {
        private readonly List<ISensor> _sensors;
        private static LineProtocolClient _influxClient;
        private string StationName { get; }
        private IPConnection _ipConnection;
        private readonly string _masterBrickHost;
        private readonly int _masterBrickPort;
        public int Callbackperiod { get; private set; }


        public Station(string masterBrickHost, int masterBrickPort, string stationName,
            string influxUri, string influxDb, string influxUser, string influxPassword, int callbackperiod)
        {
            _masterBrickHost = masterBrickHost;
            _masterBrickPort = masterBrickPort;
            StationName = stationName;
            _influxClient = new LineProtocolClient(new Uri(influxUri), influxDb, influxUser, influxPassword);
            _sensors = new List<ISensor>();
            if (callbackperiod == -1)
            {
                Callbackperiod = 5000;
            }
            else
            {
                Callbackperiod = callbackperiod * 1000;
            }
        }

        private void EnumerateCb(IPConnection sender, string uid, string connectedUid, char position,
            short[] hardwareVersion, short[] firmwareVersion, int deviceIdentifier, short enumerationType)
        {
            if (enumerationType == IPConnection.ENUMERATION_TYPE_DISCONNECTED)
            {
                Console.WriteLine("No IP connection!");
                return;
            }

            if (deviceIdentifier == BrickletTemperature.DEVICE_IDENTIFIER)
            {
                _sensors.Add(new TemperatureSensor(_ipConnection, uid, Callbackperiod, this));
            }

            if (deviceIdentifier == BrickletHumidity.DEVICE_IDENTIFIER)
            {
                _sensors.Add(new HumiditySensor(_ipConnection, uid, Callbackperiod, this));
            }

            if (deviceIdentifier == BrickletBarometer.DEVICE_IDENTIFIER)
            {
                _sensors.Add(new BarometerSensor(_ipConnection, uid, Callbackperiod, this));
            }
        }

        private void EnumerateSensores()
        {
            // Register Enumerate Callback
            _ipConnection.EnumerateCallback += EnumerateCb;

            // Trigger Enumerate
            _ipConnection.Enumerate();
            Thread.Sleep(1000); //Give Tinkerforge time to enumerate
        }

        public void SetupAndStartStation()
        {
            if (EstablishMasterBrickConnection())
            {
                EnumerateSensores();
                PrintAllEnumeratedSensors();
                Console.WriteLine($"Application with Station \"{StationName}\" is started...");
                Thread.Sleep(Timeout.Infinite);
            }
            else
            {
                Console.WriteLine("Not able to connect to MasterBrick! Check your connectivity!");
            }
        }

        private void PrintAllEnumeratedSensors()
        {
            foreach (var sensor in _sensors)
            {
                Console.WriteLine(
                    $"{sensor.SensorTyp()} Sensor measures {sensor.SensorUnitName()} ({sensor.SensorUnit()})");
            }
        }

        public async Task WriteToInfluxDb(string sensorTyp, string sensorUnit, string sensorUnitName,
            double sensorValue)
        {
            Console.WriteLine($"{sensorTyp}: {sensorValue.ToString(CultureInfo.CurrentCulture)} {sensorUnit}");

            var payload = new LineProtocolPayload();
            var point = new LineProtocolPoint(StationName,
                new Dictionary<string, object>
                {
                    // ReSharper disable once HeapView.BoxingAllocation
                    {"value", sensorValue},
                },
                new Dictionary<string, string>
                {
                    {"type", sensorTyp},
                    {"unit", sensorUnit},
                    {"unitName", sensorUnitName}
                },
                DateTime.UtcNow);
            payload.Add(point);
            var influxResult = await _influxClient.WriteAsync(payload);
            if (!influxResult.Success)
                Console.Error.WriteLine($"Problem while writing to INfluxDB \n{influxResult.ErrorMessage}");
        }

        private bool EstablishMasterBrickConnection()
        {
            if (_ipConnection == null)
            {
                _ipConnection = new IPConnection();
            }

            var retries = 0;
            while ((_ipConnection.GetConnectionState() != IPConnection.CONNECTION_STATE_CONNECTED) && retries < 3)
            {
                retries++;
                try
                {
                    _ipConnection.Connect(_masterBrickHost, _masterBrickPort);
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Caught a Tinkerforge Exception ({e.Message}). Try number {retries.ToString()}");
                }

                if (retries > 1)
                {
                    Thread.Sleep(1500);
                }
            }

            return false;
        }

        public override string ToString()
        {
            return StationName;
        }
    }
}