using SimHub.Plugins.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace User.PluginSdkDemo
{
    /// <summary>
    /// Logique d'interaction pour CustomDialog.xaml
    /// </summary>
    public partial class CustomDialog : SHDialogContentBase
    {
        public CustomDialog()
        {
            InitializeComponent();

            ShowApply = true;
            ShowOk = true;
            ShowCancel = true;
        }

        public override bool Apply()
        {
            // Apply clicked;

            return base.Apply();
        }
    }
}
