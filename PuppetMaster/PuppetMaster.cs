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
using System.Threading;

namespace DADStorm
{

    class PuppetMaster
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static PCServices services;
        public static Form1 formPuppetMaster = new Form1();
        public static TcpChannel channel = new TcpChannel(10000);
        private static Dictionary<string, string> operatorsAddresses = new Dictionary<string, string>(); //addresses by id TODOOOOO
        private static string[] fileLines = null;
        private static int lastLine = 0;
        private static string loggingType = "light";
        private static string semantics = "";

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

        public static void reset()
        {
            lastLine = 0;
            operatorsAddresses.Clear();
            fileLines = null;
            services.resetPCS();
        }

        public static void executeCommand(string command)
        {
            try
            {
                if (command.Contains("Start"))
                {
                    string operatorID = command.Split()[1];

                    foreach (string replicaID in operatorsAddresses.Keys)
                    {
                        if (replicaID.Contains(operatorID))
                        {
                            new Thread(() => doRemoteCommand(operatorsAddresses[replicaID], "START")).Start();
                        }
                    }
                }
                else if (command.Contains("Interval"))
                {
                    string operatorID = command.Split()[1];
                    int time = Int32.Parse(command.Split()[2]);
                    PuppetMaster.formPuppetMaster.addNewLineToLog("Setting an interval of " + time + "ms for " + operatorID);
                    foreach (string replicaID in operatorsAddresses.Keys)
                    {
                        if (replicaID.Contains(operatorID))
                        {
                            new Thread(() => doRemoteCommand(operatorsAddresses[replicaID], "INTERVAL-" + time)).Start();
                        }
                    }
                }
                else if (command.Contains("Status"))
                {
                    PuppetMaster.formPuppetMaster.addNewLineToLog("I'm sending to all operators a request for status");
                    foreach (string operatorAddress in operatorsAddresses.Values)
                    {
                        new Thread(() => doRemoteCommand(operatorAddress, "STATUS")).Start();
                    }
                }
                else if (command.Contains("Freeze"))
                {
                    string operatorID = command.Split()[1];
                    string replicaID = command.Split()[2];
                    string keyOperator = operatorID + "-" + replicaID;
                    PuppetMaster.formPuppetMaster.addNewLineToLog("Freezing " + keyOperator);

                    new Thread(() => doRemoteCommand(operatorsAddresses[keyOperator], "FREEZE")).Start();
                }
                else if (command.Contains("Unfreeze"))
                {
                    string operatorID = command.Split()[1];
                    string replicaID = command.Split()[2];
                    string keyOperator = operatorID + "-" + replicaID;
                    PuppetMaster.formPuppetMaster.addNewLineToLog("Unfreezing " + keyOperator);

                    new Thread(() => doRemoteCommand(operatorsAddresses[keyOperator], "UNFREEZE")).Start();
                }
                else if (command.Contains("Wait"))
                {
                    int sleepTime = Int32.Parse(command.Split()[1]);
                    PuppetMaster.formPuppetMaster.addNewLineToLog("I'm sleeping...");
                    Thread.Sleep(sleepTime);
                    PuppetMaster.formPuppetMaster.addNewLineToLog("I'm awake!");

                }
                else if (command.Contains("Crash"))
                {
                    string operatorID = command.Split()[1];
                    string replicaID = command.Split()[2];
                    string keyOperator = operatorID + "-" + replicaID;
                    PuppetMaster.formPuppetMaster.addNewLineToLog("Crashing " + keyOperator);

                    new Thread(() => doRemoteCommand(operatorsAddresses[keyOperator], "CRASH")).Start();

                }
                else
                {
                    PuppetMaster.formPuppetMaster.addNewLineToLog("Invalid Command -> " + command);
                }
            }
            catch (Exception e)
            {
                PuppetMaster.formPuppetMaster.addNewLineToLog("Invalid Command -> " + command);
            }
        }

        public static void startReadingConfigFile(string filepath, bool step)
        {
            PuppetMasterReadConfig readConfig = new PuppetMasterReadConfig();

            //TODO FileNotFound Exception

            if (fileLines == null)
            {
                try
                {
                    fileLines = File.ReadAllLines(filepath);
                }
                catch (FileNotFoundException e)
                {
                    PuppetMaster.formPuppetMaster.addNewLineToLog("File not found");
                    return;
                }
            }

            int limit = 0;
            if (step)
            {
                limit = lastLine + 1;
                if (limit > fileLines.Length) limit = fileLines.Length;
            }
            else
            {
                limit = fileLines.Length;
            }

          
            for (int i = lastLine; i < limit; i++)
            {
                lastLine++;
                string[] line = fileLines[i].Split();
                if (!String.IsNullOrEmpty(line[0]))
                {
                    int cont = i + 1;
                    Debug.WriteLine("line " + cont + "=" + line[0]);
                    Dictionary<string, string> lineContentDictionary = readConfig.readLine(line);
                    string lineID = lineContentDictionary["LINE_ID"];
                    Debug.WriteLine("lineID = " + lineID);

                    if (lineID.Equals("LOGGING_LEVEL"))
                    {
                        PuppetMaster.formPuppetMaster.addNewLineToLog("Logging Level set to " + lineContentDictionary["TYPE"]);
                        loggingType = lineContentDictionary["TYPE"];
                    }

                    if (lineID.Equals("SEMANTICS"))
                    {
                        PuppetMaster.formPuppetMaster.addNewLineToLog("Semantics set to " + lineContentDictionary["TYPE"]);
                        semantics = lineContentDictionary["TYPE"];
                    }

                    if (lineID.Equals("OP"))
                    {
                        int replicaCount = 0;
                        string operatorID = lineContentDictionary["OPERATOR_ID"];
                        foreach (string address in lineContentDictionary["ADDRESSES"].Split('$'))
                        {
                            string replicaID = operatorID + "-" + replicaCount;
                            operatorsAddresses.Add(replicaID, address);
                            replicaCount++;
                        }

                        foreach (string key in lineContentDictionary.Keys)
                        {
                            string value = lineContentDictionary[key];
                            PuppetMaster.formPuppetMaster.addNewLineToLog(key + " = " + value);
                        }
                        PuppetMaster.formPuppetMaster.addNewLineToLog("\r\n");
                        lineContentDictionary["LOGGING_LEVEL"] = loggingType;
                        lineContentDictionary["SEMANTICS"] = semantics;
                        services.sendOperatorInfoToPCS(lineContentDictionary);
                    }

                    else if (lineID.Equals("START"))
                    {
                        string operatorID = lineContentDictionary["OPERATOR_ID"];
                        foreach (string replicaID in operatorsAddresses.Keys)
                        {
                            if (replicaID.Contains(operatorID))
                            {
                                new Thread(() => doRemoteCommand(operatorsAddresses[replicaID], "START")).Start();
                            }
                        }
                    }

                    else if (lineID.Equals("INTERVAL"))
                    {
                        string operatorID = lineContentDictionary["OPERATOR_ID"];
                        int time = Int32.Parse(lineContentDictionary["TIME"]);
                        PuppetMaster.formPuppetMaster.addNewLineToLog("Setting an interval of " + time + "ms for " + operatorID);
                        foreach (string replicaID in operatorsAddresses.Keys)
                        {
                            if (replicaID.Contains(operatorID))
                            {
                                new Thread(() => doRemoteCommand(operatorsAddresses[replicaID], "INTERVAL-"+time)).Start();
                            }
                        }

                    }

                    else if (lineID.Equals("STATUS"))
                    {
                        PuppetMaster.formPuppetMaster.addNewLineToLog("I'm sending to all operators a request for status");
                        foreach (string operatorAddress in operatorsAddresses.Values)
                        {
                            PuppetMaster.formPuppetMaster.addNewLineToLog("OPERATOR ADDRESS = " + operatorAddress);
                           
                            new Thread(() => doRemoteCommand(operatorAddress, "STATUS")).Start();
                                               
                        }
                    }

                    else if (lineID.Equals("FREEZE"))
                    {
                        string operatorID = lineContentDictionary["OPERATOR_ID"];
                        string replicaID = lineContentDictionary["REPLICA_ID"];
                        string keyOperator = operatorID + "-" + replicaID;
                        PuppetMaster.formPuppetMaster.addNewLineToLog("Freezing " + keyOperator);
                       
                        new Thread(() => doRemoteCommand(operatorsAddresses[keyOperator], "FREEZE")).Start();
                    }

                    else if (lineID.Equals("UNFREEZE"))
                    {
                        string operatorID = lineContentDictionary["OPERATOR_ID"];
                        string replicaID = lineContentDictionary["REPLICA_ID"];
                        string keyOperator = operatorID + "-" + replicaID;
                        PuppetMaster.formPuppetMaster.addNewLineToLog("Unfreezing " + keyOperator);

                        new Thread(() => doRemoteCommand(operatorsAddresses[keyOperator], "UNFREEZE")).Start();
                    }

                    else if (lineID.Equals("WAIT"))
                    {
                        int sleepTime = Int32.Parse(lineContentDictionary["TIME"]);
                        Thread.Sleep(sleepTime);
                    }

                    else if (lineID.Equals("CRASH"))
                    {
                        string operatorID = lineContentDictionary["OPERATOR_ID"];
                        string replicaID = lineContentDictionary["REPLICA_ID"];
                        string keyOperator = operatorID + "-" + replicaID;
                        PuppetMaster.formPuppetMaster.addNewLineToLog("Crashing " + keyOperator);
                        new Thread(() => doRemoteCommand(operatorsAddresses[keyOperator], "CRASH")).Start();
                    }
                }
            }
        }

        private static void doRemoteCommand(string address, string command)
        {
            try
            {
                OperatorServices opServicesRemote = (OperatorServices)Activator.GetObject(
                                        typeof(OperatorServices),
                                        address);
                int time = 0;
                if (command.Contains("INTERVAL"))
                {
                    string[] infos = command.Split('-');
                    command = infos[0];
                    time = Int32.Parse(infos[1]);
                }
                switch (command)
                {
                    case "START":
                        opServicesRemote.startToProcess();
                        break;
                    case "INTERVAL":
                        opServicesRemote.intervalOperator(time);
                        break;
                    case "FREEZE":
                        opServicesRemote.freezeOperator();
                        break;
                    case "UNFREEZE":
                        opServicesRemote.unfreezeOperator();
                        break;
                    case "STATUS":
                        opServicesRemote.printStatus();
                        break;
                    case "CRASH":
                        opServicesRemote.crashOperator();
                        break;
                    default:
                        break;
                }
                
            }
            catch (Exception e)
            {
                PuppetMaster.formPuppetMaster.addNewLineToLog("I think that Operator in " + address + " was murdered");
            }
        }
    }

    class PuppetMasterServicesClass: MarshalByRefObject, PuppetMasterServices
    {
        public string operatorConnected (string msgFromOperator)
        {       
            PuppetMaster.formPuppetMaster.addNewLineToLog("Msg from Operator: " + msgFromOperator);
            return msgFromOperator;
        }

        public void addMessageToLog(string message)
        {
            PuppetMaster.formPuppetMaster.addNewLineToLog(message);
        }
    }
}
