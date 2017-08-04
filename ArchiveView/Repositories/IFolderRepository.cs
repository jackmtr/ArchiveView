using ArchiveView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveView.Repositories
{
    public interface IFolderRepository
    {
        tbl_Folder SelectByID(string id);

        tbl_Folder SelectByNumber(string number);

        void Dispose();
    }
}
