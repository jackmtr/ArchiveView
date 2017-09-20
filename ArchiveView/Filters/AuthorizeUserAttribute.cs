using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace ArchiveView.Filters
{
    //Authorization Filter
    public class AuthorizeUserAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// attribute to check if user is an admin
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var isAuthorized = base.AuthorizeCore(httpContext); //gets the authorization info of the person making the request

            if (!isAuthorized)
            {
                return false; //checks if the user is loged into windows
            }

            if (httpContext.User.IsInRole("westlandcorp\\IT-ops"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// method to run if AuthorizeCore rejects the user
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            UnauthorizedAccessException exception = new UnauthorizedAccessException("Sorry, you do not have permission to access this page.");
            exception.HelpLink = "Please contact support if there are further issues.";

            HandleErrorInfo handleErrorInfo = new HandleErrorInfo(exception, filterContext.ActionDescriptor.ControllerDescriptor.ControllerName, filterContext.ActionDescriptor.ActionName);

            filterContext.Result = new PartialViewResult
            {
                ViewName = "~/Views/Shared/_Error.cshtml",
                ViewData = new ViewDataDictionary(filterContext.Controller.ViewData) {
                    Model = handleErrorInfo
                }
            };
        }
    }
}