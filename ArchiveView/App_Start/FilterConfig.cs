using System.Web.Mvc;
using System.Web.Routing;

namespace ArchiveView
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilter(GlobalFilterCollection filters) {

            filters.Add(new HandleErrorAttribute { View = "_Error"});
        }
    }
}
