using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using SimHub.Plugins;
using SimHub.Plugins.Styles;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

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

        private DispatcherTimer _tireValueUpdateTimer;

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

        private void InitializeTireMapping()
        {
            // Get driver names with real names
            var driverNames = Plugin?.GetDriversNames() ?? new Dictionary<string, (string, string)>();
            var drivers = new List<DriverDisplayItem>();

            foreach (var team in DriversListBox.ItemsSource.Cast<TeamDrivers>())
            {
                drivers.Add(new DriverDisplayItem
                {
                    InternalName = team.Driver1.Name,
                    DisplayName = GetDisplayName(team.Driver1.Name, driverNames)
                });
                drivers.Add(new DriverDisplayItem
                {
                    InternalName = team.Driver2.Name,
                    DisplayName = GetDisplayName(team.Driver2.Name, driverNames)
                });
            }

            // Set up combo box
            TireMappingDriverComboBox.DisplayMemberPath = "DisplayName";
            TireMappingDriverComboBox.SelectedValuePath = "InternalName";
            TireMappingDriverComboBox.ItemsSource = drivers;

            // Select first driver by default if available
            if (drivers.Count > 0)
            {
                TireMappingDriverComboBox.SelectedIndex = 0;
            }

            // Initialize tire mapping controls
            var tireMappings = new List<TireMappingItem>();
            for (int i = 0; i < Plugin.Settings.CustomTireEnum.Length; i++)
            {
                tireMappings.Add(new TireMappingItem
                {
                    Index = i,
                    SelectedTireType = Plugin.Settings.CustomTireEnum[i]
                });
            }
            TireMappingItemsControl.ItemsSource = tireMappings;

            // Start timer for real-time updates
            _tireValueUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _tireValueUpdateTimer.Tick += UpdateCurrentTireValue;
            _tireValueUpdateTimer.Start();
        }

        private void InitializePointsSchemeSelection()
        {
            // Set the correct radio button based on current setting
            switch (Plugin.Settings.pointScheme)
            {
                case 1:
                    PointsScheme1Radio.IsChecked = true;
                    break;
                case 2:
                    PointsScheme2Radio.IsChecked = true;
                    break;
                case 3:
                    PointsScheme3Radio.IsChecked = true;
                    break;
                default:
                    PointsScheme1Radio.IsChecked = true;
                    break;
            }
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

        // Main constructor with plugin parameter
        public SettingsControl(F1ManagerPlotter plugin) : this()
        {
            InitializeUI(plugin);
        }

        public void InitializeUI(F1ManagerPlotter plugin)
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            InitializeDriverSelection();
            InitializeTireMapping();
            InitializePointsSchemeSelection();

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

                // Init Custom Team Name
                if (plugin.Settings.CustomTeamName != null)
                {
                    CustomTeamInput.Text = plugin.Settings.CustomTeamName;
                }

                // Initialize team color
                if (!string.IsNullOrEmpty(plugin.Settings.CustomTeamColor))
                {
                    try
                    {
                        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(plugin.Settings.CustomTeamColor);
                        TeamColorBrush = new System.Windows.Media.SolidColorBrush(color);
                        ColorHexText.Text = plugin.Settings.CustomTeamColor;
                    }
                    catch
                    {
                        // Default color if parsing fails
                        TeamColorBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                        ColorHexText.Text = "#FFFFFF";
                    }
                }
                else
                {
                    TeamColorBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    ColorHexText.Text = "#FFFFFF";
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

        private void ColorPickerButton_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true
            };

            // Set current color if one exists
            if (!string.IsNullOrEmpty(Plugin.Settings.CustomTeamColor))
            {
                try
                {
                    var currentColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Plugin.Settings.CustomTeamColor);
                    colorDialog.Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B);
                }
                catch
                {
                    // If there's an error parsing the color, just use default
                }
            }

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var selectedColor = System.Windows.Media.Color.FromArgb(
                    colorDialog.Color.A,
                    colorDialog.Color.R,
                    colorDialog.Color.G,
                    colorDialog.Color.B);

                TeamColorBrush = new System.Windows.Media.SolidColorBrush(selectedColor);
                ColorHexText.Text = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
            }
        }

        public System.Windows.Media.Brush TeamColorBrush
        {
            get { return (System.Windows.Media.Brush)GetValue(TeamColorBrushProperty); }
            set { SetValue(TeamColorBrushProperty, value); }
        }

        private void PointsScheme_Checked(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            if (sender == PointsScheme1Radio)
            {
                Plugin.Settings.pointScheme = 1;
            }
            else if (sender == PointsScheme2Radio)
            {
                Plugin.Settings.pointScheme = 2;
            }
            else if (sender == PointsScheme3Radio)
            {
                Plugin.Settings.pointScheme = 3;
            }
        }

        private void UpdateCurrentTireValue(object sender, EventArgs e)
        {
            if (TireMappingDriverComboBox.SelectedValue is string selectedDriver && Plugin != null)
            {
                // Find the driver in the telemetry data
                int driverIndex = Array.IndexOf(Plugin.carNames, selectedDriver);
                if (driverIndex >= 0 && Plugin._lastData.Car != null && driverIndex < Plugin._lastData.Car.Length)
                {
                    CurrentTireByteValueText.Text = Plugin._lastData.Car[driverIndex].tireCompound.ToString();
                }
                else
                {
                    CurrentTireByteValueText.Text = "N/A";
                }
            }
        }

        private void TireMappingDriverComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TireMappingDriverComboBox.SelectedItem is string selectedDriver && Plugin != null)
            {
                // Find the driver in the telemetry data
                int driverIndex = Array.IndexOf(Plugin.carNames, selectedDriver);
                if (driverIndex >= 0 && Plugin._lastData.Car != null && driverIndex < Plugin._lastData.Car.Length)
                {
                    CurrentTireByteValueText.Text = Plugin._lastData.Car[driverIndex].tireCompound.ToString();
                }
                else
                {
                    CurrentTireByteValueText.Text = "N/A";
                }
                UpdateCurrentTireValue(null, EventArgs.Empty);
            }
        }

        private void ResetTireMappingButton_Click(object sender, RoutedEventArgs e)
        {
            var defaults = F1Manager2024PluginSettings.GetDefaults();

            if (TireMappingItemsControl.ItemsSource is IEnumerable<TireMappingItem> tireMappings)
            {
                int i = 0;
                foreach (var item in tireMappings)
                {
                    item.SelectedTireType = defaults.CustomTireEnum[i++];
                }
            }

            // Also reset the settings
            Plugin.Settings.CustomTireEnum = defaults.CustomTireEnum;
        }

        private async void SaveCustomSettings_Click(object sender, RoutedEventArgs e)
        {
            if (CustomTeamInput.Text.Length == 0)
            {
                await SHMessageBox.Show("Team Name cannot be empty!", "Error!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            // Save tire mappings
            if (TireMappingItemsControl.ItemsSource is IEnumerable<TireMappingItem> tireMappings)
            {
                Plugin.Settings.CustomTireEnum = tireMappings.Select(x => x.SelectedTireType).ToArray();
            }

            await SHMessageBox.Show("Settings saved successfully!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            Plugin.Settings.CustomTeamName = CustomTeamInput.Text;
            Plugin.Settings.CustomTeamColor = ColorHexText.Text;
            Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
            Plugin.ReloadSettings(Plugin.Settings);

            InitializeUI(Plugin);
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

        public static readonly DependencyProperty TeamColorBrushProperty =
            DependencyProperty.Register("TeamColorBrush", typeof(System.Windows.Media.Brush), typeof(SettingsControl),
            new PropertyMetadata(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)));

        private async void CheckNewVersion(object sender, RoutedEventArgs e)
        {
            if (Plugin.Settings.SavedVersion != Plugin.version)
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
            Plugin.Settings.TrackedDriversDashboard = defaults.TrackedDriversDashboard;
            Plugin.Settings.CustomTeamName = defaults.CustomTeamName;
            Plugin.Settings.CustomTeamColor = defaults.CustomTeamColor;
            Plugin.Settings.CustomTireEnum = defaults.CustomTireEnum;
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

            CustomTeamInput.Text = "MyTeam";
        }
    }
}