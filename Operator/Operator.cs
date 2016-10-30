using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.IO;
using System.Diagnostics;

namespace DADSTORM
{
    public class Operator : MarshalByRefObject, OperatorServices
    {
        public string id = "";
        public int repID = -1;
        public string myAddress = "";
        public string input = "";
        public string status = "";
        public int repFact = 0;
        public string type = "";
        public int fieldNumber = -1;
        public string condition = "";
        public string conditionValue = "";
        public List<string> sendAddresses = new List<string>();
        public string routing = "";

        public static TcpChannel channel;
        public static Operator _operator;
        public List<string[]> inputTuples = new List<string[]>();

        //public string dll = "";
        //public classCustom = "";
        //public methodCustom = "";

        public Operator() {}

        static void Main(string[] args)
        {
            Console.WriteLine("SIZE ARGS = " + args.Length);
            string operatorType = args[6];
            switch (operatorType){
                case "COUNT":
                    _operator = new Count();
                    break;
                case "UNIQ":
                    _operator = new Uniq();
                    break;
                default:
                    _operator = new Operator();
                    break;
            }
            _operator.id = args[0];
            _operator.repID = Int32.Parse(args[1]);
            _operator.myAddress = args[2];
            channel = new TcpChannel(getPort(_operator.myAddress));
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

            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(OperatorServices),
                "OperatorServices",
                WellKnownObjectMode.Singleton);

            if (_operator.id.Equals("OP1"))
                _operator.execute(null);

            System.Console.ReadLine(); //So para manter a janela aberta
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

        private static int getPort(string address)
        {
            int port = Int32.Parse(address.Split(':')[2].Split('/')[0]);
            return port;
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

        public List<string[]> readInputFromFile(string inputFilepath)
        {
            List<string[]> inputList = new List<string[]>();
            string[] lines = File.ReadAllLines(inputFilepath);
            for (int i=0; i < lines.Length; i++)
            {
                string[] line = lines[i].Split(new[] {", "}, StringSplitOptions.None);
                if (!String.IsNullOrEmpty(line[0])){
                    if (!line[0].StartsWith("%%"))
                    {
                        string[] fields = new string[line.Length];
                        for (int j=0; j < line.Length; j++)
                        {
                            fields[j] = line[j];
                        }
                        inputList.Add(fields);
                    }
                }
            }
            return inputList;
        }

        public void printTuples(List<string[]> tuples)
        {
            foreach (string[] _tuple in tuples){
                for (int i=0; i<_tuple.Length; i++)
                {
                    Console.WriteLine(i + " = " + _tuple[i]);
                }
            }
        }

        public virtual void execute()
        {
            Console.WriteLine("GENERAL OPERATOR");
        }

        public void exchangeTuples(string[] tuple)
        {
            inputTuples.Add(tuple);
            //_operator.execute();
        }
    }
}
