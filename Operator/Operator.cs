using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace DADSTORM
{
    class Operator
    {
        private string type = "";
        private string id = "";
        private int port = 0;
        private int fieldNumber = 0;
        private string condition = "";
        private string conditionValue = "";
        private string status = "";
        //private string dll = "";
        //private classCustom = "";
        //private methodCustom = "";
        private List<string> sendAddresses = new List<string>();
        private string routing = "";
        private int repFact = 0;

        public Operator() {}

        static void Main(string[] args)
        {
            System.Console.WriteLine("Entrei na Main");
            Operator operador = new Operator();

            operador.connectToPuppetMaster();
            System.Console.ReadLine();
        }

        private void connectToPuppetMaster()
        {

            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

            PuppetMasterServices services = (PuppetMasterServices)Activator.GetObject(
                typeof(PuppetMasterServices),
                "tcp://localhost:10000/PuppetMasterServices");

            System.Console.WriteLine(services.operatorConnected("Operator 1"));
        }
    }
}
