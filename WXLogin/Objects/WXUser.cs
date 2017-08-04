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
                _nickName = value;
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
                _remarkName = value;
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
        Unkown,
        /// <summary>
        /// 朋友
        /// </summary>
        Friend,
        /// <summary>
        /// 公众号
        /// </summary>
        PublicFriend,
        /// <summary>
        /// 讨论组
        /// </summary>
        ChatRoom
    }
}
