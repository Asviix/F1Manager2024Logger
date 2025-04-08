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
                pluginManager.AddProperty($"{name}_Position", GetType(), typeof(int), "Position");
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
                                latestValues[$"{name}_Position"] = (int?)info["position"] + 1 ?? 99; // Adjusted to 1-based index
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
            catch
            {
                // Ignore malformed packets
            }
        }
    }
}