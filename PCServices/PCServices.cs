using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DADStorm
{
    public interface PCServices
    {
        string printHello();
        string changeInfo(string msg);
        string sendOperatorInfoToPCS(Dictionary<string, string> _operator);
        //string startOperator(string operatorID);
        void resetPCS();
    }
}
