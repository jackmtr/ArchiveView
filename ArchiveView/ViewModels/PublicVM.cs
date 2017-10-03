using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ArchiveView.ViewModels
{
    public class PublicVM
    {
        public int Folder_ID { get; set; } //tbl_Folder.Folder_ID

        [Display(Name = "Document ID")]
        public int Document_ID { get; set; } //tbl_Document.Document_ID

        public int DocumentType_ID { get; set; } //tbl_DocumentType.DocumentType_ID

        [Display(Name = "Type of Document")]
        public string DocumentTypeName { get; set; } //tbl_DocumentType.Name

        [Display(Name = "Issue Date")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd MMM yyyy}")]
        public Nullable<DateTime> IssueDate { get; set; } //tbl_Document.Issue_DT

        [Required(ErrorMessage = "A Description is Required")]
        public string Description { get; set; } //tbl_Document.Description

        public int Category_ID { get; set; } //tbl_Category.Category_ID

        [Display(Name = "Category")]
        public string CategoryName { get; set; } //tbl_Category.Name

        [Display(Name = "Effective Date")]
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        public Nullable<DateTime> EffectiveDate { get; set; } //tbl_DocReference.Date1_DT

        public string RefNumber { get; set; } //tbl_DocReference.RefNumber

        [Display(Name = "File Type")]
        public string FileExtension { get; set; } //tbl_Document.FileType

        public string Method { get; set; } //tbl_Document.Method

        public string Originator { get; set; } //tbl_Document.Originator

        public string Reason { get; set; } //tbl_Document.Reason

        public string Supplier { get; set; } //tbl_DocReference.Number1

        public string Recipient { get; set; } //tbl_Document.Recipient, if can only have for admin that would be nice

        public bool Hidden { get; set; } //tbl_Document.Active_IND, adding to allow easier access for admin to hide document, testing and could end up not using

        [Display(Name = "Reference Type")]
        public string ReferenceType { get; set; } //tbl_DocReference.RefNumberType_CD

        [Display(Name = "Date of Loss")]
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        public Nullable<DateTime> DateOfLoss { get; set; }

        [Display(Name = "Claim #")]
        public string ClaimNumber { get; set; }

        //going to pull the hour/min/sec out of tbl_document.LastUser_DT to make as a tiebreaker for order by
        //doesnt need a display
        public long ArchiveTime { get; set; }
    }
}