using System;
using System.Drawing;
using System.IO;

namespace SenseNet.Preview
{
    public interface IPreviewGenerationContext
    {
        int ContentId { get; }
        int PreviewsFolderId { get; }
        int StartIndex { get; }
        int MaxPreviewCount { get; }
        int PreviewResolution { get; }
        string Version { get; }

        void SetPageCount(int pageCount);
        void SetIndexes(int pageCount, out int firstIndex, out int lastIndex);

        void SavePreviewAndThumbnail(Stream imgStream, int page);
        void SaveEmptyPreview(int page);
        void SaveImage(Bitmap image, int page);

        void LogInfo(int page, string message);
        void LogWarning(int page, string message);
        void LogError(int page, string message = null, Exception ex = null);
    }
}
