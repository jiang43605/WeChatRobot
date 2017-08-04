using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;

namespace WXLogin
{
    /// <summary>
    /// 访问http服务器类
    /// </summary>
    public static class BaseService
    {
        public static string Path = "cookie";
        /// <summary>
        /// 访问服务器时的cookies
        /// </summary>
        public static CookieContainer CookiesContainer;
        /// <summary>
        /// 向服务器发送get请求  返回服务器回复数据
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static byte[] SendGetRequest(string url, CookieCollection cc = null)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "get";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.KeepAlive = true;

                if (CookiesContainer == null)
                {
                    CookiesContainer = new CookieContainer();
                }
                request.CookieContainer = new CookieContainer();  //启用cookie
                request.CookieContainer.Add(CookiesContainer.GetCookies(new Uri(url)));
                if (cc != null)
                {
                    request.CookieContainer.Add(cc);
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream response_stream = response.GetResponseStream();
                CookiesContainer.Add(response.Cookies);

                int count = (int)response.ContentLength;
                int offset = 0;
                byte[] buf = new byte[count];
                while (count > 0)  //读取返回数据
                {
                    int n = response_stream.Read(buf, offset, count);
                    if (n == 0) break;
                    count -= n;
                    offset += n;
                }
                return buf;
            }
            catch (TimeoutException t)
            {
                Console.WriteLine("超时！" + t.Message);
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        /// <summary>
        /// 向服务器发送post请求 返回服务器回复数据
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static byte[] SendPostRequest(string url, string body, bool AllowAutoRedirect = false, CookieCollection cc = null)
        {
            try
            {
                byte[] request_body = Encoding.UTF8.GetBytes(body);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "post";
                request.ContentLength = request_body.Length;
                request.AllowAutoRedirect = AllowAutoRedirect;
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.KeepAlive = true;

                if (CookiesContainer == null)
                {
                    CookiesContainer = new CookieContainer();
                }
                request.CookieContainer = new CookieContainer();  //启用cookie
                request.CookieContainer.Add(CookiesContainer.GetCookies(new Uri(url)));
                if (cc != null)
                {
                    request.CookieContainer.Add(cc);
                }

                Stream request_stream = request.GetRequestStream();
                request_stream.Write(request_body, 0, request_body.Length);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream response_stream = response.GetResponseStream();
                CookiesContainer.Add(response.Cookies);

                int count = (int)response.ContentLength;
                int offset = 0;
                byte[] buf = new byte[count];
                while (count > 0)  //读取返回数据
                {
                    int n = response_stream.Read(buf, offset, count);
                    if (n == 0) break;
                    count -= n;
                    offset += n;
                }
                return buf;
            }
            catch (TimeoutException t)
            {
                Console.WriteLine("超时！" + t.Message);
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        /// <summary>
        /// 获取指定cookie
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Cookie GetCookie(string name)
        {
            List<Cookie> cookies = GetAllCookies(CookiesContainer);
            foreach (Cookie c in cookies)
            {
                if (c.Name == name)
                {
                    return c;
                }
            }
            return null;
        }

        public static void SetCookie(string name, string value)
        {
            CookiesContainer.Add(new Cookie(name, value, "/", ".qq.com"));
        }

        public static List<Cookie> GetAllCookies(CookieContainer cc)
        {
            List<Cookie> lstCookies = new List<Cookie>();

            Hashtable table = (Hashtable)cc.GetType().InvokeMember("m_domainTable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.Instance, null, cc, new object[] { });

            foreach (object pathList in table.Values)
            {
                SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                    | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
                foreach (CookieCollection colCookies in lstCookieCol.Values)
                    foreach (Cookie c in colCookies) lstCookies.Add(c);
            }
            return lstCookies;
        }

        public static void SaveCookie()
        {
            var bf = new BinaryFormatter();

            using (var fs = new FileStream(Path, FileMode.Create))
            {
                bf.Serialize(fs, CookiesContainer);
            }
        }

        public static void GetSaveCookie()
        {

            var bf = new BinaryFormatter();
            CookiesContainer = bf.Deserialize(File.OpenRead(Path)) as CookieContainer;
        }

        public static string GetTime(int length=0)
        {
            var time = (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds;
            return length == 0 ? time.ToString() : time.ToString().Substring(0,length);
        }
    }
}
