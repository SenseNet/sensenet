namespace SenseNet.Services.Wopi
{
    internal interface IWopiBinaryResponse
    {
        ContentRepository.File File { get; }
        string FileName { get; }
    }
}
