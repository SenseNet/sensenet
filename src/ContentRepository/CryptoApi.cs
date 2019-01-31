using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Web;
using System.IO;

namespace SenseNet.ContentRepository
{
    public static class CryptoApi
    {
        public static CipherMode CMode = CipherMode.CBC;
        public static RijndaelManaged provider = new RijndaelManaged();

        public static string Decrypt(string input, string passphrase, string IVString)
        {
            var provider = new RijndaelManaged();
            var hashMD5 = new MD5CryptoServiceProvider();
            byte[] key = hashMD5.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(passphrase));
            byte[] IV = hashMD5.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(IVString));

            provider.Mode = CMode;


            byte[] inputbytes = UrlTokenDecode(input);

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, provider.CreateDecryptor(key, IV), CryptoStreamMode.Write);

            cs.Write(inputbytes, 0, inputbytes.Length);

            cs.FlushFinalBlock();
            ms.Position = 0;
            string res = new StreamReader(ms).ReadToEnd();
            return res;
        }

        public static string Crypt(string input, string passphrase, string IVString)
        {
            byte[] inputbytes = Encoding.Default.GetBytes(input);

            var provider = new RijndaelManaged();
            var hashMD5 = new MD5CryptoServiceProvider();
            byte[] key = hashMD5.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(passphrase));
            byte[] IV = hashMD5.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(IVString));

            provider.Mode = CMode;

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, provider.CreateEncryptor(key, IV),
                CryptoStreamMode.Write);
            cs.Write(inputbytes, 0, inputbytes.Length);
            cs.FlushFinalBlock();


            byte[] bytes = ms.ToArray();

            string base64 = UrlTokenEncode(bytes);
            return base64;
        }

        internal static byte[] UrlTokenDecode(string text)
        {
            var length = text.Length - 1;
            var paddingCharCount = text[length] - '0';
            var buffer = new char[length + paddingCharCount];

            for (var i = 0; i < buffer.Length; i++)
            {
                if (i >= length)
                    buffer[i] = '=';
                else
                    switch (text[i])
                    {
                        case '-': buffer[i] = '+'; break;
                        case '_': buffer[i] = '/'; break;
                        default: buffer[i] = text[i]; break;
                    }
            }

            return Convert.FromBase64CharArray(buffer, 0, buffer.Length);
        }

        internal static string UrlTokenEncode(byte[] bytes)
        {
            var b64 = Convert.ToBase64String(bytes);
            var p = b64.Length - 1;
            while (b64[p] == '=')
                p--;

            var buffer = new char[p + 2];
            buffer[p + 1] = (char) ('0' + b64.Length - 1 - p);

            for (var i = 0; i < buffer.Length - 1; i++)
            {
                switch (b64[i])
                {
                    case '+': buffer[i] = '-'; break;
                    case '/': buffer[i] = '_'; break;
                    default: buffer[i] = b64[i]; break;
                }
            }

            return new string(buffer);
        }

    }
}
