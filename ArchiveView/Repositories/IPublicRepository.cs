using ArchiveView.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveView.Repositories
{
    public interface IPublicRepository
    {
        IEnumerable<PublicVM> SelectAll(string publicNumber, string role);

        void Dispose();
    }
}
