﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace DADStorm
{
    public class Operator : MarshalByRefObject, OperatorServices
    {
        private int TIMEOUT = 10; //10 seconds for remote call timeout

        public string id = "";
        public int repID = -1;
        public string myAddress = "";
        public string input = "";
        public List<string> inputOPs = new List<string>();
        public List<string> inputFile = new List<string>();
        public string status = "";
        public int repFact = 0;
        public string type = "";
        public string routing = "";
        public int fieldNumber = -1;
        public string condition = "";
        public string conditionValue = "";
        public string dllCustom = "";
        public string classCustom = "";
        public string methodCustom = "";
        public string loggingLevel = "";
        public string semantics = "";
        public Dictionary<string, string> sendAddresses = new Dictionary<string, string>();
        public List<string> previousAddresses = new List<string>();

        public TcpChannel channel;
        public static Operator _operator;
        public static OperatorServices operatorServices;
        public static PuppetMasterServices puppetMasterServices;
        public List<string[]> inputTuples = new List<string[]>();
        public List<string[]> outputTuples = new List<string[]>();

        public Thread executeThread;
        public Thread sendTuplesThread;

        public AutoResetEvent eventExecute = new AutoResetEvent(false);
        public AutoResetEvent eventSendTuples = new AutoResetEvent(false);
        public AutoResetEvent eventPreventReturn = new AutoResetEvent(false);
        public bool freeze = false;
        public int sleepingTime = 0;
        public bool sleepingMode = false;

        public Dictionary<string, int> dictionaryPrimaryReplica = new Dictionary<string, int>();

        public Dictionary<string, List<int>> aliveReplicas = new Dictionary<string, List<int>>();

        public int contador = 0;

        public Operator() {}

        static void Main(string[] args)
        {
            string operatorType = args[6];
            switch (operatorType){
                case "COUNT":
                    _operator = new Count();
                    break;
                case "UNIQ":
                    _operator = new Uniq();
                    break;
                case "FILTER":
                    _operator = new Filter();
                    break;
                case "DUP":
                    _operator = new Duplicate();
                    break;
                case "CUSTOM":
                    _operator = new Custom();
                    break;
                default:
                    _operator = new Operator();
                    break;
            }
            _operator.id = args[0];
            _operator.repID = Int32.Parse(args[1]);
            _operator.myAddress = args[2];
            _operator.channel = new TcpChannel(getPort(_operator.myAddress));
            foreach (string input in args[3].Split('$'))
            {
                if (input.Contains(".dat"))
                {
                    _operator.inputFile.Add(input);
                } else if (input.Contains("OP"))
                {
                    _operator.inputOPs.Add(input);
                } else
                {
                    System.Console.WriteLine("unrecognized input!");
                }
            }
            //_operator.input = args[3];
            _operator.status = args[4];
            _operator.repFact = Int32.Parse(args[5]);
            _operator.type = args[6];
            _operator.routing = args[7];
            _operator.loggingLevel = args[8];
            _operator.semantics = args[9];
            if (_operator.type.Equals("UNIQ"))
            {
                _operator.fieldNumber = Int32.Parse(args[10]);
                if (args.Length > 11)
                    _operator.previousAddresses = previousAddressesToList(args[11]);
            }
            else if (_operator.type.Equals("FILTER"))
            {
                _operator.fieldNumber = Int32.Parse(args[10]);
                _operator.condition = args[11];
                _operator.conditionValue = args[12];
                if (args.Length > 13)
                    _operator.previousAddresses = previousAddressesToList(args[13]);
            }
            else if (_operator.type.Equals("DUP") || _operator.type.Equals("COUNT"))
            {
                if (args.Length > 11)
                    _operator.previousAddresses = previousAddressesToList(args[11]);
            }
            else if (_operator.type.Equals("CUSTOM"))
            {
                _operator.dllCustom = args[11];
                _operator.classCustom = args[12];
                _operator.methodCustom = args[13];
                if (args.Length > 14)
                    _operator.previousAddresses = previousAddressesToList(args[14]);
            }

            ChannelServices.RegisterChannel(_operator.channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(Operator),
                "op",
                WellKnownObjectMode.Singleton);

            //ChannelServices.UnregisterChannel(channel);

            //RemotingConfiguration.GetRegisteredWellKnownServiceTypes

            sendAddressesToPreviousOP();
            connectToPuppetMaster();

            System.Console.WriteLine("Init Operator");
            System.Console.ReadLine(); //So para manter a janela aberta
        }

        private static void sendAddressesToPreviousOP()
        {
            try
            {
                foreach (string address in _operator.previousAddresses)
                {
                    operatorServices = (OperatorServices)Activator.GetObject(
                                              typeof(OperatorServices),
                                              address);
                    string replica = _operator.id + "-" + _operator.repID;
                    operatorServices.setSendAddresses(replica, _operator.myAddress);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception e = " + e.Message);
            }
        }

        private static List<string> previousAddressesToList(string addressesToSend)
        {
            List<string> listAddresses = new List<string>();
            foreach (string address in addressesToSend.Split('$'))
            {
                listAddresses.Add(address);    
            }
            return listAddresses;
        }

        private static int getPort(string address)
        {
            int port = Int32.Parse(address.Split(':')[2].Split('/')[0]);
            return port;
        }

        private static void readFileToInputBuffer(string file)
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] line = lines[i].Split(new[] { ", " }, StringSplitOptions.None);
                if (!String.IsNullOrEmpty(line[0]))
                {
                    if (!line[0].StartsWith("%%"))
                    {
                        string[] fields = new string[line.Length];
                        for (int j = 0; j < line.Length; j++)
                        {
                            fields[j] = line[j];
                        }
                        _operator.inputTuples.Add(fields);
                    }
                }
            }
        }

        private static void connectToPuppetMaster()
        {
            puppetMasterServices = (PuppetMasterServices)Activator.GetObject(
                typeof(PuppetMasterServices),
                "tcp://localhost:10000/PuppetMasterServices");

            puppetMasterServices.operatorConnected("Operator " + _operator.id + " with repID " + _operator.repID + " has started");
        }

        public void duplicate(Operator _operator)
        {
            this.id = _operator.id;
            this.repID = _operator.repID;
            this.myAddress = _operator.myAddress;
            this.input = _operator.input;
            //this.inputOPs = _operator.inputOPs;
            //this.inputFile = _operator.inputFile;
            this.status = _operator.status;
            this.repFact = _operator.repFact;
            this.type = _operator.type;
            this.fieldNumber = _operator.fieldNumber;
            this.condition = _operator.condition;
            this.conditionValue = _operator.conditionValue;
            this.routing = _operator.routing;
            this.dllCustom = _operator.dllCustom;
            this.classCustom = _operator.classCustom;
            this.methodCustom = _operator.methodCustom;
            this.loggingLevel = _operator.loggingLevel;
            this.semantics = _operator.semantics;
        }

        public void print()
        {
            Console.WriteLine("ID = " + this.id);
            Console.WriteLine("REP_ID = " + this.repID);
            Console.WriteLine("MY_ADDRESS = " + this.myAddress);
            foreach (string input in this.inputFile)
            {
                Console.WriteLine("INPUT_FILE = " + input);
            }
            foreach (string input in this.inputOPs)
            {
                Console.WriteLine("INPUT_OP = " + input);
            }
            Console.WriteLine("STATUS = " + this.status);
            Console.WriteLine("REP_FACT = " + this.repFact);
            Console.WriteLine("TYPE = " + this.type);
            Console.WriteLine("FIELD_NUMBER = " + this.fieldNumber);
            Console.WriteLine("CONDITION = " + this.condition);
            Console.WriteLine("CONDITION_VALUE = " + this.conditionValue);
            int cont = 0;
            foreach (string address in this.sendAddresses.Values)
            {
                Console.WriteLine("ADDRESS TO SEND " + cont + ": " + address);
                cont++;
            }
            cont = 0;
            foreach (string address in this.previousAddresses)
            {
                Console.WriteLine("PREVIOUS ADDRESS " + cont + ": " + address);
                cont++;
            }
            Console.WriteLine("ROUTING = " + this.routing);
            Console.WriteLine("DLL = " + this.dllCustom);
            Console.WriteLine("CLASS = " + this.classCustom);
            Console.WriteLine("METHOD = " + this.methodCustom);
            Console.WriteLine("LOGGING LEVEL = " + this.loggingLevel);
            Console.WriteLine("SEMANTICS = " + this.semantics);
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
            //Console.WriteLine("GENERAL OPERATOR");
            if (_operator.freeze)
            {
                Console.WriteLine("Freezing...");
                _operator.eventExecute.WaitOne();
            }
        }

        public void checkSleeping()
        {
            if (_operator.sleepingMode)
            {
                Thread.Sleep(_operator.sleepingTime);
            }
        }

        public void startToProcess()
        {
            _operator.status = "Running";
            _operator.print();

            _operator.executeThread = new Thread(_operator.execute);
            _operator.executeThread.Start();

            _operator.sendTuplesThread = new Thread(_operator.sendTuples);
            _operator.sendTuplesThread.Start();

            foreach (string file in _operator.inputFile)
            {
                readFileToInputBuffer(file);
            }

            if (_operator.routing.Equals("primary"))
            {
                _operator.initPrimaryReplicasDict();
            }

            _operator.initAliveReplicas();
        }

        public void sendTuples()
        {
            Console.WriteLine("sendTuples starting");
            while (true)
            {
                if (_operator.freeze)
                {
                    _operator.eventSendTuples.WaitOne();
                }
                if (_operator.outputTuples.Count != 0)
                {
                    string[] outputTuple;
                    lock (_operator.outputTuples)
                    {
                        outputTuple = (string[])_operator.outputTuples[0].Clone();
                    }
                    
                    lock (_operator.sendAddresses)
                    {
                        string routing = _operator.routing;
                        int fieldNumberHashing = -1;
                        if (routing.Contains('('))
                        {
                            routing = "hashing";
                            fieldNumberHashing = Int32.Parse(_operator.routing.Split('(')[1].Split(')')[0]);

                        }
                        switch (routing)
                        {
                            case "primary":
                                bool receivedAnswer = true;
                                if (_operator.sendAddresses.Count != 0)
                                {
                                    List<string> savedReplicaIDs = new List<string>();
                                    foreach (string replicaID in _operator.sendAddresses.Keys)
                                    {
                                        int replicaToSendPrimary = dictionaryPrimaryReplica[replicaID.Split('-')[0]];
                                        if (replicaID.Contains("-" + replicaToSendPrimary.ToString()))
                                        {
                                            string sendAddress = _operator.sendAddresses[replicaID];
                                            try
                                            {
                                                operatorServices = (OperatorServices)Activator.GetObject(
                                                                    typeof(OperatorServices),
                                                                    sendAddress);
                                                Console.WriteLine("Sending to " + sendAddress + " the following tuple -> " + constructTuple(outputTuple));
                                                var task = Task.Run(() => operatorServices.exchangeTuples(outputTuple));
                                                if (task.Wait(TimeSpan.FromSeconds(TIMEOUT)))
                                                {
                                                    contador++;
                                                    Console.WriteLine("Received Answer " + contador);
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Did not received answer");
                                                    savedReplicaIDs.Add(replicaID);
                                                    receivedAnswer = false;
                                                    throw new DadStormTimeOutException();
                                                }
                                                //operatorServices.exchangeTuples(outputTuple);
                                                if (_operator.loggingLevel.Equals("full"))
                                                {
                                                    new Thread(() => puppetMasterServices.addMessageToLog(constructMsgToLog(outputTuple))).Start();                                                  
                                                }
                                            }
                                            catch (System.Net.Sockets.SocketException e) //Operator crashou
                                            {
                                                Console.WriteLine("I think that Operator " + replicaID + " crashed. SocketException");
                                            }
                                            catch (DadStormTimeOutException e)
                                            {
                                                Console.WriteLine("I think that Operator " + replicaID + " crashed. TimeOutException");
                                            }

                                            catch (Exception e)
                                            {
                                                Console.WriteLine("I think that Operator " + replicaID + " crashed. General Exception");
                                            }
                                        }
                                    }
                                    foreach (string savedReplicaID in savedReplicaIDs)
                                    {
                                        string failedOperator = savedReplicaID.Split('-')[0];
                                        dictionaryPrimaryReplica[failedOperator]++;
                                        Dictionary<string, int> dictNumberReplicas = getNumberOfReplicas();
                                        int numberOfReplicas = dictNumberReplicas[failedOperator];
                                        if (dictionaryPrimaryReplica[failedOperator] >= numberOfReplicas)
                                        {
                                            dictionaryPrimaryReplica[failedOperator] = 0;
                                        }
                                    }
                                    if (_operator.semantics.Equals("at-most-once") || 
                                        _operator.semantics.Equals("at-least-once") && (receivedAnswer))
                                    {
                                        lock (_operator.outputTuples)
                                        {
                                            _operator.outputTuples.RemoveAt(0);
                                        }
                                    }
                                    receivedAnswer = false;
                                }
                                break;

                            case "random":
                                bool receivedAnswerRandom = true;
                                if (_operator.sendAddresses.Count != 0)
                                {
                                    List<string> savedReplicaIDs = new List<string>();
                                    foreach (string operatorID in aliveReplicas.Keys)
                                    {
                                        int limit = aliveReplicas[operatorID].Count;
                                        Random random = new Random();
                                        int rand = random.Next(0, limit);
                                        int id = aliveReplicas[operatorID][rand];
                                        string replicaID = operatorID + "-" + id;
                                        string sendAddress = _operator.sendAddresses[replicaID];
                                        try
                                        {
                                            operatorServices = (OperatorServices)Activator.GetObject(
                                                                typeof(OperatorServices),
                                                                sendAddress);                                            
                                            Console.WriteLine("Sending to " + sendAddress + " the following tuple -> " + constructTuple(outputTuple));
                                            var task = Task.Run(() => operatorServices.exchangeTuples(outputTuple));
                                            if (task.Wait(TimeSpan.FromSeconds(TIMEOUT)))
                                            {
                                                contador++; // SAIR
                                                Console.WriteLine("Received Answer " + contador);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Did not received answer");
                                                savedReplicaIDs.Add(replicaID);
                                                receivedAnswerRandom = false;
                                                throw new DadStormTimeOutException();
                                            }
                                            if (_operator.loggingLevel.Equals("full"))
                                            {
                                                new Thread(() => puppetMasterServices.addMessageToLog(constructMsgToLog(outputTuple))).Start();
                                            }
                                        }
                                        catch (System.Net.Sockets.SocketException e) //Operator crashou
                                        {
                                            Console.WriteLine("I think that Operator " + replicaID + " crashed. SocketException");
                                        }
                                        catch (DadStormTimeOutException e)
                                        {
                                            Console.WriteLine("I think that Operator " + replicaID + " crashed. TimeOutException");
                                        }

                                        catch (Exception e)
                                        {
                                            Console.WriteLine("I think that Operator " + replicaID + " crashed. General Exception");
                                        }
                                    }
                                    foreach (string savedReplicaID in savedReplicaIDs)
                                    {
                                        string failedOperator = savedReplicaID.Split('-')[0];
                                        int failedReplicaID = Int32.Parse(savedReplicaID.Split('-')[1]);
                                        aliveReplicas[failedOperator].Remove(failedReplicaID);
                                    }

                                    if (_operator.semantics.Equals("at-most-once") ||
                                        _operator.semantics.Equals("at-least-once") && (receivedAnswerRandom))
                                    {
                                        lock (_operator.outputTuples)
                                        {
                                            _operator.outputTuples.RemoveAt(0);
                                        }
                                    }
                                    receivedAnswerRandom = false;
                                }
                                break;

                            case "hashing":
                                bool receivedAnswerHashing = true;
                                if (_operator.sendAddresses.Count != 0)
                                {
                                    List<string> savedReplicaIDs = new List<string>();
                                    foreach (string operatorID in aliveReplicas.Keys)
                                    {
                                        int limit = aliveReplicas[operatorID].Count;
                                        int positionReplica = getHashValue(outputTuple, limit, fieldNumberHashing);
                                        int id = aliveReplicas[operatorID][positionReplica];
                                        string replicaID = operatorID + "-" + id;
                                        string sendAddress = _operator.sendAddresses[replicaID];
                                        try
                                        {
                                            operatorServices = (OperatorServices)Activator.GetObject(
                                                                typeof(OperatorServices),
                                                                sendAddress);
                                            Console.WriteLine("Sending to " + sendAddress + " the following tuple -> " + constructTuple(outputTuple));
                                            var task = Task.Run(() => operatorServices.exchangeTuples(outputTuple));
                                            if (task.Wait(TimeSpan.FromSeconds(TIMEOUT)))
                                            {
                                                contador++; // SAIR
                                                Console.WriteLine("Received Answer " + contador);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Did not received answer");
                                                savedReplicaIDs.Add(replicaID);
                                                receivedAnswerRandom = false;
                                                throw new DadStormTimeOutException();
                                            }
                                            if (_operator.loggingLevel.Equals("full"))
                                            {
                                                new Thread(() => puppetMasterServices.addMessageToLog(constructMsgToLog(outputTuple))).Start();
                                            }
                                        }
                                        catch (System.Net.Sockets.SocketException e) //Operator crashou
                                        {
                                            Console.WriteLine("I think that Operator " + replicaID + " crashed. SocketException");
                                        }
                                        catch (DadStormTimeOutException e)
                                        {
                                            Console.WriteLine("I think that Operator " + replicaID + " crashed. TimeOutException");
                                        }

                                        catch (Exception e)
                                        {
                                            Console.WriteLine("I think that Operator " + replicaID + " crashed. General Exception");
                                        }
                                    }
                                    foreach (string savedReplicaID in savedReplicaIDs)
                                    {
                                        string failedOperator = savedReplicaID.Split('-')[0];
                                        int failedReplicaID = Int32.Parse(savedReplicaID.Split('-')[1]);
                                        aliveReplicas[failedOperator].Remove(failedReplicaID);
                                    }

                                    if (_operator.semantics.Equals("at-most-once") ||
                                        _operator.semantics.Equals("at-least-once") && (receivedAnswerHashing))
                                    {
                                        lock (_operator.outputTuples)
                                        {
                                            _operator.outputTuples.RemoveAt(0);
                                        }
                                    }
                                    receivedAnswerHashing = false;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
        }

        private Dictionary<string, int> getNumberOfReplicas()
        {
            Dictionary<string, int> numberOfReplicas = new Dictionary<string, int>();
            foreach (string replicaID in _operator.sendAddresses.Keys)
            {
                string operatorID = replicaID.Split('-')[0];
                if (numberOfReplicas.ContainsKey(operatorID))
                {
                    numberOfReplicas[operatorID] += 1;
                }
                else
                {
                    numberOfReplicas.Add(operatorID, 1);
                }
            }
            return numberOfReplicas;
        }

        private void initAliveReplicas()
        {
            foreach (string replicaID in _operator.sendAddresses.Keys)
            {
                Console.WriteLine("replica ID = " + replicaID);
                string operatorID = replicaID.Split('-')[0];
                int rep = Int32.Parse(replicaID.Split('-')[1]);
                if (aliveReplicas.ContainsKey(operatorID))
                {
                    aliveReplicas[operatorID].Add(rep);
                }
                else
                {
                    List<int> reps = new List<int>();
                    reps.Add(rep);
                    aliveReplicas.Add(operatorID, reps);
                }
            }
        }

        private void initPrimaryReplicasDict()
        {
            foreach (string replicaID in _operator.sendAddresses.Keys)
            {
                string operatorID = replicaID.Split('-')[0];
                if (!dictionaryPrimaryReplica.ContainsKey(operatorID))
                {
                    dictionaryPrimaryReplica.Add(operatorID, 0);
                }                
            }
        }

        private int getHashValue(string[] tuple, int limit, int fieldNumber)
        {
            string strToHash = tuple[fieldNumber - 1];
            int hashValue = strToHash.GetHashCode() % limit;
            return Math.Abs(hashValue);
        }

        public string constructMsgToLog(string[] tupleArray)
        {
            string message = "tuple " + _operator.myAddress + ", ";
            string tuple = constructTuple(tupleArray);
            return message + tuple;
        }

        public string constructTuple(string[] tupleArray)
        {
            string tuple = "";
            foreach (string s in tupleArray)
            {
                tuple += s + ", ";
            }
            tuple = tuple.Substring(0, tuple.Length - 2);
            return tuple;
        }

        public void exchangeTuples(string[] tuple)
        {
            Console.WriteLine("Received the following tuple -> " + constructTuple(tuple));
            lock(_operator.inputTuples){
                _operator.inputTuples.Add(tuple);
            }
            Console.WriteLine("Before IF Freezing Condition");
            if (_operator.freeze)
            {
                _operator.eventPreventReturn.WaitOne();
            }
            Console.WriteLine("I'm going to return a value bitchees");
        }

        public void printStatus()
        {
            Console.WriteLine("PuppetMaster asked for status");
            Console.WriteLine("My full ID = " + _operator.id + "-" + _operator.repID);
            Console.WriteLine("My Address = " + _operator.myAddress);
            Console.WriteLine("My current Status = " + _operator.status);
            int cont = 0;
            if (_operator.sendAddresses.Count > 0)
                Console.WriteLine("I'm sending to the following addresses:");
            foreach (string address in _operator.sendAddresses.Values)
            {
                Console.WriteLine(address);
                cont++;
            }
            cont = 0;
            if (_operator.previousAddresses.Count > 0)
                Console.WriteLine("I'm receiving from the following addresses:");
            foreach (string address in _operator.previousAddresses)
            {
                Console.WriteLine(address);
                cont++;
            }
        }

        public void freezeOperator()
        {
            _operator.status = "Freezing";
            _operator.freeze = true;
        }

        public void unfreezeOperator()
        {
            Console.WriteLine("Unfreezing...");
            _operator.status = "Running";
            _operator.freeze = false;
            _operator.eventExecute.Set();
            _operator.eventSendTuples.Set();
            _operator.eventPreventReturn.Set();
        }

        public void crashOperator()
        {
            Console.WriteLine("Crashing");
            try
            {
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ioe)
            {
                Debug.WriteLine("exp = " + ioe.Message);
            }
            
        }

        public void intervalOperator(int time)
        {
            _operator.sleepingTime = time;
            _operator.sleepingMode = true;
        }

        public void setSendAddresses(string operatorID, string sendAddress)
        {
            lock (_operator.sendAddresses)
            {
                _operator.sendAddresses.Add(operatorID, sendAddress);
            }
        }    
    }
}