namespace F1Manager2024Plugin
{
    public class F1Manager2024PluginSettings
    {
        public string Path { get; set; } = null;
        public bool ExporterEnabled { get; set; } = false;
        public string ExporterPath { get; set; } = null;
        public string[] trackedDrivers { get; set; } = new string[] { "MyTeam1", "MyTeam2" };
    }
}