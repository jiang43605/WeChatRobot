using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WXLogin;

namespace WxRobot
{
    public class ImageConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var userName = value as string;

            Debug.Assert(userName != null, "userName != null");
            var iconBytes = userName.Contains("@@")
                ? WXLogin.WXService.Instance.GetHeadImg(userName)
                : WXLogin.WXService.Instance.GetIcon(userName);

            return InternalHelp.ConvertByteToBitmapImage(iconBytes);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageFormatConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var msgs = (ObservableCollection<WXMsg>)value;

            var rmsg = string.Empty;
            foreach (var item in msgs)
            {
                rmsg += $"[{item.FromNickName}]-[{item.ToNickName}]-[{item.Time}]\r\n"
                        + item.Msg + "\r\n\r\n";
            }

            return rmsg;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class MessageCountConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var msgs = (ObservableCollection<WXMsg>)value;

            var num = msgs.Count(o => o.From != WXService.Instance.Me.UserName && o.Readed != true);
            return num == 0 ? "[ 点击聊天 ]" : $"[ {num} ]";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ContentToForegroundConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string)) return Brushes.DarkTurquoise;

            var str = (string)value;
            if (str == "[ 点击聊天 ]") return Brushes.DarkTurquoise;
            return Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class VisiableConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Visibility)value) == Visibility.Hidden ? false : true;
        }
    }

    public class CheckComparer : IEqualityComparer<WXLogin.WXUserViewModel>
    {
        public bool Equals(WXUserViewModel x, WXUserViewModel y)
        {
            return x.UserName == y.UserName;
        }

        public int GetHashCode(WXUserViewModel obj)
        {
            return 1;
        }
    }
    internal static class InternalHelp
    {
        internal static BitmapImage ConvertByteToBitmapImage(byte[] bytes)
        {
            try
            {
                if (bytes.Length == 0) return null;
                using (var m = new MemoryStream(bytes))
                {
                    using (var m1 = new MemoryStream())
                    {
                        System.Drawing.Image.FromStream(m).Save(m1, System.Drawing.Imaging.ImageFormat.Png);
                        var bitImage = new BitmapImage();
                        bitImage.BeginInit();
                        bitImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitImage.StreamSource = m1;
                        bitImage.EndInit();
                        bitImage.Freeze();
                        return bitImage;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("convert fail in bytes to BitmapImage: " + ex.Message);
                return null;
            }
        }
    }
}
