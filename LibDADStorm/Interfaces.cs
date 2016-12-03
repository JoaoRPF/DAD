using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    public interface IOperator
    {
        IList<IList<string>> CustomOperation(IList<string> l);
    }
}