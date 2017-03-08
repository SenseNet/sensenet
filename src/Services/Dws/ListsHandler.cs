using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Web.Services.Description;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository;

namespace SenseNet.Portal.Dws
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class ListsHandler : System.Web.Services.WebService
    {
        // =========================================================================================== Public webservice methods
        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/CheckOutFile", RequestNamespace="http://schemas.microsoft.com/sharepoint/soap/", ResponseNamespace="http://schemas.microsoft.com/sharepoint/soap/", Use=SoapBindingUse.Literal, ParameterStyle=SoapParameterStyle.Wrapped)]
        [WebMethod]
        public bool CheckOutFile(string pageUrl, string checkoutToLocal, string lastmodified)
        {
            // <pageUrl>http://snbppc070/Root/Sites/Default_Site/workspaces/Document/losangelesdocumentworkspace/Document_Library/Duis%20et%20lorem.doc</pageUrl>
            // <checkoutToLocal>true</checkoutToLocal>
            // <lastmodified>3/25/2011 3:25:14 PM</lastmodified></CheckOutFile>

            if (DwsHelper.CheckVisitor())
                return false;

            var path = DwsHelper.GetPathFromUrl(pageUrl);
            var node = Node.LoadNode(path);
            if (node != null)
            {
                if (!node.Lock.Locked)
                {
                    try
                    {
                        var gc = node as GenericContent;
                        gc.CheckOut();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex);
                    }
                }
            }

            return false;
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/CheckInFile", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public bool CheckInFile(string pageUrl, string comment, string CheckinType)
        {
            // <pageUrl>http://snbppc070/Root/Sites/Default_Site/workspaces/Document/romedocumentworkspace/Document_Library/Aenean%20semper.doc</pageUrl>
            // <comment>fff</comment>
            // <CheckinType>0</CheckinType>

            if (DwsHelper.CheckVisitor())
                return false;

            var path = DwsHelper.GetPathFromUrl(pageUrl);
            var node = Node.LoadNode(path);
            if (node != null)
            {
                if (node.Lock.Locked && node.LockedById == ContentRepository.User.Current.Id)
                {
                    try
                    {
                        var gc = node as GenericContent;
                        gc["CheckInComments"] = comment;
                        if (CheckinType == "0")
                            gc.CheckIn();       // from 1.5 -> 1.6
                        if (CheckinType == "1")
                            gc.Publish();       // from 1.5 -> 2.0
                        if (CheckinType == "2")
                            return false;       // from 1.5 -> 1.5, not implemented

                        return true;
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex);
                    }
                }
            }

            return false;
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/UndoCheckOut", RequestNamespace="http://schemas.microsoft.com/sharepoint/soap/", ResponseNamespace="http://schemas.microsoft.com/sharepoint/soap/", Use=SoapBindingUse.Literal, ParameterStyle=SoapParameterStyle.Wrapped)] 
        [WebMethod]
        public bool UndoCheckOut (string pageUrl)
        {
            if (DwsHelper.CheckVisitor())
                return false;

            var path = DwsHelper.GetPathFromUrl(pageUrl);
            var node = Node.LoadNode(path);
            if (node != null)
            {
                if (node.Lock.Locked && node.LockedById == ContentRepository.User.Current.Id)
                {
                    try
                    {
                        var gc = node as GenericContent;
                        gc.UndoCheckOut();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex);
                    }
                }
            }
            return false;
        }
    }
}
