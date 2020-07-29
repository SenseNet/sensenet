using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class CryptoApiTests : TestBase
    {
        [TestMethod]
        public void CryptoApi_UrlTokenEncodeDecode()
        {
            for (byte size = 1; size < 20; size++)
            {
                var input = new byte[size];
                for (var i = 0; i < input.Length; i++)
                    input[i] = ((i % 2) == 0) ? (byte)248 : (byte)252;

                var encoded = CryptoApi.UrlTokenEncode(input);
                var output = CryptoApi.UrlTokenDecode(encoded);

                //Assert.AreEqual(HttpServerUtility.UrlTokenEncode(input), encoded);
                Assert.AreEqual(UrlTokenEncode(input), encoded);

                Assert.AreEqual(input.Length, output.Length);
                for (var i = 0; i < input.Length; i++)
                    Assert.AreEqual(input[i], output[i]);
            }

        }
        // HttpServerUtility.UrlTokenEncode(input) alternative
        // see https://stackoverflow.com/questions/50731397/httpserverutility-urltokenencode-replacement-for-netstandard
        private static string UrlTokenEncode(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (input.Length < 1)
                return String.Empty;
            char[] base64Chars = null;

            ////////////////////////////////////////////////////////
            // Step 1: Do a Base64 encoding
            string base64Str = Convert.ToBase64String(input);
            if (base64Str == null)
                return null;

            int endPos;
            ////////////////////////////////////////////////////////
            // Step 2: Find how many padding chars are present in the end
            for (endPos = base64Str.Length; endPos > 0; endPos--)
            {
                if (base64Str[endPos - 1] != '=') // Found a non-padding char!
                {
                    break; // Stop here
                }
            }

            ////////////////////////////////////////////////////////
            // Step 3: Create char array to store all non-padding chars,
            //      plus a char to indicate how many padding chars are needed
            base64Chars = new char[endPos + 1];
            base64Chars[endPos] = (char)((int)'0' + base64Str.Length - endPos); // Store a char at the end, to indicate how many padding chars are needed

            ////////////////////////////////////////////////////////
            // Step 3: Copy in the other chars. Transform the "+" to "-", and "/" to "_"
            for (int iter = 0; iter < endPos; iter++)
            {
                char c = base64Str[iter];

                switch (c)
                {
                    case '+':
                        base64Chars[iter] = '-';
                        break;

                    case '/':
                        base64Chars[iter] = '_';
                        break;

                    case '=':
                        Debug.Assert(false);
                        base64Chars[iter] = c;
                        break;

                    default:
                        base64Chars[iter] = c;
                        break;
                }
            }
            return new string(base64Chars);
        }
    }
}
