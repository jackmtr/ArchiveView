using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ArchiveView.Exceptions
{
    public class NoResultException : Exception
    {
        public NoResultException() { }

        public NoResultException(string message) : base(message) { }
    }
}