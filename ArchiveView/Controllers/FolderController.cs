using ArchiveView.Exceptions;
using ArchiveView.Models;
using ArchiveView.Repositories;
using System;
using System.Web.Mvc;

namespace ArchiveView.Controllers
{
    //This controller is mainly used to take in the initial request info and role, and sending the user the appropriate view through the main controller (PublicVMController)
    //***this controller may not honestly be needed in the end, and the PublicVM can do the functionality here too.
    public class FolderController : Controller
    {
        private IFolderRepository repository = null;

        public FolderController() {
            this.repository = new FolderRepository();
        }

        //keep for potential of future testing of db connection
        /*
        public FolderController(IFolderRepository repository) {
            this.repository = repository;
        }
        */

        //currently the role is coming as a query value, needs to be a role check through better security
        /// <summary>
        /// Takes the ClientId and packages some data up to give to publicVMController
        /// Might just comebine this and PublicVM together, somewhat redudant to have two controllers
        /// </summary>
        /// <param name="Number">The Client Id</param>
        /// <param name="Role">Whether the person has authoratative powers in Archive View</param>
        /// <returns></returns>
        //Since all exceptions go to the same view, dont see any reason to use different handerror attributes
        // GET: Folder
        public ActionResult Index([Bind(Prefix = "ClientId")] string Number, string Role = "", string PublicName = "")
        {
            tbl_Folder folder = null;
            CustomHelpers.InMemoryCache cacheprovider = new CustomHelpers.InMemoryCache();

            folder = repository.SelectByNumber(Number);

            if (folder == null)
            {
                NoResultException exception = new NoResultException("The client may not exist or does not have any available documents.");
                exception.HelpLink = "Please check over the provided information and try again.";
                exception.Data["Client ID"] = Number;

                throw exception;
            }
            else {
                //was told to eventually create a seperate role for ArchiveView
                if (HttpContext.User.IsInRole("westlandcorp\\IT-ops"))
                {
                    TempData["RoleButton"] = "Admin";

                    if (Role == "Admin")
                    {
                        TempData["Role"] = "Admin";
                        TempData["RoleButton"] = "Client";
                    }
                    else
                    {
                        TempData["Role"] = "Client";
                    }
                }
                else
                {
                    TempData["Role"] = "Client";
                }

                //TempData["Client_Name"] = folder.Name;
                TempData["Client_Name"] = Server.UrlDecode(PublicName);

                TempData["Client_Id"] = folder.Number;
                TempData["Folder_Id"] = folder.Folder_ID; //should be a better way than carrying this variable around
            }

            cacheprovider.removeCache(folder.Folder_ID.ToString());

            return RedirectToAction("Index", "PublicVM", new { folderId = folder.Folder_ID });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            repository.Dispose();
            base.Dispose(disposing);
        }
    }
}