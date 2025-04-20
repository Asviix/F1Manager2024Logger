using System.Security.Permissions;

namespace F1Manager2024Plugin
{
    public class F1Manager2024PluginSettings
    {
        public bool ExporterEnabled { get; set; } = false;
        public string ExporterPath { get; set; } = null;
        public string[] TrackedDrivers { get; set; } = new string[] { "MyTeam1", "MyTeam2" };

        public static F1Manager2024PluginSettings GetDefaults()
        {
            return new F1Manager2024PluginSettings
            {
                ExporterEnabled = false,
                ExporterPath = null,
                TrackedDrivers = new string[] { "MyTeam1", "MyTeam2" }
            };
        }

        public void UpdateSettings(bool exporterEnabled, string exporterPath, string[] trackedDrivers)
        {
            ExporterEnabled = exporterEnabled;
            ExporterPath = exporterPath;
            TrackedDrivers = trackedDrivers;
        }
    }
}