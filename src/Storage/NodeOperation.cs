using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    public static class NodeOperation
    {
        public static string TemplateCreation { get { return "TemplateCreation"; } }
        public static string TemplateChildCopy { get { return "TemplateChildCopy"; } }
        public static string HiddenJournal { get { return "HiddenJournal"; } }

        public static string UndoCheckOut = "UndoCheckOut";
    }
}
