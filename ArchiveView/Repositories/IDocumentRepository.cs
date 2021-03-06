﻿using ArchiveView.Models;
using ArchiveView.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveView.Repositories
{
    public interface IDocumentRepository
    {
        tbl_Document SelectById(string id, bool authorized);

        IEnumerable<tbl_Document> SelectAll(string id);

        MiscPublicData GetMiscPublicData(string publicNumber);

        bool UpdateChanges(PublicVM doc, WASEntities db);

        void Dispose();
    }
}
