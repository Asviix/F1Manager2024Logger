using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using log4net.Plugin;
using Microsoft.Win32;
using SimHub.Plugins;
using SimHub.Plugins.Styles;

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
            // Initialize with empty state
            SelectedFilePathTextBox.Text = "No file selected";
        }

        private void InitializeDriverSelection()
        {
            // Initialize with all drivers from your carNames array
            var drivers = new List<DriverSelection>
            {
                new DriverSelection { Name = "Ferrari1", IsSelected = false },
                new DriverSelection { Name = "Ferrari2", IsSelected = false },
                new DriverSelection { Name = "McLaren1", IsSelected = false },
                new DriverSelection { Name = "McLaren2", IsSelected = false },
                new DriverSelection { Name = "RedBull1", IsSelected = false },
                new DriverSelection { Name = "RedBull2", IsSelected = false },
                new DriverSelection { Name = "Mercedes1", IsSelected = false },
                new DriverSelection { Name = "Mercedes2", IsSelected = false },
                new DriverSelection { Name = "Alpine1", IsSelected = false },
                new DriverSelection { Name = "Alpine2", IsSelected = false },
                new DriverSelection { Name = "Williams1", IsSelected = false },
                new DriverSelection { Name = "Williams2", IsSelected = false },
                new DriverSelection { Name = "Haas1", IsSelected = false },
                new DriverSelection { Name = "Haas2", IsSelected = false },
                new DriverSelection { Name = "RacingBulls1", IsSelected = false },
                new DriverSelection { Name = "RacingBulls2", IsSelected = false },
                new DriverSelection { Name = "KickSauber1", IsSelected = false },
                new DriverSelection { Name = "KickSauber2", IsSelected = false },
                new DriverSelection { Name = "AstonMartin1", IsSelected = false },
                new DriverSelection { Name = "AstonMartin2", IsSelected = false },
                new DriverSelection { Name = "MyTeam1", IsSelected = true }, // Default selected
                new DriverSelection { Name = "MyTeam2", IsSelected = true }  // Default selected
            };

            DriversListBox.ItemsSource = drivers;
        }

        // Main constructor with plugin parameter
        public SettingsControl(F1ManagerPlotter plugin) : this()
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

            // Initialize UI with current settings
            if (plugin.Settings != null)
            {
                SelectedFilePathTextBox.Text = plugin.Settings.Path ?? "No file selected";
                ExporterEnabledCheckbox.IsChecked = plugin.Settings.ExporterEnabled;
                ExporterPathTextBox.Text = plugin.Settings.ExporterPath ?? "No folder selected";
                if (plugin.Settings.TrackedDrivers != null)
                {
                    foreach (var driver in DriversListBox.ItemsSource.Cast<DriverSelection>())
                    {
                        driver.IsSelected = plugin.Settings.TrackedDrivers.Contains(driver.Name);
                    }
                }
                var selectedDrivers = DriversListBox.ItemsSource.Cast<DriverSelection>()
                    .Where(d => d.IsSelected)
                    .Select(d => d.Name)
                    .ToList();

                if (selectedDrivers.Any())
                {
                    DriversTextBox.Text = string.Join(", ", selectedDrivers);
                }
                else
                {
                    DriversTextBox.Text = "No drivers selected";
                }
            }
        }

        private void BrowseMMF_File(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Select Memory Mapped File",
                Filter = "All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFilePathTextBox.Text = openFileDialog.FileName;

                if (openFileDialog.FileName.Contains("F1Manager_Telemetry") == false)
                {
                    SHMessageBox.Show("Please select the correct file: F1Manager_Telemetry", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (Plugin.Settings != null)
                {
                    Plugin.Settings.Path = openFileDialog.FileName;
                    Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
                    Plugin.StartReading(Plugin.Settings.Path);
                }
            }
        }

        private void ExporterChecked(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;
            if (Plugin.Settings != null)
            {
                Plugin.Settings.ExporterEnabled = true;
                Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
            }
        }

        private void ExporterUnchecked(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;
            if (Plugin.Settings != null)
            {
                Plugin.Settings.ExporterEnabled = false;
                Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
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
                    Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
                }
            }
        }

        private async void SaveDriversButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDrivers = DriversListBox.ItemsSource.Cast<DriverSelection>()
                .Where(d => d.IsSelected)
                .Select(d => d.Name)
                .ToArray();

            if (selectedDrivers.Length >= 6)
            {
                var result = await SHMessageBox.Show(
                    "Warning! Selecting more than 6 drivers can take a lot of storage space. Are you sure you want to continue?",
                    "Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    Plugin.Settings.TrackedDrivers = selectedDrivers;
                    Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);

                    if (selectedDrivers.Any())
                    {
                        DriversTextBox.Text = string.Join(", ", selectedDrivers);
                    }
                    else
                    {
                        DriversTextBox.Text = "No drivers selected";
                    }

                    await SHMessageBox.Show("Drivers saved successfully!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
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
                    return;
                }
            }

            Plugin.Settings.TrackedDrivers = selectedDrivers;
            Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);

            if (selectedDrivers.Any())
            {
                DriversTextBox.Text = string.Join(", ", selectedDrivers);
            }
            else
            {
                DriversTextBox.Text = "No drivers selected";
            }

            await SHMessageBox.Show("Drivers saved successfully!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);

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
                Plugin.Settings.Path = defaults.Path;
                Plugin.Settings.ExporterEnabled = defaults.ExporterEnabled;
                Plugin.Settings.ExporterPath = defaults.ExporterPath;
                Plugin.Settings.TrackedDrivers = defaults.TrackedDrivers;

                Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);

                SelectedFilePathTextBox.Text = "No file selected";
                ExporterEnabledCheckbox.IsChecked = false;
                ExporterPathTextBox.Text = "No folder selected";

                foreach (var driver in DriversListBox.ItemsSource.Cast<DriverSelection>())
                {
                    driver.IsSelected = defaults.TrackedDrivers.Contains(driver.Name);
                }
                DriversTextBox.Text = string.Join(", ", defaults.TrackedDrivers);

                Plugin.StopReading();

                await SHMessageBox.Show("Settings have been reset to default!\nYou might want to restart the plugin to make sure the settings have been reset.", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}