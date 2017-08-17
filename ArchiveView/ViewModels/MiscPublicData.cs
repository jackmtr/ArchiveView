using ArchiveView.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ArchiveView.ViewModels
{
    public class MiscPublicData
    {
        public int Document_ID { get; set; }

        public string Branch { get; set; }

        public string Creator { get; set; }

        [Display(Name = "Archive Time")]
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy, h:mm:ss tt}")]
        public Nullable<DateTime> ArchiveTime { get; set; }

        public string Reason { get; set; }

        public string Recipient { get; set; }

        public byte[] File { get; set; }

        [Display(Name = "File Size")]
        public int FileSize { get; set; }

        //[Display(Name = "File Size2")]
        //public int FileSize2 { get; set; }

        public ICollection<tbl_DocReference> DocReferences { get; set; }

    }
}