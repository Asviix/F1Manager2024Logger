using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SimHub.Plugins;
using SimHub.Plugins.Styles;

namespace F1Manager2024Plugin
{
    public partial class SettingsControl : UserControl
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

            DriversComboBox.ItemsSource = drivers;
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
                if (plugin.Settings.trackedDrivers != null)
                {
                    foreach (var driver in DriversComboBox.ItemsSource.Cast<DriverSelection>())
                    {
                        driver.IsSelected = plugin.Settings.trackedDrivers.Contains(driver.Name);
                    }
                }
                var selectedDrivers = DriversComboBox.ItemsSource.Cast<DriverSelection>()
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

            var openFileDialog = new OpenFileDialog()
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

        private void SaveDriversButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDrivers = DriversComboBox.ItemsSource.Cast<DriverSelection>()
                .Where(d => d.IsSelected)
                .Select(d => d.Name)
                .ToArray();
            Plugin.Settings.trackedDrivers = selectedDrivers;
            Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);

            if (selectedDrivers.Any())
            {
                DriversTextBox.Text = string.Join(", ", selectedDrivers);
            }
            else
            {
                DriversTextBox.Text = "No drivers selected";
            }

            if (selectedDrivers.Length >= 6)
            {
                SHMessageBox.Show("Storager usage can quickly become high if a lot of drivers are selected!", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            SHMessageBox.Show("Drivers saved successfully!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);

        }
    }
}