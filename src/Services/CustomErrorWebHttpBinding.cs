using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Activation;
using System.Diagnostics;
using SenseNet.Services.ContentStore;

namespace SenseNet.Services
{
    public class CustomErrorWebHttpBehavior : WebHttpBehavior
    {
        protected override void AddServerErrorHandlers(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            base.AddServerErrorHandlers(endpoint, endpointDispatcher);
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new CustomErrorHandler());
        }

    }

    public class CustomErrorHandler : IErrorHandler
    {
        #region IErrorHandler Members

        public bool HandleError(Exception error)
        {
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            if (error is NodeLoadException)
            {
                fault = Message.CreateMessage(version, "",error.Message, new DataContractJsonSerializer(typeof(string)));
                var wbf = new WebBodyFormatMessageProperty(WebContentFormat.Json);
                fault.Properties.Add(WebBodyFormatMessageProperty.Name, wbf);
                var rmp = new HttpResponseMessageProperty();
                rmp.StatusCode = System.Net.HttpStatusCode.BadRequest;
                rmp.StatusDescription = "See fault object for more information.";
                fault.Properties.Add(HttpResponseMessageProperty.Name, rmp);
            }
            else
            {
                fault = Message.CreateMessage(version, "", error.Message, 
                    new DataContractJsonSerializer(typeof(string)));
                var wbf = new WebBodyFormatMessageProperty(WebContentFormat.Json);
                fault.Properties.Add(WebBodyFormatMessageProperty.Name, wbf);
                var rmp = new HttpResponseMessageProperty();
                rmp.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                rmp.StatusDescription = "Uknown exception...";
                fault.Properties.Add(HttpResponseMessageProperty.Name, rmp);
            }
        }

        #endregion
    }

    public class CustomErrorServiceHostFactory : WebServiceHostFactory
    {
        public CustomErrorServiceHostFactory()
        {
        }

        public override ServiceHostBase CreateServiceHost(string constructorString, Uri[] baseAddresses)
        {
            var sh = new ServiceHost(typeof(ContentStoreService), baseAddresses);
            sh.Description.Endpoints[0].Behaviors.Add(new CustomErrorWebHttpBehavior());
            return sh;
        }

        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
           return base.CreateServiceHost(serviceType, baseAddresses);
        }

    }

    [DataContract]
    public class GreaterThan3Fault
    {
        [DataMember]
        public string FaultMessage { get; set; }
        [DataMember]
        public int ErrorCode { get; set; }
        [DataMember]
        public string Location { get; set; }

    }

}