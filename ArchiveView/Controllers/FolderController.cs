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

        [HandleError(ExceptionType =typeof(FormatException), View ="_Error")]//maybe i dont need since all go to same view
        [HandleError(ExceptionType = typeof(NoResultException), View = "_Error")]//maybe i dont need since all go to same view
        //currently the role is coming as a query value, needs to be a role check through better security
        // GET: Folder
        public ActionResult Index([Bind(Prefix = "ClientId")] string Number, string Role = "")
        {
            tbl_Folder folder = null;

            folder = repository.SelectByNumber(Number);

            if (folder == null)
            {
                NoResultException exception = new NoResultException("The client may not exist or does not have any available documents.");
                exception.HelpLink = "Please check over the provided information and try again.";
                exception.Data["Client ID"] = Number;

                throw exception;
            }
            else {
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

                TempData["Client_Name"] = folder.Name;
                TempData["Client_Id"] = folder.Number;
                TempData["Folder_Id"] = folder.Folder_ID; //should be a better way than carrying this variable around
            }

            return RedirectToAction("Index", "PublicVM", new { folderId = folder.Folder_ID });
        }

        //Dispose any open connection when finished (db in this regard)
        protected override void Dispose(bool disposing)
        {
            repository.Dispose();
            base.Dispose(disposing);
        }
    }
}