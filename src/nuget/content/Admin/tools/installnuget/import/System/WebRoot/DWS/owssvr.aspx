<%@ Page Language="C#" AutoEventWireup="true" %>

<script runat="server" type="text/C#">
    public string GetFilter()
    {
        var filter = HttpUtility.ParseQueryString(SenseNet.Portal.Virtualization.PortalContext.Current.RequestedUri.Query).Get("FileDialogFilterValue");
        if (String.IsNullOrEmpty(filter)) return String.Empty;
        return String.Concat(" +(", String.Join(" OR ", filter.Split(';').Select(f => "Name:" + f)), ")");
    }
</script>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <meta http-equiv="Expires" content="0">
    <title id="onetidTitle">File Properties</title>
    <style type="text/css">
        html, body {
            margin: 0;
            padding: 0;
            min-height: 100%;
        }

        body {
            font: 14px/1.2 Arial, Helvetica, sans-serif;
            color: #222;
            padding: 0 0 10px;
            background: url(/Root/Global/images/sn_bg.jpg) repeat-x 0 200px;
            cursor: default;
            -webkit-user-select: none;
            -khtml-user-select: none;
            -moz-user-select: none;
            -o-user-select: none;
            user-select: none;
        }

        #header {
            background: url(/Root/Global/images/sensenetlogo.png) no-repeat 14px 10px #007dc2;
            height: 70px;
            overflow: hidden;
            text-indent: -3000em;
            position: fixed;
            left: 0px;
            right: 0px;
        }

        h2 {
            font-size: 18px;
            font-weight: normal;
            margin: 0 0 5px 0;
            padding: 10px 10px 5px 10px;
            border-bottom: 1px solid #ccc;
        }

        #content {
            padding: 67px 10px 0;
        }

        table {
            margin: 0;
            padding: 5px;
            border: 1px solid transparent;
            border-collapse: collapse;
            width: 100%;
            font: 12px/1.2 Arial, Helvetica, sans-serif;
            cursor: default;
            empty-cells: show;
        }

            table tr.odd {
                background-color: #f9f9f9;
            }

            table tr.even {
                background-color: #fff;
            }

            table td, table th {
                text-align: left;
                padding: 4px;
            }

            table thead td {
                padding-left: 0;
                padding-right: 0;
            }

            table thead th {
                padding: 8px 4px;
                border-bottom: 1px solid #ccc;
            }

            table tbody td:first-child, table th:first-child, table tfoot td:first-child {
                padding-left: 10px;
            }

            table tbody tr:hover {
                background-color: #f0f0f0;
            }

            table tfoot td {
                padding-top: 15px;
                border-top: 1px solid #ccc;
            }

        .sn-icon {
            vertical-align: middle;
        }

        .sn-icon32 {
            margin: 0 5px 0 0;
        }

        .sn-icon-back {
            margin: 0 5px 0 0;
        }
    </style>
</head>
<script type="text/javascript">
    L_tooltipfile_Text = "";
    L_tooltipfolder_Text = "Double-click to open this location";
    selectedElement = null
    inChangeSelection = false
    slElem = null;
    oldSelection = "";
    bIsFileDialogView = true;
    function selectrow() {
        if (slElem) {
            slElem.className = oldSelection;
            slElem.title = "";
        }
        selectedElement = window.event.srcElement;
        while (selectedElement.tagName != "TR") {
            selectedElement = selectedElement.parentElement;
        }
        slElem = selectedElement;
        oldSelection = slElem.className;
        slElem.className = "ms-selected";
        if (slElem.getAttribute("fileattribute") == "file")
            slElem.title = L_tooltipfile_Text;
        else
            slElem.title = L_tooltipfolder_Text;


        if (window.getSelection) {
            if (window.getSelection().empty) {
                // Chrome 
                window.getSelection().empty();
            } else if (window.getSelection().removeAllRanges) {
                // Firefox
                window.getSelection().removeAllRanges();
            }
        } else if (document.selection) {
            // IE? 
            document.selection.empty();
        }
    }
</script>

<body servertype="OWS" doclibslist="1" onselectstart='return false;'>
    <div id="header">Sense/Net</div>
    <div id="content">
        <% var host = SenseNet.Portal.Dws.DwsHelper.GetHostStr();
           var site = SenseNet.ContentRepository.Content.Create(SenseNet.Portal.Virtualization.PortalContext.Current.Site);
           var uri = SenseNet.Portal.Virtualization.PortalContext.Current.RequestedUri;
           var location = HttpUtility.ParseQueryString(uri.Query).Get("location");
           if (!String.IsNullOrEmpty(location)) location = "/" + location;
           var reqContentPath = uri.AbsolutePath.Replace("/_vti_bin/owssvr.dll", String.Empty) + location;
           if (!uri.AbsolutePath.Contains("/Root")) reqContentPath = site.Path + reqContentPath;
           var reqContent = SenseNet.ContentRepository.Content.Load(reqContentPath); %>

        <table id="FileDialogViewTable">
            <thead>
                <tr>
                    <td colspan="4">
                        <h1><%= reqContent.DisplayName%></h1>
                    </td>
                </tr>
            </thead>
            <% var idx = 0;
               var Nodes = SenseNet.Search.ContentQuery.Query(String.Format("(TypeIs:Folder OR TypeIs:WorkSpace) AND InFolder:'{0}' .SORT:DisplayName", reqContentPath)).Nodes;
               if (Nodes.LongCount() > 0)
               { %>
            <thead>
                <tr>
                    <td colspan="4">
                        <h2 class="sn-pt-title">Folders and Workspaces</h2>
                    </td>
                </tr>
                <tr>
                    <th colspan="2">Name</th>
                    <th>Modified by</th>
                    <th>Modification date</th>
                </tr>
            </thead>
            <tbody>
                <% foreach (var cnt in Nodes.Select(node => SenseNet.ContentRepository.Content.Create(node)))
                   {
                       var cntId = host + (uri.AbsolutePath.Contains("/Root") ? cnt.Path : SenseNet.Portal.Virtualization.PortalContext.GetSiteRelativePath(cnt.Path));
                %>
                <tr fileattribute="folder" id="<%=cntId%>" class='<%=idx++%2==0?"odd":"even"%>' onmousedown="selectrow()" onclick="selectrow()">
                    <td style="width: 16px;" align="center">
                        <img src="/Root/Global/images/icons/16/<%= cnt.Fields["Icon"].GetData() %>.png" />
                    </td>
                    <td><%= cnt.DisplayName%></td>
                    <td><%= (cnt.Fields["ModifiedBy"].GetData() as SenseNet.ContentRepository.User).FullName%></td>
                    <td><%= (cnt.Fields["ModificationDate"].GetData()).ToString()%></td>
                </tr>
            </tbody>
            <% }
                   } %>

            <thead>
                <tr>
                    <td colspan="4">
                        <h2>Files</h2>
                    </td>
                </tr>
                <tr>
                    <th colspan="2">Name</th>
                    <th>Modified by</th>
                    <th>Modification date</th>
                </tr>
            </thead>
            <tbody>
                <% var idy = 0;
                   var FileNodes = SenseNet.Search.ContentQuery.Query(String.Format("+TypeIs:File +InFolder:'{0}' {1} .SORT:DisplayName", reqContentPath, GetFilter())).Nodes;
                   if (FileNodes.LongCount() > 0)
                   {
                       foreach (var file in FileNodes.Select(node => SenseNet.ContentRepository.Content.Create(node)))
                       {
                           var fileId = host + (uri.AbsolutePath.Contains("/Root") ? file.Path : SenseNet.Portal.Virtualization.PortalContext.GetSiteRelativePath(file.Path));
                %>
                <tr fileattribute="file" id="<%=host%><%=fileId%>" class='<%=idy++%2==0?"odd":"even"%>' onmousedown="selectrow()" onclick="selectrow()">
                    <td style="width: 16px;" align="center">
                        <img src="/Root/Global/images/icons/16/<%= file.Fields["Icon"].GetData() %>.png" />
                    </td>
                    <td><%= file.DisplayName%></td>
                    <td><%= (file.Fields["ModifiedBy"].GetData() as SenseNet.ContentRepository.User).FullName%></td>
                    <td><%= (file.Fields["ModificationDate"].GetData()).ToString()%></td>
                </tr>
            </tbody>
            <% }
                   }
                   else
                   { %>
            <tr>
                <td colspan="4">There no files of the specifed type here</td>
            </tr>
            <% } %>
        </table>
    </div>
</body>
</html>
