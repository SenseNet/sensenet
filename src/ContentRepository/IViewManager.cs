using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository
{
    public interface IViewManager
    {
        void AddToDefaultView(FieldSetting fieldSetting, ContentList contentList);
        void RemoveFieldFromViews(FieldSetting fieldSetting, ContentList contentList);
    }
}
