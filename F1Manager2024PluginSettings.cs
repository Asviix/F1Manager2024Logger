using System.Security.Permissions;
using System.Security.Policy;

namespace F1Manager2024Plugin
{
    public class F1Manager2024PluginSettings
    {
        public bool ExporterEnabled { get; set; } = false;
        public string ExporterPath { get; set; } = null;
        public string[] TrackedDrivers { get; set; } = new string[] { "MyTeam1", "MyTeam2" };
        public string[] TrackedDriversDashboard { get; set; } = new string[] { "MyTeam1", "MyTeam2" };
        public string CustomTeamName { get; set; } = "MyTeam";
        public string CustomTeamColor { get; set; } = "#FFFFFF";
        public double SavedVersion { get; set; } = 0.6;
        public double RequiredVersion { get; set; } = 0.6;

        public static F1Manager2024PluginSettings GetDefaults()
        {
            return new F1Manager2024PluginSettings
            {
                ExporterEnabled = false,
                ExporterPath = null,
                TrackedDrivers = new string[] { "MyTeam1", "MyTeam2" },
                TrackedDriversDashboard = new string[] { "MyTeam1", "MyTeam2" },
                CustomTeamName = "MyTeam",
                CustomTeamColor = "#FFFFFF",
                SavedVersion = 0.6
            };
        }
    }
}