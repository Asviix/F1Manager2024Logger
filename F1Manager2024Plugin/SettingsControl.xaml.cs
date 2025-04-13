using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SimHub.Plugins;

namespace F1Manager2024Plugin
{
    public partial class SettingsControl : UserControl
    {
        public F1ManagerPlotter Plugin { get; }

        // Default constructor required for XAML
        public SettingsControl()
        {
            InitializeComponent();
            // Initialize with empty state
            SelectedFilePathTextBox.Text = "No file selected";
        }

        // Main constructor with plugin parameter
        public SettingsControl(F1ManagerPlotter plugin) : this()
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));

            Plugin = plugin;

            // Initialize UI with current settings
            if (plugin.Settings != null)
            {
                SelectedFilePathTextBox.Text = plugin.Settings.Path ?? "No file selected";
            }
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Memory Mapped File",
                Filter = "All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFilePathTextBox.Text = openFileDialog.FileName;

                if (Plugin.Settings != null)
                {
                    Plugin.Settings.Path = openFileDialog.FileName;
                    Plugin.SaveCommonSettings("GeneralSettings", Plugin.Settings);
                }

                Plugin._mmfReader?.StartReading(openFileDialog.FileName);
            }
        }
    }
}