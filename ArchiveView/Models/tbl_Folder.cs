//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ArchiveView.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class tbl_Folder
    {
        public int Folder_ID { get; set; }
        public int Cabinet_ID { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
        public int Security { get; set; }
        public System.DateTime LastUser_DT { get; set; }
    
        public virtual tbl_Cabinet tbl_Cabinet { get; set; }
    }
}
