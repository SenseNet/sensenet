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
        private static readonly string WebContent = @"<ContentType name='WebContent' parentType='ListItem' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>$Ctd-WebContent,DisplayName</DisplayName>
  <Description>$Ctd-WebContent,Description</Description>
  <Icon>WebContent</Icon>
  <Fields>
    <Field name='ReviewDate' type='DateTime'>
      <DisplayName>$Ctd-WebContent,ReviewDate-DisplayName</DisplayName>
      <Description>$Ctd-WebContent,ReviewDate-Description</Description>
      <Configuration>
        <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
        <DateTimeMode>DateAndTime</DateTimeMode>
      </Configuration>
    </Field>
    <Field name='ArchiveDate' type='DateTime'>
      <DisplayName>$Ctd-WebContent,ArchiveDate-DisplayName</DisplayName>
      <Description>$Ctd-WebContent,ArchiveDate-Description</Description>
      <Configuration>
        <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
        <DateTimeMode>DateAndTime</DateTimeMode>
      </Configuration>
    </Field>
    <Field name='EnableLifespan' type='Boolean'>
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
      </Configuration>
    </Field>
    <Field name='ValidFrom' type='DateTime'>
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
      </Configuration>
    </Field>
    <Field name='ValidTill' type='DateTime'>
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
      </Configuration>
    </Field>
    <Field name='IsTaggable' type='Boolean'>
      <DisplayName>$Ctd-GenericContent,IsTaggable-DisplayName</DisplayName>
      <Description>$Ctd-GenericContent,IsTaggable-Description</Description>
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
        <DefaultValue>false</DefaultValue>
      </Configuration>
    </Field>
    <Field name='Tags' type='LongText'>
      <DisplayName>$Ctd-GenericContent,Tags-DisplayName</DisplayName>
      <Description>$Ctd-GenericContent,Tags-Description</Description>
      <Indexing>
        <IndexHandler>SenseNet.Search.Indexing.TagIndexHandler</IndexHandler>
      </Indexing>
      <Configuration>
        <ReadOnly>false</ReadOnly>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
        <ControlHint>sn:TagList</ControlHint>
      </Configuration>
    </Field>
  </Fields>
</ContentType>";
        private static readonly string ArticleCTD = @"<ContentType name='Article' parentType='WebContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>$Ctd-Article,DisplayName</DisplayName>
  <Description>$Ctd-Article,Description</Description>
  <Icon>WebContent</Icon>
  <Fields>
    <Field name='DisplayName' type='ShortText'>
      <DisplayName>$Ctd-Article,DisplayName-DisplayName</DisplayName>
      <Description>$Ctd-Article,DisplayName-Description</Description>
      <Configuration>
        <FieldIndex>0</FieldIndex>
      </Configuration>
    </Field>
    <Field name='Name' type='ShortText'>
      <DisplayName>$Ctd-GenericContent,Name-DisplayName</DisplayName>
      <Description>$Ctd-GenericContent,Name-Description</Description>
      <Configuration>
        <FieldIndex>1</FieldIndex>
      </Configuration>
    </Field>
    <Field name='Subtitle' type='ShortText'>
      <DisplayName>$Ctd-Article,Subtitle-DisplayName</DisplayName>
      <Description>$Ctd-Article,Subtitle-Description</Description>
      <Configuration>
        <FieldIndex>2</FieldIndex>
      </Configuration>
    </Field>
    <Field name='Author' type='ShortText'>
      <DisplayName>$Ctd-Article,Author-DisplayName</DisplayName>
      <Description>$Ctd-Article,Author-Description</Description>
      <Configuration>
        <FieldIndex>3</FieldIndex>
      </Configuration>
    </Field>
    <Field name='Lead' type='LongText'>
      <DisplayName>$Ctd-Article,Lead-DisplayName</DisplayName>
      <Description>$Ctd-Article,Lead-Description</Description>
      <Indexing>
        <Analyzer>Standard</Analyzer>
      </Indexing>
      <Configuration>
        <ControlHint>sn:RichText</ControlHint>
        <FieldIndex>4</FieldIndex>
      </Configuration>
    </Field>
    <Field name='Body' type='LongText'>
      <DisplayName>$Ctd-Article,Body-DisplayName</DisplayName>
      <Description>$Ctd-Article,Body-Description</Description>
      <Indexing>
        <Analyzer>Standard</Analyzer>
      </Indexing>
      <Configuration>
        <ControlHint>sn:RichText</ControlHint>
        <FieldIndex>5</FieldIndex>
      </Configuration>
    </Field>
    <Field name='Pinned' type='Boolean'>
      <DisplayName>$Ctd-Article,Pinned-DisplayName</DisplayName>
      <Description>$Ctd-Article,Pinned-Description</Description>
      <Configuration>
        <FieldIndex>6</FieldIndex>
      </Configuration>
    </Field>
    <Field name='Keywords' type='LongText'>
      <DisplayName>$Ctd-Article,Keywords-DisplayName</DisplayName>
      <Description>$Ctd-Article,Keywords-DisplayName</Description>
      <Indexing>
        <Analyzer>Whitespace</Analyzer>
      </Indexing>
      <Configuration>
        <ControlHint>sn:Textarea</ControlHint>
        <FieldIndex>7</FieldIndex>
      </Configuration>
    </Field>
    <Field name='ImageRef' type='Reference'>
      <DisplayName>$Ctd-Article,ImageRef-DisplayName</DisplayName>
      <Configuration>
        <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
        <AllowMultiple>false</AllowMultiple>
      </Configuration>
    </Field>
    <Field name='ImageData' type='Binary'>
      <DisplayName>$Ctd-Article,ImageData-DisplayName</DisplayName>
      <Configuration>
        <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
    <Field name='Image' type='Image'>
      <DisplayName>$Ctd-Article,Image-DisplayName</DisplayName>
      <Bind property='ImageRef' />
      <Bind property='ImageData' />
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
        <ControlHint>sn:Image</ControlHint>
        <FieldIndex>8</FieldIndex>
      </Configuration>
    </Field>
    <Field name='Description' type='LongText'>
      <Configuration>
        <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
    <Field name='Version' type='Version'>
      <Configuration>
        <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
    <Field name='IsTaggable' type='Boolean'>
      <Configuration>
        <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
    <Field name='Tags' type='LongText'>
      <Configuration>
        <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
  </Fields>
</ContentType>";
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
                ContentTypeInstaller.InstallContentType(WebContent, ArticleCTD, PageComponentCTD);
                var contentTypesFolder = Content.Load(Repository.ContentTypesFolderPath);
                var httpContext = new DefaultHttpContext();

                var contentFolder = Content.Load("/Root/Content");
                var contentFolderGc = (GenericContent)contentFolder.ContentHandler;
                contentFolderGc.AllowChildType("BlogPost", save: true);

                // ACTION-1
                var response1 = UploadActions.Upload(
                        Content.Load("/Root/System/Schema/ContentTypes/GenericContent/Folder/PageComponent"),
                        httpContext,
                        ContentType: "ContentType",
                        FileLength: 2230,
                        FileName: "BlogPost",
                        PropertyName: "Binary",
                        Overwrite: true,
                        ChunkToken: "0*0*False*False",
                        FileText: BlogPostCTD)
                    .ConfigureAwait(false).GetAwaiter().GetResult();



                // ACTION-2
                var blogPost = Content.CreateNew("BlogPost", contentFolderGc, "BlogPost1");
                blogPost.Save();

                // ASSERT-2
                Assert.IsFalse(blogPost.Fields.ContainsKey("ImageAlt"));

                // ACTION-3
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

                // ASSERT-3
                //AssertNoError(response);
                var loaded = Content.Load(blogPost.Id);
                Assert.IsTrue(loaded.Fields.ContainsKey("ImageAlt"));
            });
        }
    }
}
