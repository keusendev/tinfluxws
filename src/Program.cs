using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace TinfluxWeatherStation
{
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
    internal static class Program
    {
        private static string _stationName;
        private static string _masterBrickHost;
        private static int _masterBrickTcpPort;
        private static int _callbackPeriod;
        private static int _altitudeOffset;
        private static string _influxDbHostUri;
        private static string _influxDbName;
        private static string _influxDbUser;
        private static string _influxDbPasswd;


        private static void Main(string[] args)
        {
            var argsLenght = args.Length;
            switch (argsLenght)
            {
                case 0:
                    Console.WriteLine("Starting program with ENV...");
                    _stationName = Environment.GetEnvironmentVariable("TINFLUXWS_STATIONNAME");
                    _masterBrickHost = Environment.GetEnvironmentVariable("TINFLUXWS_MASTERBRICK_HOST");

                    var masterBrickTcpPort = Environment.GetEnvironmentVariable("TINFLUXWS_MASTERBRICK_PORT");
                    _masterBrickTcpPort = masterBrickTcpPort != null ? int.Parse(masterBrickTcpPort) : 0;

                    var callbackPeriod = Environment.GetEnvironmentVariable("TINFLUXWS_CALLBACKPERIOD");
                    _callbackPeriod = callbackPeriod != null ? int.Parse(callbackPeriod) : -1;

                    var altitudeOffset = Environment.GetEnvironmentVariable("TINFLUXWS_ALTITUDEOFFSET");
                    _altitudeOffset = altitudeOffset != null ? int.Parse(altitudeOffset) : 0;

                    _influxDbHostUri = Environment.GetEnvironmentVariable("TINFLUXWS_INFLUXDB_HOST_URI");
                    _influxDbName = Environment.GetEnvironmentVariable("TINFLUXWS_INFLUXDB_NAME");

                    if (_influxDbName == null || _influxDbHostUri == null || _masterBrickHost == null ||
                        _stationName == null || _stationName.Length < 1 || _masterBrickHost.Length < 1 ||
                        _masterBrickTcpPort == 0 || _influxDbHostUri.Length < 1 ||
                        _influxDbName.Length < 1)
                    {
                        Console.WriteLine("Not all ENV are set!");
                        break;
                    }

                    const string usernameSecretFile = @"/run/secrets/influxuser";
                    _influxDbUser = ReadSecretFile(usernameSecretFile);

                    const string passwordSecretFile = @"/run/secrets/influxpassword";
                    _influxDbPasswd = ReadSecretFile(passwordSecretFile);

                    if (_influxDbUser == null)
                        _influxDbUser = Environment.GetEnvironmentVariable("TINFLUXWS_INFLUXDB_USER");
                    if (_influxDbPasswd == null)
                        _influxDbPasswd = Environment.GetEnvironmentVariable("TINFLUXWS_INFLUXDB_PASSWD");

                    if (_influxDbPasswd != null &&
                        (_influxDbUser != null && (_influxDbUser.Length < 1 || _influxDbPasswd.Length < 1)))
                    {
                        Console.WriteLine("Not all secrets or ENV are set!");
                        break;
                    }

                    StartStation();
                    break;

                case 7:
                    Console.WriteLine("Starting program with CLI args...");
                    _stationName = args[0];
                    _masterBrickHost = args[1];
                    _masterBrickTcpPort = int.Parse(args[2]);
                    _influxDbHostUri = args[3];
                    _influxDbName = args[4];
                    _influxDbUser = args[5];
                    _influxDbPasswd = args[6];
                    StartStation();
                    break;

                case 8:
                    Console.WriteLine("Starting program with CLI args...");
                    _stationName = args[0];
                    _masterBrickHost = args[1];
                    _masterBrickTcpPort = int.Parse(args[2]);
                    _influxDbHostUri = args[3];
                    _influxDbName = args[4];
                    _influxDbUser = args[5];
                    _influxDbPasswd = args[6];
                    _callbackPeriod = int.Parse(args[7]);
                    StartStation();
                    break;

                case 9:
                    Console.WriteLine("Starting program with CLI args...");
                    _stationName = args[0];
                    _masterBrickHost = args[1];
                    _masterBrickTcpPort = int.Parse(args[2]);
                    _influxDbHostUri = args[3];
                    _influxDbName = args[4];
                    _influxDbUser = args[5];
                    _influxDbPasswd = args[6];
                    _callbackPeriod = int.Parse(args[7]);
                    _altitudeOffset = int.Parse(args[8]);
                    StartStation();
                    break;

                default:
                    Console.WriteLine("Arguments not OK!");
                    break;
            }
        }

        private static void PrintSettings()
        {
            var passwdStars = string.Concat(Enumerable.Repeat("*", _influxDbPasswd.Length));

            Console.WriteLine("");
            Console.WriteLine($"Station name:      {_stationName}");
            Console.WriteLine($"Master Brick Host: {_masterBrickHost}:{_masterBrickTcpPort.ToString()}");
            Console.WriteLine($"InfluxDB Server:   {_influxDbHostUri}");
            Console.WriteLine($"InfluxDB User:     {_influxDbUser}");
            Console.WriteLine($"InflusDB Passwd:   {passwdStars}");
            Console.WriteLine($"Callback period:   {_callbackPeriod.ToString()}sec");
            Console.WriteLine($"Altitude Offset:   {_altitudeOffset.ToString()}m");
            Console.WriteLine("");
        }

        private static string ReadSecretFile(string curFile)
        {
            if (!File.Exists(curFile) || new FileInfo(curFile).Length == 0) return null;
            var lines = File.ReadAllLines(curFile);
            return lines[0];
        }

        private static void StartStation()
        {
            PrintSettings();
            var station = new Station(_masterBrickHost, _masterBrickTcpPort, _stationName, _influxDbHostUri,
                _influxDbName, _influxDbUser, _influxDbPasswd, _callbackPeriod, _altitudeOffset);

            station.SetupAndStartStation();
        }
    }
}