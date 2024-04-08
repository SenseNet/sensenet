using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests.Core;
using System.Threading;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.Testing;
using STT = System.Threading.Tasks;
using System.Globalization;

namespace SenseNet.ContentRepository.Tests;

[TestClass]
public class ChangedDataTests : TestBase
{
    private readonly CancellationToken _cancel = new CancellationTokenSource().Token;

    [TestMethod]
    public async STT.Task ChangedData_()
    {
        await Test(async () =>
        {
            var content = Content.Create(await CreateTestRoot());
            await content.CheckOutAsync(_cancel);
            content.Index++;
            await content.SaveAsync(_cancel);
            ChangedData[] changedData = null;

            // ACTION
            content.ContentHandler.Modified += (sender, args) =>
            {
                changedData = args.ChangedData.ToArray();
            };
            await content.CheckInAsync(_cancel);

            // ASSERT
            Assert.IsNotNull(changedData);
            Assert.IsTrue(changedData.Length > 0);
            Assert.IsTrue(changedData.Any(x => x.Name == "Index"));

        }).ConfigureAwait(false);
    }


    private async STT.Task<SystemFolder> CreateTestRoot()
    {
        var node = new SystemFolder(Repository.Root) { Name = "_ChangedDataTests" };
        await node.SaveAsync(CancellationToken.None).ConfigureAwait(false);
        return node;
    }

}