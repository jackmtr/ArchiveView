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

        //not being used
        //public tbl_Folder SelectByID(string id)
        //{
        //    //do this better
        //    return _db.tbl_Folder.Find(Int32.Parse(id));
        //}

        public tbl_Folder SelectByNumber(string number) {

            int clientId;

            try
            {
                clientId = Int32.Parse(number);

                //.AsNoTracking reduces resources by making this read only     
                return _db.tbl_Folder.AsNoTracking().SingleOrDefault(folder => folder.Number == clientId);
            }
            catch (FormatException e)
            {
                FormatException exception = new FormatException("Client Id must be a positive integer", e);
                exception.HelpLink = "Please check over the provided information and try again.";
                exception.Data["Client ID"] = number;

                throw exception;
            }
            catch (InvalidOperationException e) {

                InvalidOperationException exception = new InvalidOperationException("There was an issue connecting to the database", e);
                exception.HelpLink = "Please contact Support through ServiceNow.";

                throw exception;
            }
            catch (Exception e) {
                //maybe write more here
                throw new Exception("There seems to be an issue.", e);
            }
        }

        public void Dispose() {
            _db.Dispose();
        }
    }
}