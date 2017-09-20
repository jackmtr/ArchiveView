using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ArchiveView.CustomHelpers
{
    public static class CustomLink
    {
        //Custom html helper that takes in typical 'Link' attributes and allows for font awesome icons to be used in the link span
        public static IHtmlString AjaxActionLinkWithFontAwesome(this UrlHelper url,Int32 folder_id, string thissort, string thisNavBarGroup, string thisNavBarItem, string thisSearchTerm, string thisIssueYearMinRange, string thisIssueYearMaxRange, string targetId, string mode, string method, string output, string fontShortcut, bool ascending, string linkClass, string ajaxComplete, string id)
        {
            string scheme = url.RequestContext.HttpContext.Request.Url.Scheme;

            string src = url.Action("Index", "PublicVM", new { folderId = folder_id, sort = thissort, navBarGroup = thisNavBarGroup, navBarItem = thisNavBarItem, searchTerm = thisSearchTerm, IssueYearMinRange = thisIssueYearMinRange, thisIssueYearMaxRange = thisIssueYearMaxRange }, scheme);

            string formattedString = String.Format("<a href = '{0}' class = '{6}' data-ajax-update = '{1}' data-ajax-mode = '{2}' data-ajax-method = '{3}' data-ajax = 'true' data-ajax-success = '{7}' data-ajax-complete = 'rememeberSort($(this), {9})'> {4} <i  id='{8}' class='fa {5}'></i></a>", src, targetId, mode, method, output, fontShortcut, linkClass, ajaxComplete, id, ascending.ToString().ToLower());

            IHtmlString nonEncodedString = new HtmlString(formattedString);

            return nonEncodedString;
        }
    }
}