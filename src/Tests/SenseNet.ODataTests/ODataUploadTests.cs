using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Services.Core.Operations;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataUploadTests : ODataTestBase
    {
        private static readonly string PageComponentCTD = @"<ContentType name='PageComponent' parentType='Folder' handler='SenseNet.ContentRepository.Folder' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>PageComponent</DisplayName>
  <Description></Description>
  <Icon>Folder</Icon>
  <AllowedChildTypes>
    Folder,File
  </AllowedChildTypes>
  <Fields>
    <Field name='Hidden' type='Boolean'>
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
      </Configuration>
    </Field>
    <Field name='MarginTop' type='Number'>
      <DisplayName>Margin top</DisplayName>
      <Description>margin in px</Description>
      <Configuration>
        <MinValue>0</MinValue>
        <MaxValue>1000</MaxValue>
        <Digits>0</Digits>
        <DefaultValue>30</DefaultValue>
      </Configuration>
    </Field>
    <Field name='MarginBottom' type='Number'>
      <DisplayName>Margin bottom</DisplayName>
      <Description>margin in px</Description>
      <Configuration>
        <MinValue>0</MinValue>
        <MaxValue>1000</MaxValue>
        <Digits>0</Digits>
        <DefaultValue>30</DefaultValue>
      </Configuration>
    </Field>
  </Fields>
</ContentType>";
        private static readonly string BlogPostCTD = @"<ContentType name='BlogPost' parentType='PageComponent' handler='SenseNet.ContentRepository.Folder' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>Blog post</DisplayName>
  <Icon>Folder</Icon>
  <AllowedChildTypes>
  </AllowedChildTypes>
  <Fields>
    <Field name='Title' type='ShortText'>
      <DisplayName>Title</DisplayName>
      <Description></Description>
    </Field>
    <Field name='Image' type='ShortText'>
      <DisplayName>Image</DisplayName>
      <Description></Description>
    </Field>
    <Field name='Author' type='ShortText'>
      <DisplayName>Author</DisplayName>
      <Description></Description>
    </Field>
    <Field name='PublishDate' type='DateTime'>
      <DisplayName>Publish date</DisplayName>
      <Configuration>
        <DateTimeMode>Date</DateTimeMode>
      </Configuration>
    </Field>
    <Field name='Tags' type='LongText'>
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
        <TextType>LongText</TextType>
      </Configuration>
    </Field>
    <Field name='Description' type='LongText'>
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
        <TextType>LongText</TextType>
      </Configuration>
    </Field>
    <Field name='Body' type='LongText'>
      <DisplayName>Body</DisplayName>
      <Description></Description>
      <Configuration>
        <TextType>LongText</TextType>
      </Configuration>
    </Field>
    <Field name='MarginTop' type='Number'>
      <Configuration>
	    <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
    <Field name='MarginBottom' type='Number'>
      <Configuration>
	    <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
  </Fields>
</ContentType>";
        private static readonly string BlogPostCTD_updated = @"<ContentType name='BlogPost' parentType='PageComponent' handler='SenseNet.ContentRepository.Folder' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>Blog post</DisplayName>
  <Icon>Folder</Icon>
  <AllowedChildTypes>
  </AllowedChildTypes>
  <Fields>
    <Field name='Title' type='ShortText'>
      <DisplayName>Title</DisplayName>
      <Description></Description>
    </Field>
    <Field name='Image' type='ShortText'>
      <DisplayName>Image</DisplayName>
      <Description></Description>
    </Field>
    <Field name='Author' type='ShortText'>
      <DisplayName>Author</DisplayName>
      <Description></Description>
    </Field>
    <Field name='PublishDate' type='DateTime'>
      <DisplayName>Publish date</DisplayName>
      <Configuration>
        <DateTimeMode>Date</DateTimeMode>
      </Configuration>
    </Field>
    <Field name='Tags' type='LongText'>
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
        <TextType>LongText</TextType>
      </Configuration>
    </Field>
    <Field name='Description' type='LongText'>
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
        <TextType>LongText</TextType>
      </Configuration>
    </Field>
    <Field name='Body' type='LongText'>
      <DisplayName>Body</DisplayName>
      <Description></Description>
      <Configuration>
        <TextType>LongText</TextType>
      </Configuration>
    </Field>
    <Field name='MarginTop' type='Number'>
      <Configuration>
	    <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
    <Field name='MarginBottom' type='Number'>
      <Configuration>
	    <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
    <Field name='ImageAlt' type='ShortText'>
      <DisplayName>Alt</DisplayName>
      <Description>Image alt attribute</Description>
    </Field>
  </Fields>
</ContentType>";

        [TestMethod]
        public void OD_Upload_UpdateCTD_BUG1292()
        {
            IsolatedODataTest(() =>
            {
                // ARRANGE
                ContentTypeInstaller.InstallContentType(PageComponentCTD, BlogPostCTD);
                var contentTypesFolder = Content.Load(Repository.ContentTypesFolderPath);
                var httpContext = new DefaultHttpContext();

                var contentFolder = Content.Load("/Root/Content");
                var contentFolderGc = (GenericContent)contentFolder.ContentHandler;
                contentFolderGc.AllowChildType("BlogPost", save: true);

                var blogPost = Content.CreateNew("BlogPost", contentFolderGc, "BlogPost1");
                blogPost.Save();

                Assert.IsFalse(blogPost.Fields.ContainsKey("ImageAlt"));

                // ACTION
                //var response = await ODataPostAsync(
                //        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/ODataError",
                //        "?$format=table",
                //        $@"{{""errorType"":""NodeAlreadyExistsException""}}")
                //    .ConfigureAwait(false);
                var response2 = UploadActions.Upload(
                        Content.Load("/Root/System/Schema/ContentTypes/GenericContent/Folder/PageComponent"),
                        httpContext,
                        ContentType:"ContentType",
                        FileLength:2230,
                        FileName: "BlogPost",
                        PropertyName:"Binary",
                        Overwrite:true,
                        ChunkToken: "0*0*False*False",
                        FileText: BlogPostCTD_updated)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                //AssertNoError(response);
                var loaded = Content.Load(blogPost.Id);
                Assert.IsTrue(loaded.Fields.ContainsKey("ImageAlt"));
            });
        }
    }
}
