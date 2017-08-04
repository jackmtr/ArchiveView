using System.Web;
using System.Web.Optimization;

namespace ArchiveView
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            //in production mode, these script files will combine and minify into one script for performance boost
            bundles.Add(new ScriptBundle("~/bundles/otf").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/jquery-ui.js", 
                        "~/Scripts/jquery.unobtrusive*",
                        "~/Scripts/jquery.validate*",
                        "~/Scripts/bootstrap.js",
                        "~/Scripts/otf.js")); //my custom js script

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            //modernizr must be implemented in the head (unlike the other scripts at end of body) as it is used to allow older browsers understand html5
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*")); 
            
            // style sheet bundler
            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/bootstrap-grid.css", //extra table options
                      "~/Content/bootstrap-theme.css", //used mainly for table appearance, could be removed for a more basic look
                      "~/Content/font-awesome.css", //for icons
                      "~/Content/jquery-ui*",
                      "~/Content/Site.css"
                      ));
        }
    }
}