using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Common
{
    public class Rule
    {
        public static readonly Dictionary<string, string> RuleUser = new Dictionary<string, string>();
        private Window _win;
        private Common.DialogWindow _dw;

        public Rule()
        {
            this._win = new Window();
            this._win.Width = 375.2;
            this._win.Height = 113.099;
            this._win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this._win.ResizeMode = ResizeMode.NoResize;

            _dw = new Common.DialogWindow();
            this._win.Content = _dw;
            this._win.Closing += (sender, args) =>
            {
                args.Cancel = true;
                this._win.Visibility = Visibility.Hidden;
            };
        }

        public string Name => "固定回复";
     

        /// <summary>
        /// two args, 1:to userName 2:Msg
        /// </summary>
        public Action<string, string> SendMsg
        {
            set;
            get;
        }
        /// <summary>
        /// self
        /// </summary>
        public Tuple<string, string> Me { set; get; }

        /// <summary>
        /// when user click in control panel
        /// </summary>
        public void Click()
        {
            if (this._win.Visibility == Visibility.Hidden)
            {
                this._win.Visibility = Visibility.Visible;
            }
            else if (this._win.Visibility == Visibility.Visible)
            {
                this._win.Activate();
            }
            else
            {
                this._win.Show();
            }
        }

        public string MsgHandle(string userName, string msg, int type, int userType)
        {

            if (userType == 2 || userType == 8 && msg.Contains("@" + Me.Item1))
            {
                return this._win.Dispatcher.Invoke(() => _dw.txt.Text);
            }
            else
            {
                return null;
            }
        }
    }
}
