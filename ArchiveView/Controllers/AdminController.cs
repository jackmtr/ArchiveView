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

        /// <summary>
        /// Recieves an array of documentIds to allow to be edited at the same time on one form
        /// </summary>
        /// <param name="Folder_ID">The Folder_Id to target</param>
        /// <param name="EditList">An array of document_Ids</param>
        /// <returns>Returns a partial form of editable publicVMs</returns>
        [HttpGet]
        public ActionResult Edit([Bind(Prefix = "folderId")] string Folder_ID, string[] EditList)
        {
            TempData.Keep("Folder_Id");

            //***THERE IS A BUG IF THE EditList CARRIES TOO MANY OBJECTS***
            //Theory: string array size is causing some issue

            List<PublicVM> publicModel = null; // maybe move outside of action?

            publicModel = publicRepository.SelectAll(Folder_ID, "Admin")
                                            .Where(doc => EditList.Contains(doc.Document_ID.ToString()))
                                                .GroupBy(x => x.Document_ID)
                                                    .Select(x => x.First())
                                                        .ToList();

            return PartialView("_EditTable", publicModel);
        }

        /// <summary>
        /// Posts the changes made to db for the selected documents and saves it
        /// </summary>
        /// <param name="Folder_ID">Currently not used, maybe get rid or use it to get ClientId in future</param>
        /// <param name="updatedEditList">The collection of publicVM instances from he form (either unmoded or modded)</param>
        /// <returns>a redirection to the main publicVM controller</returns>
        [HttpPost]
        public ActionResult Edit([Bind(Prefix = "publicId")] string Folder_ID, List<PublicVM> updatedEditList)
        {
            if (ModelState.IsValid)
            {
                foreach (PublicVM pvm in updatedEditList)
                {
                    documentRepository.UpdateChanges(pvm, _db);
                }
                _db.SaveChanges();
            }
            return RedirectToAction("Index", "Folder", new { ClientId = TempData["Client_Id"], Role = "Admin" });
        }

        
        //***FUTURE IMPROVEMENTS: Be able to specify filters of which documents to dl
        //Possibly link this to an array/collection of documentIds, to dl, so can make use of already created filters
        /// <summary>
        /// Downloads all documents pertaining to a specific client ID
        /// </summary>
        /// <param name="Number">The Client Id to download documents for</param>
        /// <returns>a ziped file to download</returns>
        [HttpGet]
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
                    foreach (tbl_Document file in files)
                    {
                        DownloadFile(zipArchive, file);
                    }
                }

                zipArchiveMemoryStream.Seek(0, SeekOrigin.Begin); //not exactly sure what this does, I think it pertains to the ziped folders ordering
                result = zipArchiveMemoryStream.ToArray(); //I think this is the ziped item
            }

            return new FileContentResult(result, "application/zip") { FileDownloadName = "AllArchivedFilesFor_" + Number + ".zip" };
        }

        /// <summary>
        /// Downloads one file and puts it into a zip file
        /// </summary>
        /// <param name="zipArchive">The zipfile to put the file into</param>
        /// <param name="file">the file to download</param>
        private void DownloadFile(ZipArchive zipArchive, tbl_Document file)
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            repository.Dispose();
            publicRepository.Dispose();
            documentRepository.Dispose();
            base.Dispose(disposing);
        }
    }
}