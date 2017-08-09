using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json.Linq;

namespace WXLogin
{
    /// <summary>
    /// 微信消息
    /// </summary>
    public class WXMsg
    {
        /// <summary>
        /// 消息发送方
        /// </summary>
        public string From
        {
            get;
            set;
        }
        /// <summary>
        /// 消息发送方昵称
        /// </summary>
        public string FromNickName { set; get; }
        /// <summary>
        /// 消息接收方
        /// </summary>
        public string To
        {
            set;
            get;
        }
        /// <summary>
        /// 消息接收方昵称
        /// </summary>
        public string ToNickName { set; get; }
        /// <summary>
        /// 消息发送时间
        /// </summary>
        public DateTime Time
        {
            get;
            set;
        }
        /// <summary>
        /// 是否已读
        /// </summary>
        public bool Readed { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Msg
        {
            get;
            set;
        }
        /// <summary>
        /// 消息类型
        /// </summary>
        public int Type
        {
            get;
            set;
        }
    }

    /// <summary>
    /// interface for handle wx msg
    /// </summary>
    public interface IWXMsgHandle
    {
        string Handle(JToken msgInfo);
    }
}
