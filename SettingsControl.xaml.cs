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

namespace F1Manager2024Plugin
{
    public partial class SettingsControl : System.Windows.Controls.UserControl
    {
        public F1ManagerPlotter Plugin { get; }

        public class DriverSelection
        {
            public string Name { get; set; }
            public bool IsSelected { get; set; }
        }

        // Default constructor required for XAML
        public SettingsControl()
        {
            InitializeComponent();
            InitializeDriverSelection();
        }
        public class TeamDrivers
        {
            public string TeamName { get; set; }
            public DriverSelection Driver1 { get; set; }
            public DriverSelection Driver2 { get; set; }
        }

        private void InitializeDriverSelection()
        {

            // Initialize with all drivers from your carNames array
            var teams = new List<TeamDrivers>
            {
                new TeamDrivers { TeamName = "Ferrari",
                Driver1 = new DriverSelection { Name = "Ferrari1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "Ferrari2", IsSelected = false } },

                new TeamDrivers { TeamName = "McLaren",
                Driver1 = new DriverSelection { Name = "McLaren1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "McLaren2", IsSelected = false } },

                new TeamDrivers { TeamName = "Red Bull",
                Driver1 = new DriverSelection { Name = "RedBull1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "RedBull2", IsSelected = false } },

                new TeamDrivers { TeamName = "Mercedes",
                Driver1 = new DriverSelection { Name = "Mercedes1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "Mercedes2", IsSelected = false } },

                new TeamDrivers { TeamName = "Alpine",
                Driver1 = new DriverSelection { Name = "Alpine1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "Alpine2", IsSelected = false } },

                new TeamDrivers { TeamName = "Williams",
                Driver1 = new DriverSelection { Name = "Williams1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "Williams2", IsSelected = false } },

                new TeamDrivers { TeamName = "HAAS",
                Driver1 = new DriverSelection { Name = "Haas1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "Haas2", IsSelected = false } },

                new TeamDrivers { TeamName = "Racing Bulls",
                Driver1 = new DriverSelection { Name = "RacingBulls1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "RacingBulls2", IsSelected = false } },

                new TeamDrivers { TeamName = "Kick Sauber",
                Driver1 = new DriverSelection { Name = "KickSauber1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "KickSauber2", IsSelected = false } },

                new TeamDrivers { TeamName = "Aston Martin",
                Driver1 = new DriverSelection { Name = "AstonMartin1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "AstonMartin2", IsSelected = false } },

                new TeamDrivers { TeamName = "MyTeam",
                Driver1 = new DriverSelection { Name = "MyTeam1", IsSelected = false },
                Driver2 = new DriverSelection { Name = "MyTeam2", IsSelected = false } },
            };

            DriversListBox.ItemsSource = teams;
        }

        // Main constructor with plugin parameter
        public SettingsControl(F1ManagerPlotter plugin) : this()
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

            // Initialize UI with current settings
            if (plugin.Settings != null)
            {
                ExporterEnabledCheckbox.IsChecked = plugin.Settings.ExporterEnabled;
                ExporterPathTextBox.Text = plugin.Settings.ExporterPath ?? "No folder selected";

                if (plugin.Settings.TrackedDrivers != null)
                {
                    // Initialize driver selections
                    foreach (var team in DriversListBox.ItemsSource.Cast<TeamDrivers>())
                    {
                        team.Driver1.IsSelected = plugin.Settings.TrackedDrivers.Contains(team.Driver1.Name);
                        team.Driver2.IsSelected = plugin.Settings.TrackedDrivers.Contains(team.Driver2.Name);
                    }

                    // Initialize drivers text box
                    var selectedDrivers = new List<string>();
                    foreach (var team in DriversListBox.ItemsSource.Cast<TeamDrivers>())
                    {
                        if (team.Driver1.IsSelected) selectedDrivers.Add(team.Driver1.Name);
                        if (team.Driver2.IsSelected) selectedDrivers.Add(team.Driver2.Name);
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
                var defaults = F1Manager2024PluginSettings.GetDefaults();
                Plugin.Settings.ExporterEnabled = defaults.ExporterEnabled;
                Plugin.Settings.ExporterPath = defaults.ExporterPath;
                Plugin.Settings.TrackedDrivers = defaults.TrackedDrivers;

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

                await SHMessageBox.Show("Settings have been reset to default!\nYou might want to restart the plugin to make sure the settings have been reset.", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
    }
}