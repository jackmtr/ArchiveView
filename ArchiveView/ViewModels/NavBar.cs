using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ArchiveView.ViewModels
{
    public class NavBar : IEquatable<NavBar>
    {
        public string CategoryName { get; set; } //tbl_Category.Name
        public List<string> DocumentTypeName { get; set; } //tbl_DocumentType.Name

        public NavBar() {
            DocumentTypeName = new List<String>();
        }

        public bool Equals(NavBar other)
        {
            if (CategoryName == other.CategoryName && DocumentTypeName == other.DocumentTypeName) { return true; }
            return false;
        }
    }
}