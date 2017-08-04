using ArchiveView.Repositories;
using ArchiveView.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ArchiveView.Models;
using System.IO;
using System.IO.Compression;
using ArchiveView.Filters;

namespace ArchiveView.Controllers
{
    [AuthorizeUser]
    public class AdminController : Controller
    {
        private IFolderRepository repository = null;
        private IDocumentRepository documentRepository = null;
        private IPublicRepository publicRepository = null;
        private WASEntities _db = new WASEntities();

        public AdminController()
        {
            this.repository = new FolderRepository();
            this.documentRepository = new DocumentRepository();
            this.publicRepository = new PublicRepository();
        }

        [HttpGet]
        public ActionResult Edit([Bind(Prefix = "folderId")] string Folder_ID, string[] EditList) {

            //THERE IS A BUG IF THE EditList CARRIES TOO MANY OBJECTS

            List<PublicVM> publicModel = null;

            publicModel = publicRepository.SelectAll(Folder_ID, "Admin").Where(doc => EditList.Contains(doc.Document_ID.ToString())).GroupBy(x => x.Document_ID).Select( x => x.First()).ToList();

            return PartialView("_EditTable", publicModel);
        }

        [HttpPost]
        public ActionResult Edit([Bind(Prefix = "publicId")] string Folder_ID, List<PublicVM> updatedEditList) {

            if (ModelState.IsValid) {

                foreach (PublicVM pvm in updatedEditList) {

                    tbl_Document modDoc = documentRepository.SelectById(pvm.Document_ID.ToString(), true);
                    modDoc.Issue_DT = pvm.IssueDate;
                    modDoc.Description = pvm.Description;
                    modDoc.Method = pvm.Method;
                    modDoc.Originator = pvm.Originator;
                    modDoc.Reason = pvm.Reason;
                    modDoc.Recipient = pvm.Recipient;
                    modDoc.Active_IND = pvm.Hidden;

                    _db.Entry(modDoc).State = System.Data.Entity.EntityState.Modified;
                }
                _db.SaveChanges();
            }

            return RedirectToAction("Index", "Folder", new { ClientId = TempData["Client_Id"], Role = "Admin" });
            }

        [HttpGet]
        //Downloads all documents pertaining to a specific client ID
        public ActionResult DownloadAllDocuments([Bind(Prefix = "ClientId")] string Number)
        {

            tbl_Folder folder = repository.SelectByNumber(Number);

            //gets all documents for one folder_id
            IEnumerable<tbl_Document> files = documentRepository.SelectAll(folder.Folder_ID.ToString());

            byte[] result; //blank byte array, ready to be used to filled with the tbl_document.ArchivedFile bytes

            //opening a stream to allow data to be moved
            using (var zipArchiveMemoryStream = new MemoryStream())
            {

                //creating a zipArchive obj to be used and disposed of
                using (var zipArchive = new ZipArchive(zipArchiveMemoryStream, ZipArchiveMode.Create, true))
                {

                    foreach (var file in files)
                    {
                        if (file.ArchivedFile != null)
                        {
                            //according to Ramin, creation of an ArchivedFile and Submitting an ArchivedFile are different steps, so there could be 'dirty' records/documents in WAS db that has no ArchivedFile Fields records
                            var zipEntry = zipArchive.CreateEntry(file.Document_ID.ToString() + "." + file.FileExtension); //creates a unit of space for the individual file to be placed in

                            using (var entryStream = zipEntry.Open())
                            {
                                using (var tmpMemory = new MemoryStream(file.ArchivedFile))
                                {
                                    tmpMemory.CopyTo(entryStream); //copies the data into the unit space
                                }
                            }
                        }
                    }
                }

                zipArchiveMemoryStream.Seek(0, SeekOrigin.Begin); //not exactly sure what this does, I think it pertains to the ziped folders ordering
                result = zipArchiveMemoryStream.ToArray(); //I think this is the ziped item
            }

            return new FileContentResult(result, "application/zip") { FileDownloadName = "AllArchivedFilesFor_" + Number + ".zip" };
        }

        protected override void Dispose(bool disposing)
        {
            repository.Dispose();
            publicRepository.Dispose();
            documentRepository.Dispose();
            base.Dispose(disposing);
        }
    }
}