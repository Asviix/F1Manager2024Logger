﻿#pragma checksum "..\..\SettingsControl.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "ED402CA89809209D1C81A268EC5486FE325210E8F6F75917D0F5BAA96F9C868D"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using AvalonDock;
using AvalonDock.Controls;
using AvalonDock.Converters;
using AvalonDock.Layout;
using AvalonDock.Themes;
using F1Manager2024Plugin;
using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using MahApps.Metro.IconPacks;
using MahApps.Metro.IconPacks.Converter;
using SimHub.Plugins.Styles;
using SimHub.Plugins.UI;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using WoteverCommon.WPF;
using WoteverCommon.WPF.AutoGrid;
using WoteverCommon.WPF.Behaviors;
using WoteverCommon.WPF.Converters;
using WoteverCommon.WPF.Styles;
using WoteverLocalization;


namespace F1Manager2024Plugin {
    
    
    /// <summary>
    /// SettingsControl
    /// </summary>
    public partial class SettingsControl : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 27 "..\..\SettingsControl.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SelectedFilePathTextBox;
        
        #line default
        #line hidden
        
        
        #line 40 "..\..\SettingsControl.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal SimHub.Plugins.Styles.SHToggleButton ExporterEnabledCheckbox;
        
        #line default
        #line hidden
        
        
        #line 45 "..\..\SettingsControl.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox ExporterPathTextBox;
        
        #line default
        #line hidden
        
        
        #line 57 "..\..\SettingsControl.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox DriversListBox;
        
        #line default
        #line hidden
        
        
        #line 64 "..\..\SettingsControl.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal SimHub.Plugins.Styles.SHButtonPrimary SaveDriversButton;
        
        #line default
        #line hidden
        
        
        #line 66 "..\..\SettingsControl.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox DriversTextBox;
        
        #line default
        #line hidden
        
        
        #line 77 "..\..\SettingsControl.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal SimHub.Plugins.Styles.SHButtonPrimary HistoricalDataDelete;
        
        #line default
        #line hidden
        
        
        #line 86 "..\..\SettingsControl.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal SimHub.Plugins.Styles.SHButtonPrimary ResetToDefault_Button;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/F1Manager2024Plugin;component/settingscontrol.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\SettingsControl.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.SelectedFilePathTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 2:
            
            #line 28 "..\..\SettingsControl.xaml"
            ((SimHub.Plugins.Styles.SHButtonPrimary)(target)).Click += new System.Windows.RoutedEventHandler(this.BrowseMMF_File);
            
            #line default
            #line hidden
            return;
            case 3:
            this.ExporterEnabledCheckbox = ((SimHub.Plugins.Styles.SHToggleButton)(target));
            
            #line 40 "..\..\SettingsControl.xaml"
            this.ExporterEnabledCheckbox.Checked += new System.Windows.RoutedEventHandler(this.ExporterChecked);
            
            #line default
            #line hidden
            
            #line 40 "..\..\SettingsControl.xaml"
            this.ExporterEnabledCheckbox.Unchecked += new System.Windows.RoutedEventHandler(this.ExporterUnchecked);
            
            #line default
            #line hidden
            return;
            case 4:
            this.ExporterPathTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 5:
            
            #line 46 "..\..\SettingsControl.xaml"
            ((SimHub.Plugins.Styles.SHButtonPrimary)(target)).Click += new System.Windows.RoutedEventHandler(this.BrowseExporter_Folder);
            
            #line default
            #line hidden
            return;
            case 6:
            this.DriversListBox = ((System.Windows.Controls.ListBox)(target));
            return;
            case 7:
            this.SaveDriversButton = ((SimHub.Plugins.Styles.SHButtonPrimary)(target));
            
            #line 64 "..\..\SettingsControl.xaml"
            this.SaveDriversButton.Click += new System.Windows.RoutedEventHandler(this.SaveDriversButton_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            this.DriversTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 9:
            this.HistoricalDataDelete = ((SimHub.Plugins.Styles.SHButtonPrimary)(target));
            
            #line 77 "..\..\SettingsControl.xaml"
            this.HistoricalDataDelete.Click += new System.Windows.RoutedEventHandler(this.HistoricalDataDelete_Click);
            
            #line default
            #line hidden
            return;
            case 10:
            this.ResetToDefault_Button = ((SimHub.Plugins.Styles.SHButtonPrimary)(target));
            
            #line 86 "..\..\SettingsControl.xaml"
            this.ResetToDefault_Button.Click += new System.Windows.RoutedEventHandler(this.ResetToDefault_Button_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

