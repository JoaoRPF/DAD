using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    public class DadStormTimeOutException : Exception
    {
        public DadStormTimeOutException() {}

        public DadStormTimeOutException(string message) : base (message) {}

        public DadStormTimeOutException(string message, Exception inner) : base (message, inner) {}
    }
}
