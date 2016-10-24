using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace DADSTORM
{
    public class Operator
    {
        public string id = "";
        public int repID = -1;
        public string myAddress = "";
        public string input = "";
        public string status = "";
        public int repFact = 0;
        public string type = "";
        public int fieldNumber = 0;
        public string condition = "";
        public string conditionValue = "";
        public List<string> sendAddresses = new List<string>();
        public string routing = "";

        //public string dll = "";
        //public classCustom = "";
        //public methodCustom = "";

        public Operator() {}

        static void Main(string[] args)
        {
            Console.WriteLine("SIZE ARGS = " + args.Length);
            Operator _operator = new Operator();
            _operator.id = args[0];
            _operator.repID = Int32.Parse(args[1]);
            _operator.myAddress = args[2];
            _operator.input = args[3];
            _operator.status = args[4];
            _operator.repFact = Int32.Parse(args[5]);
            _operator.type = args[6];
            if (_operator.type.Equals("UNIQ"))
            {
                _operator.fieldNumber = Int32.Parse(args[7]);
                if (args.Length > 8)
                    _operator.sendAddresses = addressesToSendToList(args[8]);
            }
            else if (_operator.type.Equals("FILTER"))
            {
                _operator.fieldNumber = Int32.Parse(args[7]);
                _operator.condition = args[8];
                _operator.conditionValue = args[9];
                if (args.Length > 10)
                    _operator.sendAddresses = addressesToSendToList(args[10]);
            }
            /*else if (_operator.type.Equals("CUSTOM"))
            {

            }*/
            _operator.print();
            _operator.connectToPuppetMaster();
            System.Console.ReadLine();
        }

        private static List<string> addressesToSendToList(string addressesToSend)
        {
            List<string> listAddressesToSend = new List<string>();
            foreach (string address in addressesToSend.Split('$'))
            {
                listAddressesToSend.Add(address);    
            }
            return listAddressesToSend;
        }

        private void connectToPuppetMaster()
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

            PuppetMasterServices services = (PuppetMasterServices)Activator.GetObject(
                typeof(PuppetMasterServices),
                "tcp://localhost:10000/PuppetMasterServices");

            System.Console.WriteLine(services.operatorConnected("Operator " + this.id + " with repID " + this.repID + " has started"));
        }

        public void duplicate(Operator _operator)
        {
            this.id = _operator.id;
            this.repID = _operator.repID;
            this.myAddress = _operator.myAddress;
            this.input = _operator.input;
            this.status = _operator.status;
            this.repFact = _operator.repFact;
            this.type = _operator.type;
            this.fieldNumber = _operator.fieldNumber;
            this.condition = _operator.condition;
            this.conditionValue = _operator.conditionValue;
            //this.routing = _operator.routing;

        }

        public void print()
        {
            Console.WriteLine("ID = " + this.id);
            Console.WriteLine("REP_ID = " + this.repID);
            Console.WriteLine("MY_ADDRESS = " + this.myAddress);
            Console.WriteLine("INPUT = " + this.input);
            Console.WriteLine("STATUS = " + this.status);
            Console.WriteLine("REP_FACT = " + this.repFact);
            Console.WriteLine("TYPE = " + this.type);
            Console.WriteLine("FIELD_NUMBER = " + this.fieldNumber);
            Console.WriteLine("CONDITION = " + this.condition);
            Console.WriteLine("CONDITION_VALUE = " + this.conditionValue);
            int cont = 0;
            foreach (string address in this.sendAddresses)
            {
                Console.WriteLine("ADDRESS TO SEND " + cont + ": " + address);
                cont++;
            }
            //Console.WriteLine("ROUTING = " + this.routing);
        }
    }
}
