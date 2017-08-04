using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
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

    public class VisiableConvert:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Visibility) value) == Visibility.Hidden ? false : true;
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
