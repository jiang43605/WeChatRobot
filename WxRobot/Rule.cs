using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WXLogin;

namespace WxRobot
{
    public class Rule
    {
        public static readonly Dictionary<WXUser, Rule> Rules = new Dictionary<WXUser, Rule>();
        private object _obj;
        private string _name;

        public string Name { get { return _name; } }
        public static Rule Default
        {
            get
            {
                return new Rule("Default");
            }
        }
        public static bool SetBingDing(WXUser user, Rule r)
        {
            if (user == null || r == null) return false;
            var exist = Rules.SingleOrDefault(o => o.Key.NickName == user.NickName && o.Key.UserName == user.UserName);

            if (default(KeyValuePair<WXUser, Rule>).Equals(exist))
            {
                Rules.Add(user, r);
            }
            else
            {
                if (exist.Value.Name != Rule.Default.Name)
                    exist.Value.RemoveUser(user.UserName);
                Rules[exist.Key] = r;
            }

            if (r.Name == Rule.Default.Name) return true;
            r.SetUser(user.UserName, user.NickName);
            r.BingDingAsync(user.UserName, (int)user.UserType);
            return true;
        }
        private Rule(string name)
        {
            this._name = name;
        }
        public Rule(object obj)
        {
            if (obj.GetType().GetMethods().Count(o => o.Name == "MsgHandle") < 1)
                throw new Exception("don't include MsgHandle method in this object!");

            this._obj = obj;
            this._name = obj.GetType().GetProperty("Name").GetValue(this._obj) as string;
        }

        /// <summary>
        /// set user to obj(extrenl dll)
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="nickName"></param>
        public void SetUser(string userName, string nickName)
        {
            var rs = this._obj.GetType().GetField("RuleUser")?.GetValue(this._obj) as Dictionary<string, string>;
            if (rs != null)
                rs.Add(userName, nickName);
        }
        public void RemoveUser(string userName)
        {
            var rs = this._obj.GetType().GetField("RuleUser")?.GetValue(this._obj) as Dictionary<string, string>;
            if (rs != null)
                rs.Remove(userName);
        }

        /// <summary>
        /// when user BingDing a rule
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="userType"></param>
        public async void BingDingAsync(string userName, int userType)
        {
            await Task.Run(() =>
            {
                this._obj.GetType().GetMethod("BingDing")?.Invoke(this._obj, new object[] { userName, userType });
            });
        }
        public string Invoke(string userName, string msg, int type, int userType)
        {
            return this._obj.GetType().GetMethod("MsgHandle")?.Invoke(this._obj, new object[] { userName, msg, type, userType }) as string;
        }

        public string FromMeInvoke(string toUserName, string msg, int type, int userType)
        {
            return this._obj.GetType().GetMethod("FromMe")?.Invoke(this._obj, new object[] { toUserName, msg, type, userType }) as string;
        }
    }
}
