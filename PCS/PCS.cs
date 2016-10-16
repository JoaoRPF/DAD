using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace DADSTORM
{
    class PCS
    {
        static void Main(string[] args)
        {
            TcpChannel channel = new TcpChannel(10001);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PCServ), "PCService",
                WellKnownObjectMode.Singleton);
            System.Console.ReadLine();
        }
    }

    class PCServ : MarshalByRefObject, PCServices
    {
        public string printHello()
        {
            System.Console.WriteLine("Hello world");
            return "HELLO Puppet Master";
        }
    }
}
