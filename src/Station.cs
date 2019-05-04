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
        private int Callbackperiod { get; }
        private int AltitudeOffset { get; }

        public double LastMeasuredTemperature { private get; set; } = -1000;
        public double LastMeasuredMoisture { private get; set; } = -1000;
        public double LastMeasuredHumidity { private get; set; } = -1000;
        public double LastMeasuredAirPressure { private get; set; } = -1000;
        public double LastMeasuredUVLight { private get; set; } = -1000;


        public Station(string masterBrickHost, int masterBrickPort, string stationName,
            string influxUri, string influxDb, string influxUser, string influxPassword, int callbackperiod,
            int altitudeOffset = 0)
        {
            _masterBrickHost = masterBrickHost;
            _masterBrickPort = masterBrickPort;
            StationName = stationName;
            _influxClient = new LineProtocolClient(new Uri(influxUri), influxDb, influxUser, influxPassword);
            _sensors = new List<ISensor>();
            AltitudeOffset = altitudeOffset;
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
            
            if (deviceIdentifier == BrickletUVLight.DEVICE_IDENTIFIER)
            {
                _sensors.Add(new UVLightSensor(_ipConnection, uid, Callbackperiod, this));
            }

            if (deviceIdentifier == BrickletMoisture.DEVICE_IDENTIFIER)
            {
                _sensors.Add(new MoistureSensor(_ipConnection, uid, Callbackperiod, this));
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

                Task.Run(() => StartAirCalculationWorker());

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
                    {"value", sensorValue}
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


        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        private async Task StartAirCalculationWorker()
        {
            while (true)
            {
                Console.WriteLine("Worker is running");
                if (Math.Abs(LastMeasuredAirPressure - (-1000)) > 10 &&
                    Math.Abs(LastMeasuredHumidity - (-1000)) > 10 &&
                    Math.Abs(LastMeasuredTemperature - (-1000)) > 10)
                {
                    await CalculateAndWriteAirRelatedStuff(
                        LastMeasuredTemperature,
                        LastMeasuredHumidity,
                        LastMeasuredAirPressure);
                }

                Thread.Sleep(Callbackperiod + 5000);
            }
        }

        public override string ToString()
        {
            return StationName;
        }

        private static double RoundToThreeSig(double value)
        {
            return Math.Round(value, 3, MidpointRounding.AwayFromZero);
        }

        /*
         * @temperateure        temperature in degree celsius
         * @relativeHumidity    relative humidity e.g. 50.3 (value between 0 and 100)
         * @airPressure         pressure in hektopascal (hPa)
         * @return              returns Dense water vapor [g/m³]
         *                        & Dense dry air [g/m³]
         *                        & Specific humidity [g/kg]
         *                        & dew point temperature [°C] in a double array.
         */
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
        private async Task CalculateAndWriteAirRelatedStuff(double temperature, double relativeHumidity,
            double airPressure)
        {
            /* Unit declation
             * T (Temperature):    Kelvin [K] or Degree Celsius [°C]
             * p (Pressure):       Pascal [Pa]
             * wv:                 Water vapor
             * da:                 Dry air
             * ha:                 Humid air
             */
            double T_c = temperature; // Temperature [°C]
            double T = 273.15 + T_c; // Temperature [K]
            double p = 100.0 * airPressure; // Pressure [Pa]
            double phi = relativeHumidity / 100.0;
            double phi_max = 1.0;

            // Saturation vapor pressure over water [Pa] (Magnus-Formula)
            const double magnus_coefficient = 611.2; // [Pa]
            double e_sat_w = magnus_coefficient * Math.Exp((17.62 * T_c) / (243.12 + T_c));

            double e = phi * e_sat_w; // Water vapor partial pressure [Pa]
            double e_phimax = phi_max * e_sat_w; // Water vapor partial pressure [Pa]
            const double R_wv = 461.51; // Gas constant water [J/(kg*K]
            const double R_da = 287.058; // Gas constant dry air [J/(kg*K]

            double rho_wv = (e / (R_wv * T)); // Dense water vapor [kg/m³]
            double rho_da = ((p - e) / (R_da * T)); // Dense dry air [kg/m³]
            double rho_ha = rho_wv + rho_da; // Dense dry air [kg/m³]
            double x = (rho_wv / rho_ha); // Specific humidity [g/kg]

            // Calculate if phi = 1 -> Max saturation of water vapor in the air
            double rho_wv_phimax = (e_phimax / (R_wv * T)); // Dense water vapor [kg/m³]
            double rho_da_phimax = ((p - e_phimax) / (R_da * T)); // Dense dry air [kg/m³]
            double rho_ha_phimax = rho_wv_phimax + rho_da_phimax; // Dense dry air [kg/m³]
            double x_s = (rho_wv_phimax / rho_ha_phimax); // Specific humidity [g/kg]

            // dew point temperature [°C]
            double ln1 = Math.Log(e / magnus_coefficient);
            double ln2 = -Math.Log(e / magnus_coefficient) + 17.62;
            double t = 243.12 * (ln1 / ln2);

            double m = (rho_wv / rho_da); // Mixing ratio - moisture level

            const double p_0 = 101325.0; // Pressure at sealevel [Pa] 
            const double temperatureGradients = 5.255;
            const double a = 0.0065; // Vertical temperature gradient [K/m]
            double h = (Math.Pow((p_0 / p), (1 / temperatureGradients)) - 1) * (T / a);

            var allResults =
                new Dictionary<string, Dictionary<string, object>>
                {
                    {
                        "e_sat_w", new Dictionary<string, object>
                        {
                            {"name", "Saturation vapor pressure"},
                            {"unit", "hPa"},
                            {"value", RoundToThreeSig(e_sat_w / 100)}
                        }
                    },
                    {
                        "e", new Dictionary<string, object>
                        {
                            {"name", "Water vapor partial pressure"},
                            {"unit", "hPa"},
                            {"value", RoundToThreeSig(e / 100)}
                        }
                    },
                    {
                        "x", new Dictionary<string, object>
                        {
                            {"name", "Specific humidity"},
                            {"unit", "g water/kg humid air"},
                            {"value", RoundToThreeSig(x * 1000)}
                        }
                    },
                    {
                        "x_s", new Dictionary<string, object>
                        {
                            {"name", "Max specific humidity"},
                            {"unit", "g water/kg humid air"},
                            {"value", RoundToThreeSig(x_s * 1000)}
                        }
                    },
                    {
                        "m", new Dictionary<string, object>
                        {
                            {"name", "Mixing ratio - moisture level"},
                            {"unit", "g water/kg dry air"},
                            {"value", RoundToThreeSig(m * 1000)}
                        }
                    },
                    {
                        "rho_wv", new Dictionary<string, object>
                        {
                            {"name", "Dense water vapor"},
                            {"unit", "g/m³"},
                            {"value", RoundToThreeSig(rho_wv * 1000)}
                        }
                    },
                    {
                        "rho_da", new Dictionary<string, object>
                        {
                            {"name", "Dense dry air"},
                            {"unit", "g/m³"},
                            {"value", RoundToThreeSig(rho_da * 1000)}
                        }
                    },
                    {
                        "rho_ha", new Dictionary<string, object>
                        {
                            {"name", "Dense humid air"},
                            {"unit", "g/m³"},
                            {"value", RoundToThreeSig(rho_ha * 1000)}
                        }
                    },
                    {
                        "t", new Dictionary<string, object>
                        {
                            {"name", "Dew point temperature"},
                            {"unit", "°C"},
                            {"value", RoundToThreeSig(t)}
                        }
                    },
                    {
                        "ah", new Dictionary<string, object>
                        {
                            {"name", "Absolute humidity"},
                            {"unit", "g/m³"},
                            {"value", RoundToThreeSig(rho_wv * 1000)}
                        }
                    },
                    {
                        "h", new Dictionary<string, object>
                        {
                            {"name", "Altitude"},
                            {"unit", "m"},
                            {"value", RoundToThreeSig(h + AltitudeOffset)}
                        }
                    }
                };

            const string sensorTyp = "CALCULATOR";
            foreach (var entry in allResults)
            {
                Console.WriteLine($"{entry.Value["name"]}: {entry.Value["value"]}{entry.Value["unit"]}");
                await WriteToInfluxDb(sensorTyp, (string) entry.Value["unit"], (string) entry.Value["name"],
                    (double) entry.Value["value"]);
            }
        }
    }
}