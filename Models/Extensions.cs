using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TwitterPoster.Models
{
    public static class Extensions
    {
        public static long ToUnixTime(this DateTime dateTime)
        {
            return (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static byte[] GetBytes(this string input)
        {
            return Encoding.UTF8.GetBytes(input);
        }

        public static string PercentEncode(this string s)
        {
            byte[] bytes = s.GetBytes();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte num in bytes)
            {
                if ((int)num > 7 && (int)num < 11 || (int)num == 13)
                    stringBuilder.Append(string.Format("%0{0:X}", (object)num));
                else
                    stringBuilder.Append(string.Format("%{0:X}", (object)num));
            }
            return stringBuilder.ToString();
        }

        public static string HashWith(this string input, HashAlgorithm algorithm)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(algorithm.ComputeHash(bytes));
        }
    }
}
