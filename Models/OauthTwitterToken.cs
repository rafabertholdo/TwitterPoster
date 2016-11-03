using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TwitterPoster.Models
{
    public class OauthTwitterToken
    {
        private string _url;
        private string _timestamp;
        private string _nonce;
        private string _signature;
        private string _message;        

        private static readonly Encoding _encoding = Encoding.UTF8;

        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string Token { get; set; }
        public string TokenSecret { get; set; }

        public string Version { get { return "1.0"; } }

        public string SignatureMethod
        {
            get
            {
                return "HMAC-SHA1";
            }
        }

        private string Signaure
        {
            get
            {
                if (_signature == null)
                {
                    _signature = CreateSignature();
                }
                return _signature;
            }
        }        

        public OauthTwitterToken(string url, string message)
        {
            _url = url;
            _message = message;
            _nonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture)));                       
            _timestamp = DateTime.UtcNow.ToUnixTime().ToString();
        }

        public static string UrlEncodeRelaxed(string value)
        {
            return Uri.EscapeDataString(value).Replace("(", "(".PercentEncode()).Replace(")", ")".PercentEncode());
        }

        private string CreateSignature()
        {
            //string builder will be used to append all the key value pairs
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("POST&");
            stringBuilder.Append(Uri.EscapeDataString(_url));
            stringBuilder.Append("&");

            //the key value pairs have to be sorted by encoded key
            var dictionary = new SortedDictionary<string, string>
                                 {
                                     {"include_entities", "1" },
                                     {"include_rts", "1" },
                                     {"oauth_version", this.Version},
                                     {"oauth_consumer_key", this.ConsumerKey},
                                     {"oauth_nonce", _nonce},
                                     {"oauth_signature_method", this.SignatureMethod},
                                     {"oauth_timestamp", _timestamp},
                                     {"oauth_token", this.Token},
                                     {"status", _message}                                    
                                 };

            foreach (var keyValuePair in dictionary)
            {
                //append a = between the key and the value and a & after the value
                stringBuilder.Append(Uri.EscapeDataString(string.Format("{0}={1}&", keyValuePair.Key, keyValuePair.Value)));
            }
            string signatureBaseString = stringBuilder.ToString().Substring(0, stringBuilder.Length - 3);

            //generation the signature key the hash will use
            string signatureKey =
                Uri.EscapeDataString(this.ConsumerSecret) + "&" +
                Uri.EscapeDataString(this.TokenSecret);
            
            var hmacsha1 = new HMACSHA1(
                _encoding.GetBytes(signatureKey));

            //hash the values
            string signatureString = Convert.ToBase64String(
                hmacsha1.ComputeHash(
                    _encoding.GetBytes(signatureBaseString)));            

            return signatureString;            
        }
        
        public override string ToString()
        {
            string authorizationHeaderParams = String.Empty;            
            authorizationHeaderParams += "oauth_consumer_key="
                                         + "\"" + Uri.EscapeDataString(this.ConsumerKey) + "\",";

            authorizationHeaderParams += "oauth_nonce=" + "\"" +
                                         Uri.EscapeDataString(this._nonce) + "\",";

            authorizationHeaderParams += "oauth_signature=" + "\""
                                         + Uri.EscapeDataString(this.Signaure) + "\",";

            authorizationHeaderParams +=
                "oauth_signature_method=" + "\"" +
                Uri.EscapeDataString(this.SignatureMethod) +
                "\",";

            authorizationHeaderParams += "oauth_timestamp=" + "\"" +
                                         Uri.EscapeDataString(_timestamp) + "\",";            

            authorizationHeaderParams += "oauth_token=" + "\"" +
                                         Uri.EscapeDataString(this.Token) + "\",";            

            authorizationHeaderParams += "oauth_version=" + "\"" +
                                         Uri.EscapeDataString(this.Version) + "\"";
            return authorizationHeaderParams;
        }        
    }
}
