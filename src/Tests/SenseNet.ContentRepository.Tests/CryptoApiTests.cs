//UNDONE: Commented out because of missing HttpServerUtility
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Web;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using SenseNet.Tests.Core;

//namespace SenseNet.ContentRepository.Tests
//{
//    [TestClass]
//    public class CryptoApiTests : TestBase
//    {
//        [TestMethod]
//        public void CryptoApi_UrlTokenEncodeDecode()
//        {
//            for (byte size = 1; size < 20; size++)
//            {
//                var input = new byte[size];
//                for (var i = 0; i < input.Length; i++)
//                    input[i] = ((i % 2) == 0) ? (byte)248 : (byte)252;

//                var encoded = CryptoApi.UrlTokenEncode(input);
//                var output = CryptoApi.UrlTokenDecode(encoded);

//                Assert.AreEqual(HttpServerUtility.UrlTokenEncode(input), encoded);

//                Assert.AreEqual(input.Length, output.Length);
//                for (var i = 0; i < input.Length; i++)
//                    Assert.AreEqual(input[i], output[i]);
//            }

//        }
//    }
//}
