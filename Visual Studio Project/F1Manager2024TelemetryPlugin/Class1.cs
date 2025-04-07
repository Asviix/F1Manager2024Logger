using SimHub.Plugins;
using GameReaderCommon;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

[PluginDescription("F1 Manager 2024 UDP Telemetry Plugin")]
[PluginAuthor("YourName")]
[PluginName("F1Manager2024TelemetryPlugin")]
public class TelemetryPlugin : IPlugin
{
    private UdpClient _udpClient;
    private Thread _listenerThread;
    private bool _isRunning;

    public void Init(PluginManager pluginManager)
    {
        pluginManager.AddProperty("F1Manager2024", this);
        StartUdpListener(pluginManager, 4739);
    }

    public void End(PluginManager pluginManager)
    {
        _isRunning = false;
        _udpClient?.Close();
        _listenerThread?.Join();
    }

    private void StartUdpListener(PluginManager pluginManager, int port)
    {
        _udpClient = new UdpClient(port);
        _isRunning = true;

        _listenerThread = new Thread(() =>
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);
            while (_isRunning)
            {
                try
                {
                    byte[] bytes = _udpClient.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(bytes);

                    string[] lines = message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(':');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string valueStr = parts[1].Trim();

                            object value = double.TryParse(valueStr, out double v) ? v : valueStr;

                            // This creates a dynamic property under [F1Manager2024.{key}]
                            pluginManager.SetPropertyValue($"F1Manager2024.{key}", value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // You can log exceptions to the SimHub log folder if needed
                    System.Diagnostics.Debug.WriteLine($"[F1Manager2024Plugin] Error: {ex.Message}");
                }
            }
        });

        _listenerThread.IsBackground = true;
        _listenerThread.Start();
    }
}