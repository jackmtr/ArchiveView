using System.Web.Mvc;
using System.Web.Routing;

namespace ArchiveView
{
    public class FilterConfig
    {
        //Global filters are attribute put on all controllers
        public static void RegisterGlobalFilter(GlobalFilterCollection filters) {
            //_Error is a partial page
            filters.Add(new HandleErrorAttribute { View = "_Error"});
        }
    }
}
