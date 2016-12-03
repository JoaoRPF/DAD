using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    public interface PuppetMasterServices
    {
        
        string operatorConnected(string name);
        void addMessageToLog(string message);
    }
}
