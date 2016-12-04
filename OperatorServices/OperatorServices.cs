using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    public interface OperatorServices
    {
        void exchangeTuples(Tup tuples);
        void addSequenceNumber();
        void addProcessingNumber();
        void destroyTuple(int id);

        void setSendAddresses(string operatorID, string sendAddress);
        void startToProcess();
        void printStatus();
        void freezeOperator();
        void unfreezeOperator();
        void crashOperator();
        void intervalOperator(int time);
    }

    [Serializable]
    public struct Tup
    {
        public int id;
        public string[] fields;

        public Tup(int pID, string[] pFields)
        {
            id = pID;
            fields = pFields;
        }

        public Tup Clone()
        {
            return new Tup(this.id, (string[])this.fields.Clone());
        }
    }
}
