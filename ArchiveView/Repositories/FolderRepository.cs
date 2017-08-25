using ArchiveView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ArchiveView.Repositories
{
    public class FolderRepository : IFolderRepository
    {
        private WASEntities _db = null;

        public FolderRepository()
        {
            this._db = new WASEntities();
        }

        public tbl_Folder SelectByID(string id)
        {
            //do this better
            return _db.tbl_Folder.Find(Int32.Parse(id));
        }

        public tbl_Folder SelectByNumber(string number) {

            int clientId;

            try {
                clientId = Int32.Parse(number);
            } catch {
                clientId = 0;//will need to make null, as 0 exists?
            }

            //.AsNoTracking reduces resources by making this read only      
            return _db.tbl_Folder.AsNoTracking().SingleOrDefault(folder => folder.Number == clientId);
        }

        public void Dispose() {
            _db.Dispose();

        }
    }
}