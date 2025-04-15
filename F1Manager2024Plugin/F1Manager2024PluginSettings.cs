namespace F1Manager2024Plugin
{
    public class F1Manager2024PluginSettings
    {
        public string Path { get; set; } = null;
        public bool ExporterEnabled { get; set; } = false;
        public string ExporterPath { get; set; } = null;
        public string[] TrackedDrivers { get; set; } = new string[] { "MyTeam1", "MyTeam2" };

        public static F1Manager2024PluginSettings GetDefaults()
        {
            return new F1Manager2024PluginSettings
            {
                Path = null,
                ExporterEnabled = false,
                ExporterPath = null,
                TrackedDrivers = new string[] { "MyTeam1", "MyTeam2" }
            };
        }
    }
}