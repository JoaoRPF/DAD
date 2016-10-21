using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.IO;
using System.Diagnostics;
using System.Collections;

namespace DADSTORM
{

    class PuppetMaster
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static PCServices services;
        public static Form1 formPuppetMaster = new Form1();
        public static TcpChannel channel = new TcpChannel(10000);

        [STAThread]
        static void Main()
        {
            initPuppetMasterChannel();
            connectToPCS();

            Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(formPuppetMaster);
        }

        private static void initPuppetMasterChannel()
        {
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PuppetMasterServicesClass),
                "PuppetMasterServices",
                WellKnownObjectMode.Singleton);
        }

        private static void connectToPCS()
        {
            services = (PCServices)Activator.GetObject(
                typeof(PCServices),
                "tcp://localhost:10001/PCService");
        }

        public static void startReadingConfigFile(string filepath)
        {
            PuppetMasterReadConfig readConfig = new PuppetMasterReadConfig();
            string[] lines = File.ReadAllLines(filepath);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] line = lines[i].Split();
                if (!String.IsNullOrEmpty(line[0]))
                {
                    Debug.WriteLine("line 0 = " + line[0]);
                    Dictionary<string, string> lineContentDictionary = readConfig.readLine(line);
                    string lineID = lineContentDictionary["LINE_ID"];
                    Debug.WriteLine("lineID = " + lineID);

                    if (lineID.Equals("OP"))
                    {
                        foreach (string key in lineContentDictionary.Keys)
                        {
                            string value = lineContentDictionary[key];
                            Debug.WriteLine("OLA");
                            PuppetMaster.formPuppetMaster.addNewLineToLog(key + " = " + value);
                        }
                    }
                }
            }
        }
    }

    class PuppetMasterServicesClass: MarshalByRefObject, PuppetMasterServices
    {
        public string operatorConnected (String name)
        {       
            PuppetMaster.formPuppetMaster.addNewLineToLog(name);
            return name;
        }
    }
}
