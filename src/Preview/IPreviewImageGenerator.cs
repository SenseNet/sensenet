using System.IO;

namespace SenseNet.Preview
{
    public interface IPreviewImageGenerator
    {
        string[] KnownExtensions { get; }
        string GetTaskNameByExtension(string extension);
        string GetTaskTitleByExtension(string extension);
        string[] GetSupportedTaskNames();
        void GeneratePreview(Stream docStream, IPreviewGenerationContext context);
    }

}
