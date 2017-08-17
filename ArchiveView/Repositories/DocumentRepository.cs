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

        public tbl_Document SelectById(string id, bool authorized)
        {
            int docId = 0;
            tbl_Document document = null;

            try {
                docId = Int32.Parse(id);

                if (authorized == true)
                {
                    document = _db.tbl_Document.AsNoTracking().SingleOrDefault(p => p.Document_ID == docId);
                }
                else {
                    document = _db.tbl_Document.AsNoTracking().SingleOrDefault(p => p.Document_ID == docId && p.Active_IND == true);
                }

                //if document exists and ArchiveFile is null, it will look into the purged WAS db instead.
                if (document.ArchivedFile == null) {
                    this._db = new WASEntities("name=WASArchiveEntities");

                    if (authorized == true)
                    {
                        document = _db.tbl_Document.AsNoTracking().SingleOrDefault(p => p.Document_ID == docId && p.Active_IND == true);
                    }
                    else
                    {
                        document = _db.tbl_Document.AsNoTracking().SingleOrDefault(p => p.Document_ID == docId && p.Active_IND == false);
                    }
                    //Because this is a rare occurance, I would rather blindly search through other db's than change my model to bring in the repo value
                    //if more than one repo is used, we will have to create a repo attribute on the document model and bring tbl_Document.Repository_ID over to check and find
                }

            } catch {
                return null;
            }
            
            return document;
        }

        public IEnumerable<tbl_Document> SelectAll(string id) {

            int intId = Int32.Parse(id);

            return _db.tbl_Document.AsNoTracking().Where(d => d.Folder_ID == intId);
        }

        public MiscPublicData GetMiscPublicData(string id)
        {
            //should be able to create a function to do this repetitive code from both functions

            int documentNumberInt = Int32.Parse(id);

            var documentData = (from d in _db.tbl_Document.AsNoTracking() //.AsNoTracking reduces resources by making this read only      
                                                                          //not every document will have a corrosponding docReference
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
                                    //d.ArchivedFile,//maybe bring it over as an int
                                    SqlFunctions.DataLength(d.ArchivedFile).Value,
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
            //mpd.File = documentData.ArchivedFile;
            //mpd.FileSize = SqlFunctions.DataLength(documentData.ArchivedFile); doesnt work cause linq to entity doesnt support datalength
            //SqlFunctions.DataLength//documentData.ArchivedFile
            //mpd.FileSize2 = documentData.ArchivedFile.Length; //(documentData.ArchivedFile.Length);
            //mpd.FileSize = SqlFunctions.DataLength(mpd.File);
            mpd.FileSize = (documentData.Value)/1000;
            mpd.DocReferences = documentData.tbl_DocReference;

            return mpd;
        }

        public bool SaveChanges(tbl_Document doc) {
            //dont think im using this method, maybe others below too
            return true;
        }

        public void Update(tbl_Document doc)
        {
            _db.Entry(doc).State = System.Data.Entity.EntityState.Modified;
        }

        public void Save() {
            _db.SaveChanges();
        }

        public void Dispose()
        {
            _db.Dispose();
        }

    }
}