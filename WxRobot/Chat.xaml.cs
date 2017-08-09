using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using WXLogin;

namespace WxRobot
{
    /// <summary>
    /// Chat.xaml 的交互逻辑
    /// </summary>
    public partial class Chat : Window
    {
        private WXUserViewModel _user;
        public WXUserViewModel User
        {
            set
            {
                this.Title = value.DisplayName;
                _user = value;
            }
            get => _user;
        }

        public Chat(WXUserViewModel user)
        {
            InitializeComponent();
            this.print.DataContext = user;
            User = user;
            Icon = user.BitMapImage as BitmapImage;
            this.Loaded += Chat_Loaded;
        }

        private void Chat_Loaded(object sender, RoutedEventArgs e)
        {
            this.print.ScrollToEnd();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var from = WXService.Instance.Me.UserName;
                var to = User.UserName;
                var msg = input.Text;
                var fromNickName = WXService.Instance.GetNickName(from, msg);
                var toNickName = WXService.Instance.GetNickName(from, msg);

                User.Messages.Add(new WXMsg
                {
                    From = from,
                    To = to,
                    Msg = msg,
                    Type = 1,
                    FromNickName = fromNickName,
                    ToNickName = toNickName,
                    Time = DateTime.Now,
                    Readed = true
                });

                WXService.Instance.SendMsgAsync(msg, from, to, 1);
                this.input.Clear();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            print.TextChanged -= print_TextChanged;
            input.KeyUp -= TextBox_KeyUp;
            this.print.DataContext = null;
            _user = null;
            Icon = null;
        }

        private void print_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.print.ScrollToEnd();
        }
    }
}
