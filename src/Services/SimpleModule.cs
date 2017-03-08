using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Diagnostics;

namespace SenseNet.Services
{
    internal class SimpleModule : IHttpModule
    {

        #region IHttpModule Members

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
            context.EndRequest += new EventHandler(context_EndRequest);
        }

        #endregion

        #region Methods

        private void context_BeginRequest(object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;
            Debug.WriteLine("-------- BEGIN --------");
            try
            {
                Debug.WriteLine("METHOD: " + context.Request.HttpMethod);
                Debug.WriteLine("PATH: '" + context.Request.Path + "'");
                Debug.WriteLine("AUTH HEADER: " + context.Request.Headers["Authorization"]);
                Debug.Write("HEADERS: ");
                foreach (var x in context.Request.Headers.AllKeys)
                {
                    Debug.Write(string.Format("{0}={1}", x, context.Request.Headers[x]));
                }
                Debug.WriteLine("");
            }
            catch (Exception ex) //TODO: catch block
            {
                Debug.WriteLine("SIMPLEMODULE EXCEPTION: " + ex.Message);
            }
            Debug.WriteLine("-------- / BEGIN --------");
        }

        private void context_EndRequest(object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;
            Debug.WriteLine("--- END ---");
            try
            {
                Debug.WriteLine("Authenticated: " + HttpContext.Current.User.Identity.IsAuthenticated.ToString());
                Debug.WriteLine("UserName: " + HttpContext.Current.User.Identity.Name);
            }
            catch (Exception ex) //TODO: catch block
            {
                Debug.WriteLine("SIMPLEMODULE EXCEPTION: " + ex.Message);
            }
            Debug.WriteLine("--- / END ---");
        }

        #endregion
    }
}
