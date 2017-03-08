using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("YesNo")]
    [DataSlot(0, RepositoryDataType.String, typeof(string), typeof(int), typeof(Enum))]
    [DefaultFieldSetting(typeof(YesNoFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.DropDown")]
    public class YesNoField : ChoiceField
    {
    }
}
