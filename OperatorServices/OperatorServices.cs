using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    public interface OperatorServices
    {
        void exchangeTuples(string[] tuples);
        void setSendAddresses(string operatorID, string sendAddress, string routing, int repFact);
        void startToProcess();
        void printStatus();
        void freezeOperator();
        void unfreezeOperator();
        void crashOperator();
        void intervalOperator(int time);
    }
}
