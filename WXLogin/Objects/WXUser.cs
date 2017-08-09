using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Linq;

namespace WXLogin
{
    /// <summary>
    /// 微信用户
    /// </summary>
    public class WXUser
    {
        private static readonly List<WXMsg> Messages = new List<WXMsg>();
        // 用户类型
        public UserType UserType { set; get; }
        // 子用户，用于讨论组
        public List<WXUser> MemberList { set; get; }
        // 显示名称
        public string DisplayName { set; get; }
        //用户id
        private string _userName;
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
            }
        }
        //昵称
        private string _nickName;
        public string NickName
        {
            get
            {
                return _nickName;
            }
            set
            {
                _nickName = WXService.DecodeMsgFace(value);
            }
        }
        //头像url
        private string _headImgUrl;
        public string HeadImgUrl
        {
            get
            {
                return _headImgUrl;
            }
            set
            {
                _headImgUrl = value;
            }
        }
        //备注名
        private string _remarkName;
        public string RemarkName
        {
            get
            {
                return _remarkName;
            }
            set
            {
                _remarkName = WXService.DecodeMsgFace(value);
            }
        }
        //性别 男1 女2 其他0
        private string _sex;
        public string Sex
        {
            get
            {
                return _sex;
            }
            set
            {
                _sex = value;
            }
        }
        //签名
        private string _signature;
        public string Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                _signature = value;
            }
        }
        //城市
        private string _city;
        public string City
        {
            get
            {
                return _city;
            }
            set
            {
                _city = value;
            }
        }
        //省份
        private string _province;
        public string Province
        {
            get
            {
                return _province;
            }
            set
            {
                _province = value;
            }
        }
        //昵称全拼
        private string _pyQuanPin;
        public string PYQuanPin
        {
            get
            {
                return _pyQuanPin;
            }
            set
            {
                _pyQuanPin = value;
            }
        }
        //备注名全拼
        private string _remarkPYQuanPin;
        public string RemarkPYQuanPin
        {
            get
            {
                return _remarkPYQuanPin;
            }
            set
            {
                _remarkPYQuanPin = value;
            }
        }

        //头像
        private bool _loading_icon = false;
        private byte[] _icon;
        public byte[] Icon
        {
            get
            {
                if (_icon == null && !_loading_icon)
                {
                    _loading_icon = true;

                    WXService wxs = WXService.Instance;
                    if (_userName.Contains("@@"))  //讨论组
                    {
                        _icon = wxs.GetHeadImg(_userName);
                    }
                    else if (_userName.Contains("@"))  //好友
                    {
                        _icon = wxs.GetIcon(_userName);
                    }
                    else
                    {
                        _icon = wxs.GetIcon(_userName);
                    }

                    _loading_icon = false;
                }
                return _icon;
            }
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string ShowName => string.IsNullOrEmpty(_remarkName) ? _nickName : _remarkName;

        /// <summary>
        /// 显示的拼音全拼
        /// </summary>
        public string ShowPinYin => string.IsNullOrEmpty(_remarkPYQuanPin) ? _pyQuanPin : _remarkPYQuanPin;

        /// <summary>
        /// 获取用户类型
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="verifyflag"></param>
        /// <returns></returns>
        public static UserType GetUserType(string userName, string verifyflag)
        {
            // except for these, there also have some other type
            // if you interest for this, please search "isBrand" key in
            // chrome sources -> res.wx.qq.com -> a/wx_fed/webwx... -> js -> index_....js
            // and you will see all user type
            switch (userName)
            {
                case "newsapp":
                    return UserType.NewsApp;
                case "filehelper":
                    return UserType.FileHelper;
                case "fmessage":
                    return UserType.Fmessage;
            }

            if (userName.StartsWith("@@")) return UserType.ChatRoom;
            if (Convert.ToBoolean(int.Parse(verifyflag) & 8)) return UserType.BrandContact;
            return UserType.Friend;
        }

    }

    public class ChatRoomUser
    {
        public string DisplayName { set; get; }
        public string NickName { set; get; }
        public string PYQuanPin { set; get; }
        public string UserName { set; get; }
    }
    public enum UserType
    {
        /// <summary>
        /// 暂时未知
        /// </summary>
        Unkown = 0,
        /// <summary>
        /// 朋友
        /// </summary>
        Friend = 2,
        /// <summary>
        /// 公众号
        /// </summary>
        BrandContact = 4,
        /// <summary>
        /// 讨论组
        /// </summary>
        ChatRoom = 8,
        /// <summary>
        /// 新闻号
        /// </summary>
        NewsApp = 16,
        /// <summary>
        /// Fmessage
        /// </summary>
        Fmessage = 32,
        /// <summary>
        /// 文件助手
        /// </summary>
        FileHelper = 64

    }
}
