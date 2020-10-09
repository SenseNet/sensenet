using System;

namespace SenseNet.ApplicationModel
{
    public class ClientAction : PortalAction
    {
        public virtual string MethodName { get; set; }

        public virtual string ParameterList { get; set; }

        /// <summary>
        /// Custom client actions may use this value (provided by the builder who declared the action)
        /// to redirect to the back url after completing the action. This may be useful when the portlet
        /// binding is different than the default binding, and the default redirect mechanism would 
        /// result in a wrong url.
        /// </summary>
        public bool RedirectToBackUrl { get; protected set; }

        public override string Uri
        {
            get
            {
                return this.Forbidden ? string.Empty : Callback;
            }
        }

        private string _callback;
        public virtual string Callback
        {
            get
            {
                
                if (this.Forbidden)
                    return string.Empty;

                return _callback ?? string.Format("javascript:{0}({1});return false;", MethodName, ParameterList);
            }
            set
            {
                _callback = value;
            }
        }

        public override void Initialize(ContentRepository.Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            object rtb;
            if (this.GetParameters().TryGetValue("RedirectToBackUrl", out rtb))
                this.RedirectToBackUrl = Convert.ToBoolean(rtb);
        }
    }
}
