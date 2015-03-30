using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WhatsAppApi.Parser;
using WhatsAppApi.Settings;

namespace WhatsAppApi.Register
{
    public class WhatsRegisterV2
    {
        public string GetUserAgent()
        {
            return WaBuildHashData.UserAgent;
        }

        public string GenerateIdentity(string phoneNumber, string salt = "")
        {
            return (phoneNumber + salt).Reverse().ToSHAString();
        }

        public string GetToken(string number)
        {
            return WaToken.GenerateToken(number, WaBuildHashData.Token);
        }

        public bool RequestCode(string phoneNumber, out string password, string method = "sms", string id = null)
        {
            string response = string.Empty;
            return RequestCode(phoneNumber, out password, out response, method, id);
        }

        public bool RequestCode(string phoneNumber, out string password, out string response, string method = "sms", string id = null)
        {
            string request = string.Empty;
            return RequestCode(phoneNumber, out password, out request, out response, method, id);
        }

        public bool RequestCode(string phoneNumber, out string password, out string request, out string response, string method = "sms", string id = null)
        {
            response = null;
            password = null;
            request = null;
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    //auto-generate
                    id = GenerateIdentity(phoneNumber);
                }

                PhoneNumber pn = new PhoneNumber(phoneNumber);
                string token = this.GetToken(pn.Number);
                
                request = string.Format("https://v.whatsapp.net/v2/code?cc={0}&in={1}&to={0}{1}&method={2}&sim_mcc={3}&sim_mnc={4}&token={5}&id={6}&lg={7}&lc={8}", pn.CC, pn.Number, method, pn.MCC, pn.MNC, token, id, pn.ISO639, pn.ISO3166);
                response = GetResponse(request);
                password = response.GetJsonValue("pw");
                if (!string.IsNullOrEmpty(password))
                {
                    return true;
                }
                return (response.GetJsonValue("status") == "sent");
            }
            catch(Exception e)
            {
                response = e.Message;
                return false;
            }
        }

        public string RegisterCode(string phoneNumber, string code, string id = null)
        {
            string response = string.Empty;
            return this.RegisterCode(phoneNumber, code, out response, id);
        }

        public string RegisterCode(string phoneNumber, string code, out string response, string id = null)
        {
            response = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    //auto generate
                    id = this.GenerateIdentity(phoneNumber);
                }
                PhoneNumber pn = new PhoneNumber(phoneNumber);

                string uri = string.Format("https://v.whatsapp.net/v2/register?cc={0}&in={1}&id={2}&code={3}", pn.CC, pn.Number, id, code);
                response = this.GetResponse(uri);
                if (response.GetJsonValue("status") == "ok")
                {
                    return response.GetJsonValue("pw");
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public string RequestExist(string phoneNumber, string id = null)
        {
            string response = string.Empty;
            return RequestExist(phoneNumber, out response, id);
        }

        public string RequestExist(string phoneNumber, out string response, string id = null)
        {
            response = string.Empty;
            try
            {
                if (String.IsNullOrEmpty(id))
                {
                    id = GenerateIdentity(phoneNumber);
                }
                PhoneNumber pn = new PhoneNumber(phoneNumber);
                string uri = string.Format("https://v.whatsapp.net/v2/exist?cc={0}&in={1}&id={2}", pn.CC, pn.Number, id);
                response = GetResponse(uri);
                if (response.GetJsonValue("status") == "ok")
                {
                    return response.GetJsonValue("pw");
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string GetResponse(string uri)
        {
            HttpWebRequest request = HttpWebRequest.Create(new Uri(uri)) as HttpWebRequest;
            request.KeepAlive = false;
            request.UserAgent = this.GetUserAgent();
            request.Accept = "text/json";
            using (var reader = new System.IO.StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadLine();
            }
        }

        public static string UrlEncode(string data)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in data.ToCharArray())
            {
                int i = (int)c;
                if (
                    (
                        i >= 0 && i <= 31
                    )
                    ||
                    (
                        i >= 32 && i <= 47
                    )
                    ||
                    (
                        i >= 58 && i <= 64
                    )
                    ||
                    (
                        i >= 91 && i <= 96
                    )
                    ||
                    (
                        i >= 123 && i <= 126
                    )
                    ||
                    i > 127
                )
                {
                    //encode 
                    sb.Append('%');
                    sb.AppendFormat("{0:x2}", (byte)c);
                }
                else
                {
                    //do not encode
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static class WaBuildHashData
        {
            public const string UserAgent = "WhatsApp/2.12.68 S40Version/14.26 Device/Nokia302";
            public const string Token = "PdA2DJyKoUrwLw1Bg6EIhzh502dF9noR9uFCllGk1425519315543{0}";
        }
    }

    public static class WaExtenders
    {
        public static string ToSHAString(this IEnumerable<char> s)
        {
            return new string(s.ToArray()).ToSHAString();
        }

        public static string ToSHAString(this String s)
        {
            byte[] data = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(s));
            string str = Encoding.GetEncoding("iso-8859-1").GetString(data);
            str = WhatsRegisterV2.UrlEncode(str).ToLower();
            return str;
        }

        public static string ToMD5String(this IEnumerable<char> s)
        {
            return new string(s.ToArray()).ToMD5String();
        }

        public static string ToMD5String(this String s)
        {
            return string.Join(string.Empty, MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(s)).Select(item => item.ToString("x2")).ToArray());
        }


        public static void GetLanguageAndLocale(this CultureInfo self, out string language, out string locale)
        {
            string name = self.Name;
            int n1 = name.IndexOf('-');
            if (n1 > 0)
            {
                int n2 = name.LastIndexOf('-');
                language = name.Substring(0, n1);
                locale = name.Substring(n2 + 1);
            }
            else
            {
                language = name;
                switch (language)
                {
                    case "cs":
                        locale = "CZ";
                        return;

                    case "da":
                        locale = "DK";
                        return;

                    case "el":
                        locale = "GR";
                        return;

                    case "ja":
                        locale = "JP";
                        return;

                    case "ko":
                        locale = "KR";
                        return;

                    case "sv":
                        locale = "SE";
                        return;

                    case "sr":
                        locale = "RS";
                        return;
                }
                locale = language.ToUpper();
            }
        }

        public static string GetJsonValue(this String s, string parameter)
        {
            Match match;
            if ((match = Regex.Match(s, string.Format("\"?{0}\"?:\"(?<Value>.+?)\"", parameter), RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success)
            {
                return match.Groups["Value"].Value;
            }
            return null;
        }
    }
}
