namespace F1Manager2024Plugin
{
    public class F1Manager2024PluginSettings
    {
        public bool ExporterEnabled { get; set; } = false;
        public string ExporterPath { get; set; } = null;
        public string[] TrackedDrivers { get; set; } = new string[] { "MyTeam1", "MyTeam2" };
        public string[] TrackedDriversDashboard { get; set; } = new string[] { "MyTeam1", "MyTeam2" };
        public bool SaveFileFound { get; set; } = false;
        public double SavedVersion { get; set; } = 1.1;

        public static F1Manager2024PluginSettings GetDefaults()
        {
            return new F1Manager2024PluginSettings
            {
                ExporterEnabled = false,
                ExporterPath = null,
                TrackedDrivers = new string[] { "MyTeam1", "MyTeam2" },
                TrackedDriversDashboard = new string[] { "MyTeam1", "MyTeam2" },
                SaveFileFound = false,
            };
        }
    }
}