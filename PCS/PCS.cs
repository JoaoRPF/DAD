using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.IO;
using System.Diagnostics;

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
            System.Console.WriteLine("Sou o pcs");
            return "Sou o PuppetMaster";
        }

        public string changeInfo(string info)
        {
            string operatorPath = BuildPaths.getExecPath("\\Operator\\bin\\Debug\\Operator.exe");
            string operatorArgs = info;
            ProcessStartInfo operatorStartInfo = new ProcessStartInfo();
            operatorStartInfo.FileName = operatorPath;
            operatorStartInfo.Arguments = operatorArgs;
            Process operatorProcess = Process.Start(operatorStartInfo);
            System.Console.WriteLine("Comecei processo com info: " + info);
            return "PCS recebeu: " + info;
        }
    }

    public static class BuildPaths
    {
        public static string processPath;

        public static string getPath()
        {
            string currentPath = Directory.GetCurrentDirectory();
            string beforePath = Directory.GetParent(currentPath).ToString();
            string beforePath2 = Directory.GetParent(beforePath).ToString();
            processPath = Directory.GetParent(beforePath2).ToString();
            return processPath;
        }

        public static string getExecPath(string execPath)
        {
            processPath = getPath() + execPath;
            return processPath;
        }
    }
}
