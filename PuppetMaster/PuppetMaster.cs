using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace DADSTORM
{
    static class PuppetMaster
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static PCServices services;

        [STAThread]
        static void Main()
        {
            connectToPCS();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static void connectToPCS()
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

            services = (PCServices)Activator.GetObject(
                typeof(PCServices),
                "tcp://localhost:10001/PCService");
        }
    }
}
