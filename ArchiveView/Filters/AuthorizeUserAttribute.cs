using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace ArchiveView.Filters
{
    public class AuthorizeUserAttribute : AuthorizeAttribute
    {

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var isAuthorized = base.AuthorizeCore(httpContext); //gets the authorization info of the person making the request

            if (!isAuthorized)
            {
                return false; //checks if the user is loged into windows
            }

            //checks if the user making the request is the same as when starting application
            //This code doesnt work when published to remote server
            //if (WindowAuth.WindowLoginName != httpContext.User.Identity.Name)
            //{
            //    return false;
            //}

            if (httpContext.User.IsInRole("westlandcorp\\IT-ops"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            //can do checks for what info to give user when not authorized arrises
            filterContext.Result = new ViewResult
            {
                ViewName = "~/Views/Shared/Errors.cshtml"
            };
        }
    }
}