using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ImageTests : TestBase
    {
        [TestMethod]
        public void Image_SetDimensions()
        {
            Test(() =>
            {
                var imgContainer = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
                imgContainer.Save();

                var img = new Image(imgContainer, "Image") {Name = "img1.png"};
                img.Binary.SetStream(RepositoryTools.GetStreamFromString("this is not an image"));
                img.Save();

                //UNDONE: check dimensions?

                //UNDONE: set real image with real dimensions
            });
        }
    }
}
