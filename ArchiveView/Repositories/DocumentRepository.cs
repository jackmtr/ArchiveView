using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ArchiveView.Models;
using ArchiveView.ViewModels;
using System.Data.Entity.SqlServer;

namespace ArchiveView.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private WASEntities _db = null;

        public DocumentRepository()
        {
            this._db = new WASEntities();
        }

        /// <summary>
        /// Get a specific Document
        /// </summary>
        /// <param name="id">Document Id</param>
        /// <param name="authorized">bool to check if user should be able to retrieve HIDDEN documents</param>
        /// <returns></returns>
        public tbl_Document SelectById(string id, bool authorized)
        {
            int docId;
            tbl_Document document = null;

            try
            {
                docId = Int32.Parse(id);

                if (authorized == true)
                {
                    document = _db.tbl_Document.AsNoTracking().SingleOrDefault(p => p.Document_ID == docId);
                }
                else
                {
                    document = _db.tbl_Document.AsNoTracking().SingleOrDefault(p => p.Document_ID == docId && p.Active_IND == true);
                }

                //if document exists and ArchiveFile is null, it will look into the purged WAS db instead.
                if (document.ArchivedFile == null)
                {
                    this._db = new WASEntities("name=WASArchiveEntities");

                    if (authorized == true)
                    {
                        document = _db.tbl_Document.AsNoTracking().SingleOrDefault(p => p.Document_ID == docId);
                    }
                    else
                    {
                        document = _db.tbl_Document.AsNoTracking().SingleOrDefault(p => p.Document_ID == docId && p.Active_IND == true);
                    }
                    //Because this is a rare occurance, I would rather blindly search through other db's than change my model to bring in the repo value
                    //if more than one repo is used, we will have to create a repo attribute on the document model and bring tbl_Document.Repository_ID over to check and find
                }

                return document;
            }
            catch (FormatException e)
            {
                FormatException exception = new FormatException("Document Id must be a positive integer", e);
                exception.HelpLink = "Please check over the provided information and try again.";
                exception.Data["Document ID"] = id;

                throw exception;
            }
            catch (InvalidOperationException e) {
                InvalidOperationException exception = new InvalidOperationException("There was an issue connecting to the database", e);
                exception.HelpLink = "Please contact Support through ServiceNow.";

                throw exception;
            }
            catch (Exception e)
            {
                //maybe write more here
                throw new Exception();
            }
        }

        /// <summary>
        /// Get all document of one client
        /// Currently only used by DownloadAllDocuments()
        /// May not be needed after redoing DownloadAllDocs to have filters
        /// </summary>
        /// <param name="id">Folder ID</param>
        /// <returns></returns>
        public IEnumerable<tbl_Document> SelectAll(string id) {
            try
            {
                int intId = Int32.Parse(id);

                return _db.tbl_Document.AsNoTracking().Where(d => d.Folder_ID == intId);
            }
            catch (FormatException e)
            {
                FormatException exception = new FormatException("Folder Id must be a positive integer", e);
                exception.HelpLink = "Please check over the provided information and try again.";
                exception.Data["Folder ID"] = id;

                throw exception;
            }
            catch (InvalidOperationException e)
            {
                InvalidOperationException exception = new InvalidOperationException("There was an issue connecting to the database", e);
                exception.HelpLink = "Please contact Support through ServiceNow.";

                throw exception;
            }
            catch (Exception e)
            {
                //maybe write more here
                throw new Exception();
            }
        }

        /// <summary>
        /// Gathers additional secondary fields for the document in question
        /// </summary>
        /// <param name="id">Document Id</param>
        /// <returns>MiscPublicData with additional info of the document</returns>
        public MiscPublicData GetMiscPublicData(string id)
        {
            try {
                //should be able to create a function to do this repetitive code from both functions

                int documentNumberInt = Int32.Parse(id);

                //not every document will have a corrosponding docReference
                var documentData = (from d in _db.tbl_Document.AsNoTracking() //.AsNoTracking reduces resources by making this read only                       
                                    join dr in _db.tbl_DocReference on d.Document_ID equals dr.Document_ID into ps
                                    where d.Document_ID == documentNumberInt
                                    from dr in ps.DefaultIfEmpty()
                                    select new
                                    {
                                        d.Document_ID,
                                        d.Division_CD,
                                        d.CreatorFirstName,
                                        d.CreatorLastName,
                                        d.LastUser_DT,
                                        d.Reason,
                                        d.Recipient,
                                        Value = d.ArchivedFile != null ? SqlFunctions.DataLength(d.ArchivedFile).Value : 0, //Value is the byte size of file, should rename
                                        //a 0 byte size will mean filesize is unobtainable, logic added in view to show this
                                        //Additional information: This function can only be invoked from LINQ to Entities.
                                        d.tbl_DocReference
                                    }).First();
                //instead of doing .First(), should be a better way of bringing over just 1 record since they SHOULD(?) all be the same, probably a better LINQ statement
                //torn between making a subclass for docReference to pull those 3 properties from, or just use the full dataset.  

                MiscPublicData mpd = new MiscPublicData();

                mpd.Document_ID = documentData.Document_ID;
                mpd.Branch = documentData.Division_CD;
                mpd.Creator = documentData.CreatorFirstName + " " + documentData.CreatorLastName;
                mpd.ArchiveTime = documentData.LastUser_DT;
                mpd.Reason = documentData.Reason;
                mpd.Recipient = documentData.Recipient;
                mpd.FileSize = (documentData.Value) / 1000;
                mpd.DocReferences = documentData.tbl_DocReference;

                return mpd;
            }
            catch (FormatException e)
            {
                FormatException exception = new FormatException("Document Id must be a positive integer", e);
                exception.HelpLink = "Please check over the provided information and try again.";
                exception.Data["Document ID"] = id;

                throw exception;
            }
            catch (InvalidOperationException e)
            {

                InvalidOperationException exception = new InvalidOperationException("There was an issue connecting to the database", e);
                exception.HelpLink = "Please contact Support through ServiceNow.";

                throw exception;
            }
            catch (Exception e)
            {
                //maybe write more here
                throw new Exception();
            }
        }

        /// <summary>
        /// Updates the entity model with the changes, but does not save.
        /// </summary>
        /// <param name="publicVM">The publicVm instance</param>
        /// <param name="db">the database</param>
        /// <returns>(make use of this)</returns>
        public bool UpdateChanges(PublicVM publicVM, WASEntities db) {

            tbl_Document modDoc = SelectById(publicVM.Document_ID.ToString(), true);

            //maybe more elegant way to do this
            modDoc.Issue_DT = publicVM.IssueDate;
            modDoc.Description = publicVM.Description;
            modDoc.Method = publicVM.Method;
            modDoc.Originator = publicVM.Originator;
            modDoc.Reason = publicVM.Reason;
            modDoc.Recipient = publicVM.Recipient;
            modDoc.Active_IND = publicVM.Hidden;

            db.Entry(modDoc).State = System.Data.Entity.EntityState.Modified; //tags the row as one thats modifed and needs to be saved

            //should probably do some conditional check here to allow user to know if it actually got modified
            return true;
        }

        public void Dispose()
        {
            _db.Dispose();
        }

    }
}