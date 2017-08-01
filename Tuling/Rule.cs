using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Tuling
{
    public class Rule
    {
        public static readonly Dictionary<string, string> RuleUser = new Dictionary<string, string>();
        private Window _win;
        private DialogWindow _dw;
        private TulingChat _tc = new TulingChat();

        public Rule()
        {
            this._win = new Window();
            this._win.Width = 353.6;
            this._win.Height = 450;
            this._win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this._win.ResizeMode = ResizeMode.NoResize;

            _dw = new DialogWindow();
            this._win.Content = _dw;
            this._win.Closing += (sender, args) =>
            {
                args.Cancel = true;
                this._win.Visibility = Visibility.Hidden;
            };
        }

        public string Name => "图灵机器人";

        public Action<string, string> SendMsg
        {
            set;
            get;
        }

        public Tuple<string, string> Me { set; get; }
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

        public string MsgHandle(string userName, string msg, int type)
        {
            var time = this._win.Dispatcher.Invoke(() => { return int.Parse(_dw.time.Text); });
            var replaceDic = this._win.Dispatcher.Invoke(() => { return _dw.ReplaceDic; });
            var key = this._win.Dispatcher.Invoke(() => { return _dw.apiKey.Text; });

            if (string.IsNullOrEmpty(key)) return null;
            var reMsg = _tc.GetChat(key, RuleUser.ContainsKey(userName) ? RuleUser[userName] : "null", msg);
            var isRandom = this._win.Dispatcher.Invoke(() => { return _dw.isRandom.IsChecked; });

            foreach (var item in replaceDic)
            {
                reMsg.Replace(item.Key, item.Value);
            }

            if (isRandom == true)
            {
                time = (new Random()).Next(1000 * time);
            }

            if (msg.Contains("@" + Me.Item1) || (type == 1 && !userName.StartsWith("@@")))
            {
                System.Threading.Thread.Sleep(time);
                return reMsg;
            }
            else
            {
                return null;
            }
        }
    }
}
