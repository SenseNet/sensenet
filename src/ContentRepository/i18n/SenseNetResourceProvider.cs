using SenseNet.ContentRepository.Storage;
using System;
using System.Globalization;
using System.Resources;
using System.Web.Compilation;

namespace SenseNet.ContentRepository.i18n
{
    public class SenseNetResourceProvider : IResourceProvider
    {

        private string _className;

        public SenseNetResourceProvider(string className)
        {
            _className = className;
        }


        public object GetObject(string resourceKey, CultureInfo culture)
        {
            if (culture == null)
                culture = CultureInfo.CurrentUICulture;

            return SenseNetResourceManager.Current.GetObject(_className, resourceKey, culture);
        }

        public IResourceReader ResourceReader
        {
            get { throw new SnNotSupportedException(); }
        }

    }
}