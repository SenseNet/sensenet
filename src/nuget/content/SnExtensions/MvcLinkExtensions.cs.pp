using System.Collections.Generic;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;

namespace $rootnamespace$
{
    public static class MvcLinkExtensions
    {
        private const string DefaultRouteName = "Default";

        public static string MvcRouteName { get; set; }

        /// <summary>
        /// Returns an anchor element (a element) for the specified link text and action.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <returns>An anchor element (a element).</returns>
        public static MvcHtmlString MvcActionLink(this HtmlHelper htmlHelper, string linkText, string actionName)
        {
            return htmlHelper.RouteLink(linkText, MvcRouteName ?? DefaultRouteName,
                new RouteValueDictionary
                {
                    {"action", actionName}
                });
        }

        /// <summary>
        /// Returns an anchor element (a element) for the specified link text, action, and route values.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="linkText"> The inner text of the anchor element.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="routeValues">An object that contains the parameters for a route.
        /// The parameters are retrieved through reflection by examining the properties of the object. 
        /// The object is typically created by using object initializer syntax.</param>
        /// <returns>An anchor element (a element).</returns>
        public static MvcHtmlString MvcActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, object routeValues)
        {
            var routeValuesDictionary = MergeRouteValues(actionName, null, routeValues);
            return htmlHelper.RouteLink(linkText, MvcRouteName ?? DefaultRouteName, routeValuesDictionary);
        }

        /// <summary>
        /// Returns an anchor element (a element) for the specified link text, action, and route values as a route value dictionary.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="routeValues">An object that contains the parameters for a route.</param>
        /// <returns>An anchor element (a element).</returns>
        public static MvcHtmlString MvcActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, RouteValueDictionary routeValues)
        {
            var routeValuesDictionary = MergeRouteValues(actionName, null, routeValues);
            return htmlHelper.RouteLink(linkText, MvcRouteName ?? DefaultRouteName, routeValuesDictionary);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName"></param>
        /// <returns>An anchor element (a element).</returns>
        public static MvcHtmlString MvcActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName)
        {
            return htmlHelper.RouteLink(linkText, MvcRouteName ?? DefaultRouteName,
                new RouteValueDictionary
                {
                    {"controller", controllerName},
                    {"action", actionName}
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="routeValues">An object that contains the parameters for a route. 
        /// The parameters are retrieved through reflection by examining the properties of the object.
        /// The object is typically created by using object initializer syntax.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes for the element.
        /// The attributes are retrieved through reflection by examining the properties of the object.
        /// The object is typically created by using object initializer syntax.</param>
        /// <returns>An anchor element (a element).</returns>
        public static MvcHtmlString MvcActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, object routeValues, object htmlAttributes)
        {
            var routeValuesDictionary = MergeRouteValues(actionName, null, routeValues);
            return htmlHelper.RouteLink(linkText, MvcRouteName ?? DefaultRouteName, routeValuesDictionary, GetHtmlAttributes(htmlAttributes));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="routeValues">An object that contains the parameters for a route.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.</param>
        /// <returns>An anchor element (a element).</returns>
        public static MvcHtmlString MvcActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            var routeValuesDictionary = MergeRouteValues(actionName, null, routeValues);
            return htmlHelper.RouteLink(linkText, MvcRouteName ?? DefaultRouteName, routeValuesDictionary, htmlAttributes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName"></param>
        /// <param name="routeValues">An object that contains the parameters for a route. 
        /// The parameters are retrieved through reflection by examining the properties of the object.
        /// The object is typically created by using object initializer syntax.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes for the element.
        /// The attributes are retrieved through reflection by examining the properties of the object.
        /// The object is typically created by using object initializer syntax.</param>
        /// <returns>An anchor element (a element).</returns>
        public static MvcHtmlString MvcActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, object routeValues, object htmlAttributes)
        {
            var routeValuesDictionary = MergeRouteValues(actionName, controllerName, routeValues);
            return htmlHelper.RouteLink(linkText, MvcRouteName ?? DefaultRouteName, routeValuesDictionary, GetHtmlAttributes(htmlAttributes));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName"></param>
        /// <param name="routeValues">An object that contains the parameters for a route.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.</param>
        /// <returns>An anchor element (a element).</returns>
        public static MvcHtmlString MvcActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            var routeValuesDictionary = MergeRouteValues(actionName, controllerName, routeValues);
            return htmlHelper.RouteLink(linkText, MvcRouteName ?? DefaultRouteName, routeValuesDictionary, htmlAttributes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element.</param>
        /// <param name="actionName"></param>
        /// <param name="controllerName"></param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="hostName">The host name for the URL.</param>
        /// <param name="fragment">The URL fragment name (the anchor name).</param>
        /// <param name="routeValues">An object that contains the parameters for a route. 
        /// The parameters are retrieved through reflection by examining the properties of the object.
        /// The object is typically created by using object initializer syntax.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes for the element.
        /// The attributes are retrieved through reflection by examining the properties of the object.
        /// The object is typically created by using object initializer syntax.</param>
        /// <returns>An anchor element (a element).</returns>
        public static MvcHtmlString MvcActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes)
        {
            var routeValuesDictionary = MergeRouteValues(actionName, controllerName, routeValues);
            return htmlHelper.RouteLink(linkText, MvcRouteName ?? DefaultRouteName, protocol, hostName, fragment, routeValuesDictionary, GetHtmlAttributes(htmlAttributes));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element.</param>
        /// <param name="actionName"></param>
        /// <param name="controllerName"></param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="hostName">The host name for the URL.</param>
        /// <param name="fragment">The URL fragment name (the anchor name).</param>
        /// <param name="routeValues">An object that contains the parameters for a route.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.</param>
        /// <returns>An anchor element (a element).</returns>
        public static MvcHtmlString MvcActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            var routeValuesDictionary = MergeRouteValues(actionName, controllerName, routeValues);
            return htmlHelper.RouteLink(linkText, MvcRouteName ?? DefaultRouteName, protocol, hostName, fragment, routeValuesDictionary, htmlAttributes);
        }

        private static RouteValueDictionary MergeRouteValues(string actionName, string controllerName, object routeValues)
        {
            return MergeRouteValues(actionName, controllerName, FillValues(routeValues, new RouteValueDictionary()));
        }
        private static RouteValueDictionary MergeRouteValues(string actionName, string controllerName, RouteValueDictionary values)
        {
            if (actionName != null)
                values["action"] = actionName;
            if (actionName != null)
                values["controller"] = controllerName;
            return values;
        }

        private static RouteValueDictionary GetHtmlAttributes(object htmlAttributes)
        {
            return FillValues(htmlAttributes, new RouteValueDictionary());
        }
        private static RouteValueDictionary FillValues(object anonymousObject, RouteValueDictionary values)
        {
            var properties = anonymousObject?.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
			if (properties != null)
            {
				foreach (var propertyInfo in properties)
					values[propertyInfo.Name] = propertyInfo.GetMethod.Invoke(anonymousObject, null);
			}
            return values;
        }
    }
}