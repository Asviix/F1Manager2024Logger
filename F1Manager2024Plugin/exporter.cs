using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace F1Manager2024Plugin
{
    internal class Exporter
    {
        private string _exportDirectory;
        private Dictionary<string, string> _driverFilePaths = new Dictionary<string, string>();
        private Dictionary<string, bool> _headersWritten = new Dictionary<string, bool>();
        private F1Manager2024PluginSettings _settings;

        public Exporter(F1Manager2024PluginSettings settings)
        {
            _settings = settings;
            _exportDirectory = _settings.ExporterPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "F1ManagerTelemetry");

            Directory.CreateDirectory(_exportDirectory);
        }

        public void ExportData(dynamic data, string carName)
        {
            try
            {
                // Skip if exporter is disabled or driver not in tracked drivers
                if (!_settings.ExporterEnabled || !_settings.trackedDrivers.Contains(carName))
                    return;

                string trackName = data["MyTeam1"]?["telemetry"]?["session"]?["trackName"] ?? "UnknownTrack";
                string sessionType = data["MyTeam1"]?["telemetry"]?["session"]?["sessionType"] ?? "UnknownSession";

                string basePath = _settings.ExporterPath ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "F1ManagerTelemetry");

                string sessionFolder = Path.Combine(basePath, "exported_data", $"{trackName} {sessionType}");
                string carFolder = Path.Combine(sessionFolder, carName);

                Directory.CreateDirectory(carFolder);

                // Initialize file path for this driver if not exists
                if (!_driverFilePaths.ContainsKey(carName))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    _driverFilePaths[carName] = Path.Combine(carFolder, $"{carName}_Telemetry_{timestamp}.csv");
                    _headersWritten[carName] = false;
                }

                string filePath = _driverFilePaths[carName];
                bool headersWritten = _headersWritten[carName];


                var telemetryData = new Dictionary<string, object>
                {
                    // Session data
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ["trackName"] = data["MyTeam1"]?["telemetry"]?["session"]?["trackName"] ?? "",
                    ["sessionType"] = data["MyTeam1"]?["telemetry"]?["session"]?["sessionType"] ?? "",
                    ["timeElapsed"] = data["MyTeam1"]?["telemetry"]?["session"]?["timeElapsed"] ?? 0,

                    // Driver info
                    ["driverNumber"] = data[carName]?["telemetry"]?["driver"]?["driverNumber"] ?? 0,
                    ["pitstopStatus"] = data[carName]?["telemetry"]?["driver"]?["pitstopStatus"] ?? "",
                    ["currentLap"] = data[carName]?["telemetry"]?["driver"]?["status"]?["currentLap"] ?? 0,
                    ["turnNumber"] = data[carName]?["telemetry"]?["driver"]?["status"]?["turnNumber"] ?? 0,
                    ["position"] = data[carName]?["telemetry"]?["driver"]?["position"] ?? 0,

                    // Tyres
                    ["compound"] = data[carName]?["telemetry"]?["driver"]?["car"]?["tyres"]?["compound"] ?? "",
                    ["flTemp"] = data[carName]?["telemetry"]?["driver"]?["car"]?["tyres"]?["temperature"]?["flTemp"] ?? 0,
                    ["flDeg"] = data[carName]?["telemetry"]?["driver"]?["car"]?["tyres"]?["wear"]?["flDeg"] ?? 0,
                    ["frTemp"] = data[carName]?["telemetry"]?["driver"]?["car"]?["tyres"]?["temperature"]?["frTemp"] ?? 0,
                    ["frDeg"] = data[carName]?["telemetry"]?["driver"]?["car"]?["tyres"]?["wear"]?["frDeg"] ?? 0,
                    ["rlTemp"] = data[carName]?["telemetry"]?["driver"]?["car"]?["tyres"]?["temperature"]?["rlTemp"] ?? 0,
                    ["rlDeg"] = data[carName]?["telemetry"]?["driver"]?["car"]?["tyres"]?["wear"]?["rlDeg"] ?? 0,
                    ["rrTemp"] = data[carName]?["telemetry"]?["driver"]?["car"]?["tyres"]?["temperature"]?["rrTemp"] ?? 0,
                    ["rrDeg"] = data[carName]?["telemetry"]?["driver"]?["car"]?["tyres"]?["wear"]?["rrDeg"] ?? 0,

                    // Car telemetry
                    ["speed"] = data[carName]?["telemetry"]?["driver"]?["car"]?["speed"] ?? 0,
                    ["rpm"] = data[carName]?["telemetry"]?["driver"]?["car"]?["rpm"] ?? 0,
                    ["gear"] = data[carName]?["telemetry"]?["driver"]?["car"]?["gear"] ?? 0,

                    // Components
                    ["engineTemp"] = data[carName]?["telemetry"]?["driver"]?["car"]?["components"]?["engine"]?["engineTemp"] ?? 0,
                    ["engineDeg"] = data[carName]?["telemetry"]?["driver"]?["car"]?["components"]?["engine"]?["engineDeg"] ?? 0,
                    ["gearboxDeg"] = data[carName]?["telemetry"]?["driver"]?["car"]?["components"]?["gearbox"]?["gearboxDeg"] ?? 0,
                    ["ersDeg"] = data[carName]?["telemetry"]?["driver"]?["car"]?["components"]?["ers"]?["ersDeg"] ?? 0,

                    // Energy
                    ["charge"] = data[carName]?["telemetry"]?["driver"]?["car"]?["charge"] ?? 0,
                    ["fuel"] = data[carName]?["telemetry"]?["driver"]?["car"]?["fuel"] ?? 0,

                    // Modes
                    ["paceMode"] = data[carName]?["telemetry"]?["driver"]?["car"]?["modes"]?["paceMode"] ?? "",
                    ["fuelMode"] = data[carName]?["telemetry"]?["driver"]?["car"]?["modes"]?["fuelMode"] ?? "",
                    ["ersMode"] = data[carName]?["telemetry"]?["driver"]?["car"]?["modes"]?["ersMode"] ?? "",
                    ["drsMode"] = data[carName]?["telemetry"]?["driver"]?["car"]?["modes"]?["drsMode"] ?? "",

                    // Timings
                    ["currentLapTime"] = data[carName]?["telemetry"]?["driver"]?["timings"]?["currentLapTime"] ?? 0,
                    ["driverBestLap"] = data[carName]?["telemetry"]?["driver"]?["timings"]?["driverBestLap"] ?? 0,
                    ["lastLapTime"] = data[carName]?["telemetry"]?["driver"]?["timings"]?["lastLapTime"] ?? 0,
                    ["lastS1Time"] = data[carName]?["telemetry"]?["driver"]?["timings"]?["sectors"]?["lastS1Time"] ?? 0,
                    ["lastS2Time"] = data[carName]?["telemetry"]?["driver"]?["timings"]?["sectors"]?["lastS2Time"] ?? 0,
                    ["lastS3Time"] = data[carName]?["telemetry"]?["driver"]?["timings"]?["sectors"]?["lastS3Time"] ?? 0,

                    // Session info
                    ["bestSessionTime"] = data["MyTeam1"]?["telemetry"]?["session"]?["bestSessionTime"] ?? 0,
                    ["rubber"] = data["MyTeam1"]?["telemetry"]?["session"]?["rubber"] ?? 0,
                    ["airTemp"] = data["MyTeam1"]?["telemetry"]?["session"]?["weather"]?["airTemp"] ?? 0,
                    ["trackTemp"] = data["MyTeam1"]?["telemetry"]?["session"]?["weather"]?["trackTemp"] ?? 0,
                    ["weather"] = data["MyTeam1"]?["telemetry"]?["session"]?["weather"]?["weather"] ?? ""
                };

                // Write to CSV
                using (var writer = new StreamWriter(filePath, true))
                {
                    if (!headersWritten)
                    {
                        // Write headers in the specified order
                        writer.WriteLine(string.Join(",", telemetryData.Keys));
                        _headersWritten[carName] = true;
                    }

                    // Write values in the same order as headers
                    writer.WriteLine(string.Join(",", telemetryData.Values.Select(v => EscapeCsvValue(v?.ToString()))));
                }
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Export error for {carName}: {ex.Message}");
            }
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }
}