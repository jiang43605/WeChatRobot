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

namespace Tuling
{
    /// <summary>
    /// DialogWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DialogWindow : UserControl
    {
        public Dictionary<string, string> ReplaceDic;
        public DialogWindow()
        {
            InitializeComponent();
            ReplaceDic = new Dictionary<string, string>();
        }

        private void add_Click(object sender, RoutedEventArgs e)
        {
            if (ReplaceDic.ContainsKey(this.source.Text))
            {
                ReplaceDic[this.source.Text] = this.target.Text;
                this.print.Text += $"[modify] {this.source.Text} => {this.source.Text} [{DateTime.Now.ToString()}]\r\n";
            }
            else
            {
                ReplaceDic.Add(this.source.Text, this.source.Text);
                this.print.Text += $"{this.source.Text} => {this.source.Text} [{DateTime.Now.ToString()}]\r\n";
            }
        }

        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://hr.tuling123.com/help/help_center.jhtml?nav=doc");
        }
    }
}
