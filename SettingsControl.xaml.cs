using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SimHub.Plugins;
using SimHub.Plugins.Styles;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace F1Manager2024Plugin
{
    public partial class SettingsControl : System.Windows.Controls.UserControl
    {
        public F1ManagerPlotter Plugin { get; set; }

        public class DriverSelection
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public bool IsSelected { get; set; } = false;
        }

        // Default constructor required for XAML
        public SettingsControl() => InitializeComponent();

        public class TeamDrivers
        {
            public string TeamName { get; set; }
            public string BeautifiedTeamName { get; set; }
            public DriverSelection Driver1 { get; set; }
            public DriverSelection Driver2 { get; set; }
        }

        public class DriverDisplayItem
        {
            public string InternalName { get; set; }
            public string DisplayName { get; set; }
        }

        public class TireMappingItem : INotifyPropertyChanged
        {
            public int Index { get; set; }

            private string _selectedTireType;
            public string SelectedTireType
            {
                get => _selectedTireType;
                set
                {
                    _selectedTireType = value;
                    OnPropertyChanged();
                }
            }

            public List<string> AvailableTireTypes { get; } = new List<string>
            {
                "Soft", "Medium", "Hard", "Intermediates", "Wet", "Not-Set"
            };

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void InitializeDriverSelection()
        {
            var driverNames = Plugin?.GetDriversNames() ?? new Dictionary<string, (string, string)>();
            var teamNames = Plugin?.GetDriversTeamNames() ?? new Dictionary<string, string>();

            // Create a deep copy for Dashboard Tracker
            var teamsDash = CreateTeamsList(driverNames, teamNames);
            var teamsExporter = CreateTeamsList(driverNames, teamNames); // Separate list for Exporter

            DriversListBox.ItemsSource = teamsExporter;
            DriversListBoxDash.ItemsSource = teamsDash; // Different source for dashboard
        }

        private List<TeamDrivers> CreateTeamsList(Dictionary<string, (string, string)> driverNames, Dictionary<string, string> teamNames)
        {
            return new List<TeamDrivers>
            {
                new() { TeamName = "Ferrari", BeautifiedTeamName = GetTeamDisplayName("Ferrari1", teamNames),
                Driver1 = new DriverSelection { Name = "Ferrari1", DisplayName = GetDisplayName("Ferrari1", driverNames) },
                Driver2 = new DriverSelection { Name = "Ferrari2", DisplayName = GetDisplayName("Ferrari2", driverNames) } },

                new() { TeamName = "McLaren", BeautifiedTeamName = GetTeamDisplayName("McLaren1", teamNames),
                Driver1 = new DriverSelection { Name = "McLaren1", DisplayName = GetDisplayName("McLaren1", driverNames) },
                Driver2 = new DriverSelection { Name = "McLaren2", DisplayName = GetDisplayName("McLaren2", driverNames) } },

                new() { TeamName = "Red Bull", BeautifiedTeamName = GetTeamDisplayName("RedBull1", teamNames),
                Driver1 = new DriverSelection { Name = "RedBull1", DisplayName = GetDisplayName("RedBull1", driverNames) },
                Driver2 = new DriverSelection { Name = "RedBull2", DisplayName = GetDisplayName("RedBull2", driverNames) } },

                new() { TeamName = "Mercedes", BeautifiedTeamName = GetTeamDisplayName("Mercedes1", teamNames),
                Driver1 = new DriverSelection { Name = "Mercedes1", DisplayName = GetDisplayName("Mercedes1", driverNames) },
                Driver2 = new DriverSelection { Name = "Mercedes2", DisplayName = GetDisplayName("Mercedes2", driverNames) } },

                new() { TeamName = "Alpine", BeautifiedTeamName = GetTeamDisplayName("Alpine1", teamNames),
                Driver1 = new DriverSelection { Name = "Alpine1", DisplayName = GetDisplayName("Alpine1", driverNames) },
                Driver2 = new DriverSelection { Name = "Alpine2", DisplayName = GetDisplayName("Alpine2", driverNames) } },

                new() { TeamName = "Williams", BeautifiedTeamName = GetTeamDisplayName("Williams1", teamNames),
                Driver1 = new DriverSelection { Name = "Williams1", DisplayName = GetDisplayName("Williams1", driverNames) },
                Driver2 = new DriverSelection { Name = "Williams2", DisplayName = GetDisplayName("Williams2", driverNames) } },

                new() { TeamName = "HAAS", BeautifiedTeamName = GetTeamDisplayName("Haas1", teamNames),
                Driver1 = new DriverSelection { Name = "Haas1", DisplayName = GetDisplayName("Haas1", driverNames) },
                Driver2 = new DriverSelection { Name = "Haas2", DisplayName = GetDisplayName("Haas2", driverNames) } },

                new() { TeamName = "Racing Bulls", BeautifiedTeamName = GetTeamDisplayName("RacingBulls1", teamNames),
                Driver1 = new DriverSelection { Name = "RacingBulls1", DisplayName = GetDisplayName("RacingBulls1", driverNames) },
                Driver2 = new DriverSelection { Name = "RacingBulls2", DisplayName = GetDisplayName("RacingBulls2", driverNames) } },

                new() { TeamName = "Kick Sauber", BeautifiedTeamName = GetTeamDisplayName("KickSauber1", teamNames),
                Driver1 = new DriverSelection { Name = "KickSauber1", DisplayName = GetDisplayName("KickSauber1", driverNames) },
                Driver2 = new DriverSelection { Name = "KickSauber2", DisplayName = GetDisplayName("KickSauber2", driverNames) } },

                new() { TeamName = "Aston Martin", BeautifiedTeamName = GetTeamDisplayName("AstonMartin1", teamNames),
                Driver1 = new DriverSelection { Name = "AstonMartin1", DisplayName = GetDisplayName("AstonMartin1", driverNames) },
                Driver2 = new DriverSelection { Name = "AstonMartin2", DisplayName = GetDisplayName("AstonMartin2", driverNames) } },

                new() { TeamName = "Custom Team", BeautifiedTeamName = GetTeamDisplayName("MyTeam1", teamNames),
                Driver1 = new DriverSelection { Name = "MyTeam1", DisplayName = GetDisplayName("MyTeam1", driverNames), IsSelected = true },
                Driver2 = new DriverSelection { Name = "MyTeam2", DisplayName = GetDisplayName("MyTeam2", driverNames), IsSelected = true } },
            };
        }

        private string GetDisplayName(string internalName, Dictionary<string, (string First, string Last)> driverNames)
        {
            if (driverNames.TryGetValue(internalName, out var name))
            {
                // Handle cases where first or last name might be null or "Unknown"
                var firstName = string.IsNullOrWhiteSpace(name.First) || name.First == "Unknown"
                    ? string.Empty
                    : name.First;
                var lastName = string.IsNullOrWhiteSpace(name.Last) || name.Last == "Unknown"
                    ? string.Empty
                    : name.Last;

                return $"{firstName} {lastName}".Trim();
            }

            // Fallback to internal name if no driver info found
            return internalName;
        }

        private string GetTeamDisplayName(string internalName, Dictionary<string, string> teamNames)
        {
            if (teamNames.TryGetValue(internalName, out var name))
            {
                return name;
            }
            // Fallback to internal name if no team info found
            return internalName;
        }

        // Main constructor with plugin parameter
        public SettingsControl(F1ManagerPlotter plugin) : this()
        {
            InitializeUI(plugin);
        }

        public void InitializeUI(F1ManagerPlotter plugin)
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            InitializeDriverSelection();

            // Initialize UI with current settings
            if (plugin.Settings != null)
            {
                ExporterEnabledCheckbox.IsChecked = plugin.Settings.ExporterEnabled;
                ExporterPathTextBox.Text = plugin.Settings.ExporterPath ?? "No folder selected";

                if (plugin.Settings.TrackedDrivers != null)
                {
                    // Initialize team selections
                    foreach (var team in DriversListBox.ItemsSource.Cast<TeamDrivers>())
                    {
                        team.Driver1.IsSelected = plugin.Settings.TrackedDrivers.Contains(team.Driver1.Name);
                        team.Driver2.IsSelected = plugin.Settings.TrackedDrivers.Contains(team.Driver2.Name);
                    }

                    // Initialize drivers text box
                    var selectedDrivers = new List<string>();
                    foreach (var team in DriversListBox.ItemsSource.Cast<TeamDrivers>())
                    {
                        if (team.Driver1.IsSelected) selectedDrivers.Add(team.Driver1.DisplayName);
                        if (team.Driver2.IsSelected) selectedDrivers.Add(team.Driver2.DisplayName);
                    }

                    // Initialize Driver Dash Selections
                    foreach (var team in DriversListBoxDash.ItemsSource.Cast<TeamDrivers>())
                    {
                        team.Driver1.IsSelected = plugin.Settings.TrackedDriversDashboard.Contains(team.Driver1.Name);
                        team.Driver2.IsSelected = plugin.Settings.TrackedDriversDashboard.Contains(team.Driver2.Name);
                    }

                    DriversTextBox.Text = selectedDrivers.Any()
                        ? string.Join(", ", selectedDrivers)
                        : "No drivers selected";
                }
            }
        }

        private void ExporterChecked(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;
            if (Plugin.Settings != null)
            {
                Plugin.Settings.ExporterEnabled = true;
            }
        }

        private void ExporterUnchecked(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;
            if (Plugin.Settings != null)
            {
                Plugin.Settings.ExporterEnabled = false;
            }
        }

        private void BrowseExporter_Folder(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select Exporter Folder"
            };
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExporterPathTextBox.Text = folderBrowserDialog.SelectedPath;
                if (Plugin.Settings != null)
                {
                    Plugin.Settings.ExporterPath = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private async void SaveExporter_Settings(object sender, RoutedEventArgs e)
        {
            var selectedDrivers = new List<string>();

            foreach (var team in DriversListBox.ItemsSource.Cast<TeamDrivers>())
            {
                if (team.Driver1.IsSelected) selectedDrivers.Add(team.Driver1.Name);
                if (team.Driver2.IsSelected) selectedDrivers.Add(team.Driver2.Name);
            }

            if (selectedDrivers.Count >= 6)
            {
                var result = await SHMessageBox.Show(
                    "Warning! Selecting more than 6 drivers can take a lot of storage space. Are you sure you want to continue?",
                    "Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    if (selectedDrivers.Any())
                    {
                        DriversTextBox.Text = string.Join(", ", selectedDrivers);
                    }
                    else
                    {
                        DriversTextBox.Text = "No drivers selected";
                    }
                }
                else
                {
                    if (Plugin.Settings.TrackedDrivers != null)
                    {
                        foreach (var driver in DriversListBox.ItemsSource.Cast<DriverSelection>())
                        {
                            driver.IsSelected = Plugin.Settings.TrackedDrivers.Contains(driver.Name);
                        }
                    }
                    if (Plugin.Settings.TrackedDrivers.Any())
                    {
                        DriversTextBox.Text = string.Join(", ", Plugin.Settings.TrackedDrivers);
                    }
                    else
                    {
                        DriversTextBox.Text = "No drivers selected";
                    }
                    Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
                    return;
                }
            }

            Plugin.Settings.TrackedDrivers = selectedDrivers.ToArray();
            Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
            Plugin.ReloadSettings(Plugin.Settings);

            if (selectedDrivers.Any())
            {
                DriversTextBox.Text = string.Join(", ", selectedDrivers);
            }
            else
            {
                DriversTextBox.Text = "No drivers selected";
            }

            await SHMessageBox.Show("Settings saved successfully!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            InitializeUI(Plugin);

        }

        private async void SaveTrackedDriversButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDriversDash = new List<string>();

            foreach (var team in DriversListBoxDash.ItemsSource.Cast<TeamDrivers>())
            {
                if (team.Driver1.IsSelected) selectedDriversDash.Add(team.Driver1.Name);
                if (team.Driver2.IsSelected) selectedDriversDash.Add(team.Driver2.Name);
            }

            if (selectedDriversDash.Count > 2)
            {
                await SHMessageBox.Show("You cannot select more than 2 drivers!", "Error!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else
            {
                // Reset selections based on saved settings
                foreach (var team in DriversListBoxDash.ItemsSource.Cast<TeamDrivers>())
                {
                    team.Driver1.IsSelected = Plugin.Settings.TrackedDriversDashboard.Contains(team.Driver1.Name);
                    team.Driver2.IsSelected = Plugin.Settings.TrackedDriversDashboard.Contains(team.Driver2.Name);
                }

                Plugin.Settings.TrackedDriversDashboard = selectedDriversDash.ToArray();
                Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
                Plugin.ReloadSettings(Plugin.Settings);
                await SHMessageBox.Show("Settings saved successfully!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);

                InitializeUI(Plugin);
            }
        }

        private void HistoricalDataDelete_Click(object sender, RoutedEventArgs e)
        {
            Plugin.ClearAllHistory();

            SHMessageBox.Show("All historical data has been deleted!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ResetToDefault_Button_Click(object sender, RoutedEventArgs e)
        {
            var result = await SHMessageBox.Show(
                "Are you sure you want to reset all settings to default?",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                ResetSettingsToDefault();

                await SHMessageBox.Show("Settings have been reset to default!\nYou might want to restart the plugin to make sure the settings have been reset.", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            InitializeUI(Plugin);
        }

        private void OpenHelpLinks(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBlock textBlock && textBlock.Tag is string url)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    SHMessageBox.Show($"Failed to open the link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void HighlightHelpLinks(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBlock textBlock)
            {
                textBlock.TextDecorations.Add(System.Windows.TextDecorations.Underline);
                textBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 85, 168));
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Hand;
            }
        }

        private void RemoveHighlightHelpLinks(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBlock textBlock)
            {
                textBlock.TextDecorations.Clear();
                textBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 102, 204));
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        public static readonly DependencyProperty TeamColorBrushProperty =
            DependencyProperty.Register("TeamColorBrush", typeof(System.Windows.Media.Brush), typeof(SettingsControl),
            new PropertyMetadata(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)));

        private async void PageLoadEvent(object sender, RoutedEventArgs e)
        {
            if (Plugin.Settings.SavedVersion != Plugin.version)
            {
                ResetSettingsToDefault();

                await SHMessageBox.Show("New Version detected, settings have been reset to default.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (!Plugin.Settings.SaveFileFound)
            {
                await SHMessageBox.Show("Save file not found. Please create a save-game first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            InitializeUI(Plugin);
        }

        private void ResetSettingsToDefault()
        {
            var defaults = F1Manager2024PluginSettings.GetDefaults();
            Plugin.Settings.ExporterEnabled = defaults.ExporterEnabled;
            Plugin.Settings.ExporterPath = defaults.ExporterPath;
            Plugin.Settings.TrackedDrivers = defaults.TrackedDrivers;
            Plugin.Settings.TrackedDriversDashboard = defaults.TrackedDriversDashboard;
            Plugin.Settings.SavedVersion = Plugin.version;

            Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
            Plugin.ReloadSettings(Plugin.Settings);

            ExporterEnabledCheckbox.IsChecked = false;
            ExporterPathTextBox.Text = "No folder selected";

            foreach (var team in DriversListBox.ItemsSource.Cast<TeamDrivers>())
            {
                team.Driver1.IsSelected = defaults.TrackedDrivers.Contains(team.Driver1.Name);
                team.Driver2.IsSelected = defaults.TrackedDrivers.Contains(team.Driver2.Name);
            }
            DriversTextBox.Text = string.Join(", ", defaults.TrackedDrivers);
        }
    }
}