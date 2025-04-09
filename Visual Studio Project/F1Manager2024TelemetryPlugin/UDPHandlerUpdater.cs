using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace F1Manager2024TelemetryPlugin
{
    [PluginName("F1 Manager 2024 Telemetry Plugin")]
    [PluginDescription("Extracts data from the UDP Created by the Python Script, and makes it available in SimHub Studio")]
    [PluginAuthor("Thomas DEFRANCE")]
    public class TelemetryPlugin : IPlugin, IDataPlugin
    {
        public PluginManager? PluginManager { get; set; }
        public Thread? udpListenerThread;
        public UdpClient? udpClient;
        public bool running = false;

        public readonly Dictionary<string, object> latestValues = new Dictionary<string, object>();

        public void Init(PluginManager pluginManager)
        {
            this.PluginManager = pluginManager;

            pluginManager.AddProperty("F1MConnected", GetType(), typeof(bool), "Is F1Manager connected");

            // Add session properties
            pluginManager.AddProperty("timeElapsed", GetType(), typeof(float), "Time Elapsed");
            pluginManager.AddProperty("trackName", GetType(), typeof(string), "Track Name");
            pluginManager.AddProperty("bestSessionTime", GetType(), typeof(float), "Best Session Time");
            pluginManager.AddProperty("rubberState", GetType(), typeof(float), "Rubber State");
            pluginManager.AddProperty("airTemp", GetType(), typeof(float), "Air Temperature");
            pluginManager.AddProperty("trackTemp", GetType(), typeof(float), "Track Temperature");
            pluginManager.AddProperty("weather", GetType(), typeof(string), "Weather");

            // Add properties for all 22 drivers
            string[] carNames = new string[]
            {
                "Ferrari1", "Ferrari2",
                "McLaren1", "McLaren2",
                "RedBull1", "RedBull2",
                "Mercedes1", "Mercedes2",
                "Alpine1", "Alpine2",
                "Williams1", "Williams2",
                "Haas1", "Haas2",
                "RacingBulls1", "RacingBulls2",
                "KickSauber1", "KickSauber2",
                "AstonMartin1", "AstonMartin2",
                "MyTeam1", "MyTeam2"
            };

            foreach (var name in carNames)
            {
                // Position and basic info
                pluginManager.AddProperty($"{name}_Position", GetType(), typeof(int), "Position");
                pluginManager.AddProperty($"{name}_DriverNumber", GetType(), typeof(int), "Driver Number");
                pluginManager.AddProperty($"{name}_PitStopStatus", GetType(), typeof(string), "Pit Stop Status");
    
                // Status
                pluginManager.AddProperty($"{name}_TurnNumber", GetType(), typeof(int), "Turn Number");
                pluginManager.AddProperty($"{name}_CurrentLap", GetType(), typeof(int), "Current Lap");
    
                // Timings
                pluginManager.AddProperty($"{name}_CurrentLapTime", GetType(), typeof(float), "Current Lap Time");
                pluginManager.AddProperty($"{name}_DriverBestLap", GetType(), typeof(float), "Driver Best Lap");
                pluginManager.AddProperty($"{name}_LastLapTime", GetType(), typeof(float), "Last Lap Time");
                pluginManager.AddProperty($"{name}_LastS1Time", GetType(), typeof(float), "Last Sector 1 Time");
                pluginManager.AddProperty($"{name}_LastS2Time", GetType(), typeof(float), "Last Sector 2 Time");
                pluginManager.AddProperty($"{name}_LastS3Time", GetType(), typeof(float), "Last Sector 3 Time");
    
                // Car telemetry
                pluginManager.AddProperty($"{name}_Speed", GetType(), typeof(int), "Speed (km/h)");
                pluginManager.AddProperty($"{name}_Rpm", GetType(), typeof(int), "RPM");
                pluginManager.AddProperty($"{name}_Gear", GetType(), typeof(int), "Gear");
                pluginManager.AddProperty($"{name}_Charge", GetType(), typeof(float), "ERS Charge");
                pluginManager.AddProperty($"{name}_Fuel", GetType(), typeof(float), "Fuel");
    
                // Tyres
                pluginManager.AddProperty($"{name}_TyreCompound", GetType(), typeof(string), "Tyre Compound");
                pluginManager.AddProperty($"{name}_flTemp", GetType(), typeof(float), "Front Left Temp");
                pluginManager.AddProperty($"{name}_frTemp", GetType(), typeof(float), "Front Right Temp");
                pluginManager.AddProperty($"{name}_rlTemp", GetType(), typeof(float), "Rear Left Temp");
                pluginManager.AddProperty($"{name}_rrTemp", GetType(), typeof(float), "Rear Right Temp");
                pluginManager.AddProperty($"{name}_flDeg", GetType(), typeof(float), "Front Left Wear");
                pluginManager.AddProperty($"{name}_frDeg", GetType(), typeof(float), "Front Right Wear");
                pluginManager.AddProperty($"{name}_rlDeg", GetType(), typeof(float), "Rear Left Wear");
                pluginManager.AddProperty($"{name}_rrDeg", GetType(), typeof(float), "Rear Right Wear");
    
                // Modes
                pluginManager.AddProperty($"{name}_PaceMode", GetType(), typeof(string), "Pace Mode");
                pluginManager.AddProperty($"{name}_FuelMode", GetType(), typeof(string), "Fuel Mode");
                pluginManager.AddProperty($"{name}_ERSMode", GetType(), typeof(string), "ERS Mode");
                pluginManager.AddProperty($"{name}_DRSMode", GetType(), typeof(string), "DRS Mode");
    
                // Components
                pluginManager.AddProperty($"{name}_EngineTemp", GetType(), typeof(float), "Engine Temp");
                pluginManager.AddProperty($"{name}_EngineDeg", GetType(), typeof(float), "Engine Wear");
                pluginManager.AddProperty($"{name}_GearboxDeg", GetType(), typeof(float), "Gearbox Wear");
                pluginManager.AddProperty($"{name}_ERSDeg", GetType(), typeof(float), "ERS Wear");
}

            // Start UDP listener
            StartUdpListener();
        }

        public void End(PluginManager pluginManager)
        {
            running = false;
            udpClient?.Close();
            udpListenerThread?.Join();
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            lock (latestValues)
            {
                foreach (var kvp in latestValues)
                {
                    pluginManager.SetPropertyValue<TelemetryPlugin>(kvp.Key, kvp.Value);
                }
            }
        }

        public void StartUdpListener()
        {
            running = true;
            udpListenerThread = new Thread(() =>
            {
                try
                {
                    udpClient = new UdpClient(20777); // Default port
                    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                    while (running)
                    {
                        byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                        string json = Encoding.UTF8.GetString(receivedBytes);
                        ParseJson(json);
                    }
                }
                catch (Exception ex)
                {
                    PluginManager?.AddProperty("F1MListenerError", GetType(), typeof(string), "Listener Error");
                    PluginManager?.SetPropertyValue<TelemetryPlugin>("F1MListenerError", ex.Message);
                }
            });

            udpListenerThread.IsBackground = true;
            udpListenerThread.Start();
        }

		public void ParseJson(string json)
		{
			try
			{
				var parsed = JObject.Parse(json);

				lock (latestValues)
				{
					latestValues["F1MConnected"] = true;

					var cars = parsed["cars"] as JObject;
					if (cars != null)
					{
						foreach (var car in cars)
						{
							string name = car.Key;
							var info = car.Value;

							if (info != null)
							{
								// Position and basic info
								latestValues[$"{name}_Position"] = (int?)info["position"] + 1 ?? 0;
								latestValues[$"{name}_DriverNumber"] = (int?)info["driverNumber"] ?? 0;
								latestValues[$"{name}_PitStopStatus"] = (string?)info["pitstopStatus"] ?? string.Empty;
								
								// Status
								latestValues[$"{name}_TurnNumber"] = (int?)info["turnNumber"] ?? 0;
								latestValues[$"{name}_CurrentLap"] = (int?)info["currentLap"] ?? 0;
								
								// Timings
								latestValues[$"{name}_CurrentLapTime"] = (float?)info["currentLapTime"] ?? 0f;
								latestValues[$"{name}_DriverBestLap"] = (float?)info["driverBestLap"] ?? 0f;
								latestValues[$"{name}_LastLapTime"] = (float?)info["lastLapTime"] ?? 0f;
								latestValues[$"{name}_LastS1Time"] = (float?)info["lastS1Time"] ?? 0f;
								latestValues[$"{name}_LastS2Time"] = (float?)info["lastS2Time"] ?? 0f;
								latestValues[$"{name}_LastS3Time"] = (float?)info["lastS3Time"] ?? 0f;
								
								// Car telemetry
								latestValues[$"{name}_Speed"] = (int?)info["speed"] ?? 0;
								latestValues[$"{name}_Rpm"] = (int?)info["rpm"] ?? 0;
								latestValues[$"{name}_Gear"] = (int?)info["gear"] ?? 0;
								latestValues[$"{name}_Charge"] = (float?)info["charge"] ?? 0f;
								latestValues[$"{name}_Fuel"] = (float?)info["fuel"] ?? 0f;
								
								// Tyres
								latestValues[$"{name}_TyreCompound"] = (string?)info["tyreCompound"] ?? string.Empty;
								latestValues[$"{name}_flTemp"] = (float?)info["flTemp"] ?? 0f;
								latestValues[$"{name}_frTemp"] = (float?)info["frTemp"] ?? 0f;
								latestValues[$"{name}_rlTemp"] = (float?)info["rlTemp"] ?? 0f;
								latestValues[$"{name}_rrTemp"] = (float?)info["rrTemp"] ?? 0f;
								latestValues[$"{name}_flDeg"] = (float?)info["flDeg"] ?? 0f;
								latestValues[$"{name}_frDeg"] = (float?)info["frDeg"] ?? 0f;
								latestValues[$"{name}_rlDeg"] = (float?)info["rlDeg"] ?? 0f;
								latestValues[$"{name}_rrDeg"] = (float?)info["rrDeg"] ?? 0f;
								
								// Modes
								latestValues[$"{name}_PaceMode"] = (string?)info["paceMode"] ?? string.Empty;
								latestValues[$"{name}_FuelMode"] = (string?)info["fuelMode"] ?? string.Empty;
								latestValues[$"{name}_ERSMode"] = (string?)info["ersMode"] ?? string.Empty;
								latestValues[$"{name}_DRSMode"] = (string?)info["drsMode"] ?? string.Empty;
								
								// Components
								latestValues[$"{name}_EngineTemp"] = (float?)info["engineTemp"] ?? 0f;
								latestValues[$"{name}_EngineDeg"] = (float?)info["engineDeg"] ?? 0f;
								latestValues[$"{name}_GearboxDeg"] = (float?)info["gearboxDeg"] ?? 0f;
								latestValues[$"{name}_ERSDeg"] = (float?)info["ersDeg"] ?? 0f;
							}
						}
					}

					var session = parsed["session"] as JObject;
					if (session != null)
					{
                        latestValues["timeElapsed"] = (float?)session["timeElapsed"] ?? 0f;
                        latestValues["trackName"] = (string?)session["trackName"] ?? string.Empty;
                        latestValues["bestSessionTime"] = (float?)session["bestSessionTime"] ?? 0f;
                        latestValues["rubberState"] = (float?)session["rubberState"] ?? 0f;
                        latestValues["airTemp"] = (float?)session["airTemp"] ?? 0f;
                        latestValues["trackTemp"] = (float?)session["trackTemp"] ?? 0f;
                        latestValues["weather"] = (string?)session["weather"] ?? string.Empty;
					}
				}
			}
			catch (Exception ex)
			{
				PluginManager?.SetPropertyValue<TelemetryPlugin>("F1MListenerError", ex.Message);
			}
		}
    }
}