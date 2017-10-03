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
        private WASEntities _db = null;

        public PublicRepository() {
            this._db = new WASEntities(); //dbcontext class
        }

        //maybe add parameterized constructor
        //issue with specific computers (i.e. Rupinders) caching data that should have been updated
        //should check other computers and see if this issue is related to specific computer only
        public IEnumerable<PublicVM> SelectAll(string publicNumber, string role) {

            List<PublicVM> PublicVMList = new List<PublicVM>();

            int publicNumberInt;

            try
            {
                publicNumberInt = Int32.Parse(publicNumber); //should be able to be done in LINQ
                //technically user never inputs this, provided by code from Client Id
                //should not have an issue with an exception here then
            }
            catch (FormatException e)
            {
                FormatException exception = new FormatException("Public Number must be a positive integer", e);
                exception.HelpLink = "Please check over the provided information and try again.";
                exception.Data["Public ID"] = publicNumber;

                throw exception;
            }
            catch (InvalidOperationException e)
            {
                InvalidOperationException exception = new InvalidOperationException("There was an issue connecting to the database", e);
                exception.HelpLink = "Please contact Support through ServiceNow.";

                throw exception;
            }
            catch (Exception e) {
                //maybe write more here
                throw new Exception("There seems to be an issue.", e);
            }

            //refactor this into a more efficient conditional
            if (role == "Admin")
            {
                var documentList = (from d in _db.tbl_Document.AsNoTracking() //.AsNoTracking reduces resources by making this read only                        
                                    join f in _db.tbl_Folder.AsNoTracking() on d.Folder_ID equals f.Folder_ID
                                    //left outer join b/c not every document will have a reference number right away (aka no docreference)
                                    join dr in _db.tbl_DocReference.AsNoTracking() on d.Document_ID equals dr.Document_ID into ps
                                    join dt in _db.tbl_DocumentType.AsNoTracking() on d.DocumentType_ID equals dt.DocumentType_ID
                                    join cat in _db.tbl_Category.AsNoTracking() on dt.Category_ID equals cat.Category_ID
                                    where d.Folder_ID == publicNumberInt
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
                                        Value = d.tbl_DocReference.Where(e => e.RefNumberType_CD == "Claim").FirstOrDefault().Date1_DT,
                                        Value1 = d.tbl_DocReference.Where(e => e.RefNumberType_CD == "Claim").FirstOrDefault().RefNumber,
                                        Value2 = d.LastUser_DT,
                                        d.Active_IND //only want recipient and active_ind for admin, wonder if better way to do this
                                    }).ToList();

                if (!documentList.Any())
                {
                    throw new ArgumentException("This client does not exist or has no available records.");
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
                    objpvm.Recipient = item.Recipient;
                    objpvm.DateOfLoss = item.Value;
                    objpvm.Hidden = item.Active_IND;
                    objpvm.ClaimNumber = item.Value1;
                    objpvm.ArchiveTime = item.Value2.Ticks;

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
                                        dr.Number1,
                                        Value = d.tbl_DocReference.Where(e => e.RefNumberType_CD == "Claim").FirstOrDefault().Date1_DT,
                                        Value1 = d.tbl_DocReference.Where(e => e.RefNumberType_CD == "Claim").FirstOrDefault().RefNumber,
                                        Value2 = d.LastUser_DT
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
                    objpvm.DateOfLoss = item.Value;
                    objpvm.ClaimNumber = item.Value1;
                    objpvm.ArchiveTime = item.Value2.Ticks;

                    PublicVMList.Add(objpvm);
                }
            }

            return PublicVMList;
        }

        public void Dispose() {

            _db.Dispose();
        }
    }
}