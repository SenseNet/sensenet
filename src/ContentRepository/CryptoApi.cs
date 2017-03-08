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


            byte[] inputbytes = HttpServerUtility.UrlTokenDecode(input);

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

            string base64 = HttpServerUtility.UrlTokenEncode(bytes); // Convert.ToBase64String(bytes);
            return base64;
        }
    }
}
