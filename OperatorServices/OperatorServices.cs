using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    [Serializable]
    public struct ForwardTup
    {
        public bool closed;
        public string[] tup;

        public ForwardTup(string[] outputTuple)
        {
            //FALTA, POR EX. NO OPERATOR DUP actualizar um campo do output tuple tendo em consideracao os novos atributos (sentBy)
            //array the closed (para suportar envio a varios opx's)
            //this.id
            //this.sentByAddress = whoSent;
            this.tup = outputTuple;
            this.closed = false;
        }

        public ForwardTup(string[] outputTuple, bool isClosed)
        {
            this.tup = outputTuple;
            this.closed = isClosed;
        }

        public ForwardTup Clone()
        {
            return new ForwardTup(this.tup, this.closed);
        }
    }
    public interface OperatorServices
    {
        void exchangeTuples(ForwardTup tuples);
        void setSendAddresses(string operatorID, string sendAddress, string routing, int repFact);
        void startToProcess();
        void printStatus();
        void freezeOperator();
        void unfreezeOperator();
        void crashOperator();
        void intervalOperator(int time);
        int ping(int m);
        void updateRepPing(int r);
        List<ForwardTup> getDeadReplicaTuples(string operatorID);
    }
}
