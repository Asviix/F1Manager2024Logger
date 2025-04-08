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
    [PluginDescription("F1Manager2024 Telemetry Plugin")]
    [PluginAuthor("Thomas DEFRANCE")]
    public class TelemetryPlugin : IPlugin, IDataPlugin
    {
        public PluginManager PluginManager { get; set; }
        public Thread udpListenerThread;
        public UdpClient udpClient;
        public bool running = false;

        public readonly Dictionary<string, object> latestValues = new Dictionary<string, object>();

        public void Init(PluginManager pluginManager)
        {
            this.PluginManager = pluginManager;

            pluginManager.AddProperty("F1MConnected", GetType(), typeof(bool), "Is F1Manager connected");

            // Add properties for session + weather
            pluginManager.AddProperty("SessionType", GetType(), typeof(string), "Current session");
            pluginManager.AddProperty("Weather", GetType(), typeof(string), "Current weather");

            // Add properties for all 22 drivers
            string[] carNames = new string[]
            {
                "Ferrari1","Ferrari2","RedBull1","RedBull2","Mercedes1","Mercedes2",
                "McLaren1","McLaren2","AstonMartin1","AstonMartin2","Alpine1","Alpine2",
                "AlphaTauri1","AlphaTauri2","AlfaRomeo1","AlfaRomeo2","Haas1","Haas2",
                "Williams1","Williams2","MyTeam1","MyTeam2"
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

                            latestValues[$"{name}_Position"] = (int?)info["position"] ?? 99;
                        }
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