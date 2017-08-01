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

namespace Intervals
{
    /// <summary>
    /// DialogWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DialogWindow : UserControl
    {
        public DialogWindow()
        {
            InitializeComponent();
        }

        private void autoCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (this.autoCheckBox.IsChecked == true)
            {
                this.intervalCheckBox.IsChecked = false;
            }
        }

        private void intervalCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (this.intervalCheckBox.IsChecked == true)
            {
                this.autoCheckBox.IsChecked = false;
            }
        }
    }
}
