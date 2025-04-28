using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.Win32;
using SimHub.Plugins;
using SimHub.Plugins.Styles;
using System.Windows.Forms;
using System.Linq.Expressions;
using SimHub.Plugins.DataPlugins.RGBDriver.LedsContainers.Groups;
using log4net.Plugin;

namespace F1Manager2024Plugin
{
    public partial class SettingsControl : System.Windows.Controls.UserControl
    {
        public F1ManagerPlotter Plugin { get; set; }

        public class DriverSelection
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public bool IsSelected { get; set; }
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

        private void InitializeDriverSelection()
        {
            var driverNames = Plugin?.GetDriversNames() ?? new Dictionary<string, (string, string)>();

            // Create a deep copy for Dashboard Tracker
            var teamsDash = CreateTeamsList(driverNames);
            var teamsExporter = CreateTeamsList(driverNames); // Separate list for Exporter

            if (Plugin.Settings.CustomTeamName.Length > 0)
            {
                teamsDash.Add(CreateMyTeamEntry(driverNames, Plugin.Settings.CustomTeamName));
                teamsExporter.Add(CreateMyTeamEntry(driverNames, Plugin.Settings.CustomTeamName));
            }
            else
            {
                teamsDash.Add(CreateMyTeamEntry(driverNames, "MyTeam"));
                teamsExporter.Add(CreateMyTeamEntry(driverNames, "MyTeam"));
            }

            DriversListBox.ItemsSource = teamsExporter;
            DriversListBoxDash.ItemsSource = teamsDash; // Different source for dashboard
        }

        private List<TeamDrivers> CreateTeamsList(Dictionary<string, (string, string)> driverNames)
        {
            return new List<TeamDrivers>
            {
                new() { TeamName = "Ferrari", BeautifiedTeamName = "Ferrari",
                Driver1 = new DriverSelection { Name = "Ferrari1", DisplayName = GetDisplayName("Ferrari1", driverNames) },
                Driver2 = new DriverSelection { Name = "Ferrari2", DisplayName = GetDisplayName("Ferrari2", driverNames) }},

                new() { TeamName = "McLaren", BeautifiedTeamName = "McLaren",
                Driver1 = new DriverSelection { Name = "McLaren1", DisplayName = GetDisplayName("McLaren1", driverNames), IsSelected = false },
                Driver2 = new DriverSelection { Name = "McLaren2", DisplayName = GetDisplayName("McLaren2", driverNames), IsSelected = false } },

                new() { TeamName = "Red Bull", BeautifiedTeamName = "Red Bull Racing",
                Driver1 = new DriverSelection { Name = "RedBull1", DisplayName = GetDisplayName("RedBull1", driverNames), IsSelected = false },
                Driver2 = new DriverSelection { Name = "RedBull2", DisplayName = GetDisplayName("RedBull2", driverNames), IsSelected = false } },

                new() { TeamName = "Mercedes", BeautifiedTeamName = "Mercedes AMG Petronas F1",
                Driver1 = new DriverSelection { Name = "Mercedes1", DisplayName = GetDisplayName("Mercedes1", driverNames), IsSelected = false },
                Driver2 = new DriverSelection { Name = "Mercedes2", DisplayName = GetDisplayName("Mercedes2", driverNames), IsSelected = false } },

                new() { TeamName = "Alpine", BeautifiedTeamName = "Alpine",
                Driver1 = new DriverSelection { Name = "Alpine1", DisplayName = GetDisplayName("Alpine1", driverNames), IsSelected = false },
                Driver2 = new DriverSelection { Name = "Alpine2", DisplayName = GetDisplayName("Alpine2", driverNames), IsSelected = false } },

                new() { TeamName = "Williams", BeautifiedTeamName = "Williams Racing",
                Driver1 = new DriverSelection { Name = "Williams1", DisplayName = GetDisplayName("Williams1", driverNames), IsSelected = false },
                Driver2 = new DriverSelection { Name = "Williams2", DisplayName = GetDisplayName("Williams2", driverNames), IsSelected = false } },

                new() { TeamName = "HAAS", BeautifiedTeamName = "Haas F1",
                Driver1 = new DriverSelection { Name = "Haas1", DisplayName = GetDisplayName("Haas1", driverNames), IsSelected = false },
                Driver2 = new DriverSelection { Name = "Haas2", DisplayName = GetDisplayName("Haas2", driverNames), IsSelected = false } },

                new() { TeamName = "Racing Bulls", BeautifiedTeamName = "Racing Bulls",
                Driver1 = new DriverSelection { Name = "RacingBulls1", DisplayName = GetDisplayName("RacingBulls1", driverNames), IsSelected = false },
                Driver2 = new DriverSelection { Name = "RacingBulls2", DisplayName = GetDisplayName("RacingBulls2", driverNames), IsSelected = false } },

                new() { TeamName = "Kick Sauber", BeautifiedTeamName = "Kick Sauber",
                Driver1 = new DriverSelection { Name = "KickSauber1", DisplayName = GetDisplayName("KickSauber1", driverNames), IsSelected = false },
                Driver2 = new DriverSelection { Name = "KickSauber2", DisplayName = GetDisplayName("KickSauber2", driverNames), IsSelected = false } },

                new() { TeamName = "Aston Martin", BeautifiedTeamName = "Aston Martin",
                Driver1 = new DriverSelection { Name = "AstonMartin1", DisplayName = GetDisplayName("AstonMartin1", driverNames), IsSelected = false },
                Driver2 = new DriverSelection { Name = "AstonMartin2", DisplayName = GetDisplayName("AstonMartin2", driverNames), IsSelected = false } },
            };
        }

        private TeamDrivers CreateMyTeamEntry(Dictionary<string, (string, string)> driverNames, string teamName)
        {
            return new TeamDrivers
            {
                TeamName = "MyTeam",
                BeautifiedTeamName = teamName,
                Driver1 = new DriverSelection { Name = "MyTeam1", DisplayName = GetDisplayName("MyTeam1", driverNames) },
                Driver2 = new DriverSelection { Name = "MyTeam2", DisplayName = GetDisplayName("MyTeam2", driverNames) }
            };
        }

        private string GetDisplayName(string internalName, Dictionary<string, (string First, string Last)> driverNames)
        {
            return driverNames.TryGetValue(internalName, out var name) ? $"{name.First} {name.Last}" : internalName;
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
                CustomTeamInput.Text = plugin.Settings.CustomTeamName ?? "MyTeam";

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

                if (plugin.Settings.CustomTeamName != null)
                {
                    CustomTeamInput.Text = plugin.Settings.CustomTeamName;
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
            if (sender is System.Windows.Controls.TextBlock textBlock && !string.IsNullOrWhiteSpace(textBlock.Text))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = textBlock.Text,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    SHMessageBox.Show($"Failed to open the URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void SaveCustomSettings_Click(object sender, RoutedEventArgs e)
        {
            if (CustomTeamInput.Text.Length == 0)
            {
                await SHMessageBox.Show("Team Name cannot be empty!", "Error!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            await SHMessageBox.Show("Settings saved successfully!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            Plugin.Settings.CustomTeamName = CustomTeamInput.Text;
            Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
            Plugin.ReloadSettings(Plugin.Settings);

            InitializeUI(Plugin);
        }

        private async void CheckNewVersion(object sender, RoutedEventArgs e)
        {
            if (Plugin.Settings.SavedVersion != Plugin.Settings.RequiredVersion)
            {
                ResetSettingsToDefault();

                await SHMessageBox.Show("New Version detected, settings have been reset to default.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            InitializeUI(Plugin);
        }

        private void ResetSettingsToDefault()
        {
            var defaults = F1Manager2024PluginSettings.GetDefaults();
            Plugin.Settings.ExporterEnabled = defaults.ExporterEnabled;
            Plugin.Settings.ExporterPath = defaults.ExporterPath;
            Plugin.Settings.TrackedDrivers = defaults.TrackedDrivers;
            Plugin.Settings.CustomTeamName = defaults.CustomTeamName;
            Plugin.Settings.SavedVersion = defaults.SavedVersion;

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

            CustomTeamInput.Text = "MyTeam";
        }
    }
}