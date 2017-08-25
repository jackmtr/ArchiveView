using ArchiveView.Models;
using ArchiveView.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web;

namespace ArchiveView.Repositories
{
    public class PublicRepository : IPublicRepository
    {
        //might need to decouple this VM repo from the dbcontext by creating more repos this VMrepo will grab from

        private WASEntities _db = null;

        public PublicRepository() {
            this._db = new WASEntities(); //dbcontext class
        }

        //add parameterized constructor

        public IEnumerable<PublicVM> SelectAll(string publicNumber, string role) {

            List<PublicVM> PublicVMList = new List<PublicVM>();

            int publicNumberInt;

            try {
                publicNumberInt = Int32.Parse(publicNumber); //should be able to be done in LINQ
            } catch {
                return null;
            }

            if (role == "Admin")
            {
                var documentList = (from d in _db.tbl_Document.AsNoTracking() //.AsNoTracking reduces resources by making this read only                        
                                    join f in _db.tbl_Folder.AsNoTracking() on d.Folder_ID equals f.Folder_ID
                                    //left outer join b/c not every document will have a reference number right away (aka no docreference)
                                    join dr in _db.tbl_DocReference.AsNoTracking() on d.Document_ID equals dr.Document_ID into ps
                                    join dt in _db.tbl_DocumentType.AsNoTracking() on d.DocumentType_ID equals dt.DocumentType_ID
                                    join cat in _db.tbl_Category.AsNoTracking() on dt.Category_ID equals cat.Category_ID
                                    where d.Folder_ID == publicNumberInt
                                    //where d.Active_IND == true //should only show active records, hide soft deleted ones
                                    where d.DocumentNumber != null
                                    where cat.Category_ID != 6
                                    from dr in ps.DefaultIfEmpty()
                                    select new
                                    {
                                        f.Folder_ID,
                                        d.Document_ID,
                                        dt.DocumentType_ID,
                                        DtName = dt.Name,
                                        d.Issue_DT,
                                        d.Description,
                                        cat.Category_ID,
                                        CatName = cat.Name,
                                        dr.Date1_DT,
                                        dr.RefNumber,
                                        dr.RefNumberType_CD,
                                        d.FileExtension,
                                        d.Method,
                                        d.Originator,
                                        d.Reason,
                                        dr.Number1,
                                        d.Recipient,
                                        d.Active_IND //only want recipient and active_ind for admin, wonder if better way to do this
                                    }).ToList();

                if (!documentList.Any())
                {
                    throw new System.ArgumentException("This client does not exist or has no available records.");
                }

                foreach (var item in documentList) {
                    PublicVM objpvm = new PublicVM();

                    objpvm.Folder_ID = item.Folder_ID;
                    objpvm.Document_ID = item.Document_ID;
                    objpvm.DocumentType_ID = item.DocumentType_ID;
                    objpvm.DocumentTypeName = item.DtName;
                    objpvm.IssueDate = item.Issue_DT;
                    objpvm.Description = item.Description;
                    objpvm.Category_ID = item.Category_ID;
                    objpvm.CategoryName = item.CatName;
                    objpvm.EffectiveDate = item.Date1_DT;
                    objpvm.RefNumber = item.RefNumber;
                    objpvm.ReferenceType = item.RefNumberType_CD;
                    objpvm.FileExtension = item.FileExtension.ToLower();
                    objpvm.Method = item.Method;
                    objpvm.Originator = item.Originator;
                    objpvm.Reason = item.Reason;
                    objpvm.Supplier = item.Number1;
                    objpvm.Recipient = item.Recipient; //not sure to keep
                    objpvm.Hidden = item.Active_IND; //not sure to keep

                    PublicVMList.Add(objpvm);
                }
            }
            else {
                var documentList = (from d in _db.tbl_Document.AsNoTracking() //.AsNoTracking reduces resources by making this read only                        
                                    join f in _db.tbl_Folder.AsNoTracking() on d.Folder_ID equals f.Folder_ID
                                    //left outer join b/c not every document will have a reference number right away (aka no docreference)
                                    join dr in _db.tbl_DocReference.AsNoTracking() on d.Document_ID equals dr.Document_ID into ps
                                    join dt in _db.tbl_DocumentType.AsNoTracking() on d.DocumentType_ID equals dt.DocumentType_ID
                                    join cat in _db.tbl_Category.AsNoTracking() on dt.Category_ID equals cat.Category_ID
                                    where d.Folder_ID == publicNumberInt
                                    where d.Active_IND == true //should only show active records, hide soft deleted ones
                                    where cat.Category_ID != 6 //Ramin said the System category records can be omitted from system
                                    from dr in ps.DefaultIfEmpty()
                                    select new
                                    {
                                        f.Folder_ID,
                                        d.Document_ID,
                                        dt.DocumentType_ID,
                                        DtName = dt.Name,
                                        d.Issue_DT,
                                        d.Description,
                                        cat.Category_ID,
                                        CatName = cat.Name,
                                        dr.Date1_DT,
                                        dr.RefNumber,
                                        dr.RefNumberType_CD,
                                        d.FileExtension,
                                        d.Method,
                                        d.Originator,
                                        d.Reason,
                                        dr.Number1
                                    }).ToList();

                if (!documentList.Any()) {
                    throw new System.ArgumentException("This client does not exist or has no available records.");
                }

                foreach (var item in documentList)
                {
                    PublicVM objpvm = new PublicVM();

                    objpvm.Folder_ID = item.Folder_ID;
                    objpvm.Document_ID = item.Document_ID;
                    objpvm.DocumentType_ID = item.DocumentType_ID;
                    objpvm.DocumentTypeName = item.DtName;
                    objpvm.IssueDate = item.Issue_DT;
                    objpvm.Description = item.Description;
                    objpvm.Category_ID = item.Category_ID;
                    objpvm.CategoryName = item.CatName;
                    objpvm.EffectiveDate = item.Date1_DT;
                    objpvm.RefNumber = item.RefNumber;
                    objpvm.ReferenceType = item.RefNumberType_CD;
                    objpvm.FileExtension = item.FileExtension.ToLower();
                    objpvm.Method = item.Method;
                    objpvm.Originator = item.Originator;
                    objpvm.Reason = item.Reason;
                    objpvm.Supplier = item.Number1;

                    PublicVMList.Add(objpvm);
                }
            }

            return PublicVMList;  //the missing ref data exists here
        }

        public void Dispose() {

            _db.Dispose();
        }
    }
}