using System;
using System.Web.Services;
using SenseNet.ContentRepository.Mail;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage;
using System.Text;

namespace SenseNet.Portal.Exchange
{
    [WebService(Namespace = "http://tempuri.org/")]
    public class PushNotificationClient : WebService, INotificationServiceBinding
    {
        public SendNotificationResultType SendNotification(SendNotificationResponseType SendNotification1)
        {
            var result = new SendNotificationResultType();
            var rmta = SendNotification1.ResponseMessages.Items;

            foreach (ResponseMessageType rmt in rmta)
            {
                if (rmt.ResponseCode != ResponseCodeType.NoError)
                {
                    SnLog.WriteError("An error occurred during receiving exchange notifications: " + rmt.MessageText,
                        categories: ExchangeHelper.ExchangeLogCategory);
                    result.SubscriptionStatus = SubscriptionStatusType.OK;
                    return result;
                }

                var snrmt = rmt as SendNotificationResponseMessageType;
                var notification = snrmt.Notification;

                // check if subscription is valid: 
                //  1: if it is not the last subscription, unsubscribe, so we don't receive mails twice or more
                //  2: if target email has been deleted, unsubscribe
                var contextNode = PortalContext.Current.ContextNode;
                var targetEmail = contextNode["ListEmail"] as string;
                var expectedSubscriptionId = contextNode["ExchangeSubscriptionId"] as string;
                var targetEmailEmpty = string.IsNullOrEmpty(targetEmail);
                var subscriptiondifferent = expectedSubscriptionId != notification.SubscriptionId;
                if (subscriptiondifferent || targetEmailEmpty)
                {
                    var loginfo = string.Concat("Exchange unsubscribe: subscriptionId of event and last subscription are different:", (subscriptiondifferent).ToString(), ", targetemail is empty: ", targetEmailEmpty.ToString(), ", path: ", contextNode.Path);
                    SnLog.WriteInformation(loginfo, categories: ExchangeHelper.ExchangeLogCategory);

                    result.SubscriptionStatus = SubscriptionStatusType.Unsubscribe;
                    return result;
                }

                var items = notification.Items;

                // extract mail ids
                var itemIdsBuilder = new StringBuilder();
                var lastWatermark = string.Empty;
                for (int i = 0; i < items.Length; i++)
                {
                    var bocet = items[i] as BaseObjectChangedEventType;
                    if (bocet != null)
                    {
                        var loginfo = string.Concat(" - Path:", contextNode.Path, ", Email:", contextNode["ListEmail"], ", Watermark:", bocet.Watermark, ", SubscriptionId:", notification.SubscriptionId);
                        SnLog.WriteInformation(string.Concat("Exchange ", notification.ItemsElementName[i].ToString(), loginfo), categories: ExchangeHelper.ExchangeLogCategory);

                        var itemId = bocet.Item as ItemIdType;
                        if (itemId != null)
                        {
                            itemIdsBuilder.Append(itemId.Id);
                            itemIdsBuilder.Append(";");
                        }
                        lastWatermark = bocet.Watermark;
                    }
                    else
                    {
                        var loginfo = string.Concat(" - Path:", contextNode.Path, ", Email:", contextNode["ListEmail"]);
                        SnLog.WriteInformation(string.Concat("Exchange ", notification.ItemsElementName[i].ToString(), loginfo), categories: ExchangeHelper.ExchangeLogCategory);
                    }
                }

                // persist mail ids under contentlist
                var itemIds = itemIdsBuilder.ToString();
                if (!string.IsNullOrEmpty(itemIds))
                {
                    var parent = SetWatermark(contextNode, lastWatermark);
                    if (parent != null)
                    {
                        using (new SystemAccount())
                        {
                            try
                            {
                                var task = new Task(parent)
                                {
                                    Name = "IncomingEmail",
                                    AllowIncrementalNaming = true,
                                    Description = itemIds
                                };
                                task.Save();
                            }
                            catch (Exception ex)
                            {
                                SnLog.WriteException(ex);
                            }
                        }
                    }
                }
            }
            result.SubscriptionStatus = SubscriptionStatusType.OK;
            return result;
        }

        private static Node SetWatermark(Node contextNode, string watermark)
        {
            var parent = Node.LoadNode(RepositoryPath.Combine(contextNode.Path, ExchangeHelper.PUSHNOTIFICATIONMAILCONTAINER));
            if (parent == null)
            {
                using (new SystemAccount())
                {
                    parent = new SystemFolder(contextNode);
                    parent.Name = ExchangeHelper.PUSHNOTIFICATIONMAILCONTAINER;
                    try
                    {
                        parent["Description"] = watermark;
                        parent.Save();
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex, categories: ExchangeHelper.ExchangeLogCategory);
                        return null;
                    }
                }
            }
            else
            {
                // persist watermark on existing content
                using (new SystemAccount())
                {
                    var retryCount = 3;
                    while (retryCount > 0)
                    {
                        try
                        {
                            var gc = parent as GenericContent;
                            gc["Description"] = watermark;
                            gc.Save(SavingMode.KeepVersion);
                            break;
                        }
                        catch (NodeIsOutOfDateException)
                        {
                            retryCount--;
                            parent = Node.LoadNode(RepositoryPath.Combine(contextNode.Path, ExchangeHelper.PUSHNOTIFICATIONMAILCONTAINER));
                        }
                    }
                }
            }

            return parent;
        }
    }
}