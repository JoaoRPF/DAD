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
        public int tupleID;
        public string sentByAddress;

        public ForwardTup(string[] outputTuple, int tupID)
        {
            //FALTA, POR EX. NO OPERATOR DUP actualizar um campo do output tuple tendo em consideracao os novos atributos (sentBy)
            //array the closed (para suportar envio a varios opx's)
            this.sentByAddress = "";
            this.tupleID = tupID;
            this.tup = outputTuple;
            this.closed = false;
        }

        public ForwardTup(string[] outputTuple, bool isClosed, int tupID)
        {
            this.sentByAddress = "";
            this.tupleID = tupID;
            this.tup = outputTuple;
            this.closed = isClosed;
        }

        public ForwardTup(string[] outputTuple, bool isClosed, int tupID, string whoSent)
        {
            this.sentByAddress = "";
            this.tupleID = tupID;
            this.tup = outputTuple;
            this.closed = isClosed;
        }
        public ForwardTup Clone()
        {
            return new ForwardTup(this.tup, this.closed, this.tupleID, this.sentByAddress);
        }
    }
    public interface OperatorServices
    {
        void exchangeTuples(ForwardTup tuples);
        bool checkIfTupleClosed(string whoAsked, int tupleID);
        void warnUpStreamOfAliveReplica(string opID, int repID);
        void setSendAddresses(string operatorID, string sendAddress, string routing, int repFact);
        void startToProcess();
        void printStatus();
        void freezeOperator();
        void unfreezeOperator();
        void crashOperator();
        void intervalOperator(int time);
        int ping(int m);
        void updateRepPing(int r);
        List<ForwardTup> getDeadReplicaTuples(string deadReplicaID, string whoAskedID);
    }
}
