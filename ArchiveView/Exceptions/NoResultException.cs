using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ArchiveView.Exceptions
{
    //custom exception that is used when query result leads to no client being found, when there should always be one
    public class NoResultException : Exception
    {
        public NoResultException() { }

        public NoResultException(string message) : base(message) { }
    }
}