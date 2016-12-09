using System;
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
        private int PING_TIMEOUT = 5; //10 seconds for remote call timeout

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
        public string[] myReplicaAddresses;

        public Dictionary<string, string> routingTable = new Dictionary<string, string>(); //Routing Policy to Send Addresses 

        public TcpChannel channel;
        public static Operator _operator;
        public static OperatorServices operatorServices;
        public static PuppetMasterServices puppetMasterServices;
        public List<ForwardTup> inputTuples = new List<ForwardTup>();
        public List<ForwardTup> outputTuples = new List<ForwardTup>();

        public Thread executeThread;
        public Thread sendTuplesThread;

        public AutoResetEvent eventExecute = new AutoResetEvent(false);
        public AutoResetEvent eventSendTuples = new AutoResetEvent(false);
        public AutoResetEvent eventPreventReturn = new AutoResetEvent(false);
        public bool freeze = false;
        public int sleepingTime = 0;
        public bool sleepingMode = false;

        public Dictionary<string, int> dictionaryPrimaryReplica = new Dictionary<string, int>();

        public Dictionary<string, bool[]> aliveReplicas = new Dictionary<string, bool[]>();

        public Bitstorm bitStorm = new Bitstorm();

        public int contador = 0;
        private Thread pingThread;
        public int masterID = 0;
        public int[] oldRepPings;
        public int[] newRepPings;
        public bool[] updatedRep;
        private int MAX_REPLICAS = 100;

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
            _operator.myReplicaAddresses = new string[_operator.repFact];
            _operator.oldRepPings = new int[_operator.repFact];
            _operator.newRepPings = new int[_operator.repFact];
            _operator.updatedRep = new bool[_operator.repFact];
            _operator.type = args[6];
            _operator.routing = args[7];
            _operator.loggingLevel = args[8];
            _operator.semantics = args[9];
            if (_operator.type.Equals("UNIQ"))
            {
                _operator.fieldNumber = Int32.Parse(args[10]);
                if (args.Length == 12 && args[11].Contains("$"))
                    _operator.previousAddresses = previousAddressesToList(args[11]);
                if (args.Length == 12 && args[11].Contains("&"))
                    _operator.myReplicaAddresses = myReplicaAddressesToList(args[11]);
                if (args.Length == 13)
                {
                    _operator.previousAddresses = previousAddressesToList(args[11]);
                    _operator.myReplicaAddresses = myReplicaAddressesToList(args[12]);
                }
            }
            else if (_operator.type.Equals("FILTER"))
            {
                _operator.fieldNumber = Int32.Parse(args[10]);
                _operator.condition = args[11];
                _operator.conditionValue = args[12];
                if (args.Length == 14 && args[13].Contains("$"))
                    _operator.previousAddresses = previousAddressesToList(args[13]);
                if (args.Length == 14 && args[13].Contains("&"))
                    _operator.myReplicaAddresses = myReplicaAddressesToList(args[13]);
                if (args.Length == 15)
                {
                    _operator.previousAddresses = previousAddressesToList(args[13]);
                    _operator.myReplicaAddresses = myReplicaAddressesToList(args[14]);
                }
            }
            else if (_operator.type.Equals("DUP") || _operator.type.Equals("COUNT"))
            {
                if (args.Length == 12 && args[11].Contains("$"))
                    _operator.previousAddresses = previousAddressesToList(args[11]);
                if (args.Length == 12 && args[11].Contains("&"))
                    _operator.myReplicaAddresses = myReplicaAddressesToList(args[11]);
                if (args.Length == 13)
                {
                    _operator.previousAddresses = previousAddressesToList(args[11]);
                    _operator.myReplicaAddresses = myReplicaAddressesToList(args[12]);
                }
            }
            else if (_operator.type.Equals("CUSTOM"))
            {
                _operator.dllCustom = args[11];
                _operator.classCustom = args[12];
                _operator.methodCustom = args[13];
                if (args.Length == 15 && args[14].Contains("$"))
                    _operator.previousAddresses = previousAddressesToList(args[14]);
                if (args.Length == 15 && args[14].Contains("&"))
                    _operator.myReplicaAddresses = myReplicaAddressesToList(args[14]);
                if (args.Length == 16)
                {
                    _operator.previousAddresses = previousAddressesToList(args[14]);
                    _operator.myReplicaAddresses = myReplicaAddressesToList(args[15]);
                }
            }

            for (int i = 0; i < _operator.repFact; i++)
            {
                string replicaID = _operator.id + "-" + i;
                _operator.addToAliveReplicas(replicaID, _operator.repFact);
                _operator.oldRepPings[i] = 0;
                _operator.newRepPings[i] = 0;
                _operator.updatedRep[i] = false;
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

            System.Console.WriteLine("Created Operator " + _operator.id + "-" + _operator.repID);
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
                    operatorServices.setSendAddresses(replica, _operator.myAddress, _operator.routing, _operator.repFact);
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

        private static string[] myReplicaAddressesToList(string addressesToSend)
        {
            string[] listAddresses = new string[_operator.repFact];
            int i = 0;
            foreach (string address in addressesToSend.Split('&'))
            {
                if (!address.Equals("NULL"))
                {
                    listAddresses[i] = address;
                }
                else
                {
                    listAddresses[i] = null;
                }
                i++;
            }
            return listAddresses;
        }

        private static int getPort(string address)
        {
            int port = Int32.Parse(address.Split(':')[2].Split('/')[0]);
            return port;
        }

        public void readFileToInputBuffer(string file)
        {
            string[] lines = File.ReadAllLines(file);
            Random randomFile = new Random(123456789); //seed value, equal to all replicas. Could be any value
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
                        int workReplica = -1;
                        int limit = aliveReplicas[id].Length;
                        int fieldNumberHashing = -1;
                        string routingCopy = routing;
                        if (routing.Contains('('))
                        {
                            fieldNumberHashing = Int32.Parse(routing.Split('(')[1].Split(')')[0]);
                            routingCopy = "hashing";
                        }
                        switch (routingCopy)
                        {
                            case "primary":
                                workReplica = _operator.getLowerReplica(id);
                                break;
                            case "random":
                                workReplica = randomFile.Next(0, limit);
                                workReplica = _operator.getCorrectAliveID(id, workReplica, limit);
                                break;
                            case "hashing":
                                ForwardTup tup = new ForwardTup(fields);
                                workReplica = _operator.getHashValue(tup, limit, fieldNumberHashing, id);
                                break;
                        }
                        if (workReplica == repID)
                        {
                            lock (_operator.inputTuples) {
                                ForwardTup tup = new ForwardTup(fields);
                                _operator.inputTuples.Add(tup);
                            }
                        }
                    }
                }
            }
        }

        private int getLowerReplica(string operatorID)
        {
            int lowest = 0;
            foreach (bool alive in aliveReplicas[operatorID])
            {
                if (alive)
                {
                    return lowest;
                }
                lowest++;
            }
            return 0;
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
            /*Thread.Sleep(5000); //Just to guarantee that the initialization is over. OP2 has already given its replica addresses to OP1 so it can have those addresses
            _operator.initAliveReplicas(); */

            _operator.status = "Running";
            _operator.print();

            _operator.executeThread = new Thread(_operator.execute);
            _operator.executeThread.Start();

            _operator.sendTuplesThread = new Thread(_operator.sendTuples);
            _operator.sendTuplesThread.Start();

            _operator.pingThread = new Thread(_operator.stormTolerance);
            _operator.pingThread.Start();

            foreach (string file in _operator.inputFile)
            {
                _operator.readFileToInputBuffer(file);
            }
        }

        private void stormTolerance()
        {
            Thread.Sleep(3000);
            while (true)
            {
                if (_operator.freeze || semantics.Equals("at-most-once"))
                {
                    continue;
                }
                Console.WriteLine("storm Tolerance is alive ! ");
                if (masterID == repID)
                {
                    Thread.Sleep(3000);
                    aliveRepCheck();
                } else
                {
                    Thread.Sleep(1000);
                    pingMaster();
                }
            }
        }

        private void pingMaster()
        {
                string masterAddress = myReplicaAddresses[masterID];

                operatorServices = (OperatorServices)Activator.GetObject(
                                    typeof(OperatorServices),
                                    masterAddress);

            Console.WriteLine("pinging master ---> " + masterAddress);

                var task = Task.Run(() => operatorServices.updateRepPing(repID));

                if (!task.Wait(TimeSpan.FromSeconds(PING_TIMEOUT)))
                {
                    masterID++;
                    if (masterID == repFact) masterID = 0;
                }
        }

        private void aliveRepCheck()
        {
            for (int i = 0; i < oldRepPings.Length; i++)
            {
                if (i == repID) continue;

                if (oldRepPings[i] == newRepPings[i])
                {
                    operatorServices = (OperatorServices)Activator.GetObject(
                                        typeof(OperatorServices),
                                        myReplicaAddresses[i]);
                    int result = -1;
                    var task = Task.Run(() => result = operatorServices.ping(repID));
                    if (!task.Wait(TimeSpan.FromSeconds(PING_TIMEOUT)))
                    {
                        if (_operator.updatedRep[i])
                        {
                            Console.WriteLine("NAO VOU PEDIR NADA ! da replica " + i);
                        }
                        else
                        {
                            Console.WriteLine("VOU PEDIR OS TUPLOS DA REPLICA ---> " + i);
                            foreach (string upStreamAddress in previousAddresses)
                            {
                                operatorServices = (OperatorServices)Activator.GetObject(
                                            typeof(OperatorServices),
                                            upStreamAddress);

                                Console.WriteLine("ISTO ESTORIA ---> replica: " + id + "-" + i + " upStreamAddress: " + upStreamAddress);

                                List<ForwardTup> tuples = operatorServices.getDeadReplicaTuples(id + "-" + i);
                                //List<ForwardTup> tuples = new List<ForwardTup>();
                                foreach (ForwardTup tup in tuples)
                                {
                                    lock (inputTuples)
                                    {
                                        inputTuples.Add(tup);
                                    }
                                }

                                _operator.updatedRep[i] = true;
                            }
                        }
                    }
                    else
                    {
                        if(result < masterID) masterID = result;
                    }
                } else
                {
                        oldRepPings[i] = newRepPings[i];
                }
            }
        }

        public void sendTuples()
        {
            Console.WriteLine("Send-Tuples Thread started");

            Thread.Sleep(5000);

            while (true)
            {
                if (_operator.freeze)
                {
                    _operator.eventSendTuples.WaitOne();
                }
                if (_operator.outputTuples.Count != 0)
                {
                    ForwardTup outputTuple;
                    lock (_operator.outputTuples)
                    {
                        outputTuple = (ForwardTup)_operator.outputTuples[0].Clone();
                    }

                    lock (_operator.aliveReplicas) //antes era lock send addresses
                    {
                        lock (_operator.routingTable)
                        {
                            bool receivedAnswer = true;
                            if (_operator.sendAddresses.Count != 0)
                            {
                                List<string> savedReplicaIDs = new List<string>();
                                foreach (string operatorID in aliveReplicas.Keys)
                                {
                                    if (!operatorID.Equals(_operator.id))
                                    {
                                        int limit = aliveReplicas[operatorID].Length;
                                        int workReplica = -1;
                                        string routingToThisReplica = routingTable[operatorID];
                                        int fieldNumberHashing = -1;
                                        if (routingToThisReplica.Contains('('))
                                        {
                                            fieldNumberHashing = Int32.Parse(routingToThisReplica.Split('(')[1].Split(')')[0]);
                                            routingToThisReplica = "hashing";
                                        }
                                        switch (routingToThisReplica)
                                        {
                                            case "primary":
                                                workReplica = _operator.getLowerReplica(operatorID);
                                                break;
                                            case "random":
                                                Random random = new Random();
                                                workReplica = random.Next(0, limit);
                                                workReplica = _operator.getCorrectAliveID(operatorID, workReplica, limit);
                                                break;
                                            case "hashing":
                                                workReplica = _operator.getHashValue(outputTuple, limit, fieldNumberHashing, operatorID);
                                                break;
                                        }
                                        string replicaID = operatorID + "-" + workReplica;
                                        string sendAddress = _operator.sendAddresses[replicaID];
                                        try
                                        {
                                            operatorServices = (OperatorServices)Activator.GetObject(
                                                                typeof(OperatorServices),
                                                                sendAddress);
                                            Console.WriteLine("Sending to " + sendAddress + " the following tuple -> " + constructTuple(outputTuple.tup));

                                            //remote call para fechar o tuplo !
                                            //a remote call deve 'falhar' se tiver fechado, e esse tuplo é descartado
                                            //em vez de ser enviado.
                                            //quando um OPx pede tuplos de uma replica que falhou, o
                                            //operador que manda deve fechar esses tuplos, e abrir o mesmo tuplo
                                            //com a key do OPx a quem re-enviou

                                            var task = Task.Run(() => operatorServices.exchangeTuples(outputTuple));
                                            if (task.Wait(TimeSpan.FromSeconds(TIMEOUT)))
                                            {
                                                contador++; // Just to make an aux print in the Console
                                                Console.WriteLine("Received Answer " + contador);
                                                _operator.bitStorm.addForwardTup(outputTuple, replicaID);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Did not received answer");
                                                savedReplicaIDs.Add(replicaID);
                                                receivedAnswer = false;
                                                throw new DadStormTimeOutException();
                                            }
                                            if (_operator.loggingLevel.Equals("full"))
                                            {
                                                new Thread(() => puppetMasterServices.addMessageToLog(constructMsgToLog(outputTuple.tup))).Start();
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
                                    int failedReplicaID = Int32.Parse(savedReplicaID.Split('-')[1]);
                                    _operator.aliveReplicas[failedOperator][failedReplicaID] = false;
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

        /*private void initAliveReplicas()
        {
            foreach (string replicaID in _operator.sendAddresses.Keys)
            {
                Console.WriteLine("Operator ID and Replica that i can send to = " + replicaID);
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
        }*/

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

        private int getHashValue(ForwardTup tuple, int limit, int fieldNumber, string operatorID)
        {
            string strToHash = tuple.tup[fieldNumber - 1];
            int hashValue = strToHash.GetHashCode() % limit;
            //Console.WriteLine("HASH VALUE = " + hashValue);
            int replicaID = Math.Abs(hashValue);
            replicaID = getCorrectAliveID(operatorID, replicaID, limit);
            return replicaID;
        }

        private int getCorrectAliveID(string operatorID, int initialReplica, int limit)
        {
            Console.WriteLine("OPERATOR ID -->> " + operatorID);
            //Console.WriteLine("GETTING CORRECT ALIVE ID !!!! I HAVE ---> " + initialReplica);
            int countIterations = 0;
            int replicaID = initialReplica;
            //Console.WriteLine("initial replica = " + replicaID);
            while (!aliveReplicas[operatorID][replicaID])
            {
                //Console.WriteLine("EU NUNCA ENTRO AQUIIIIIIIIIIIIIIIII :)");
                replicaID++;
                if (replicaID == limit)
                {
                    replicaID = 0;
                }
                countIterations++;
                if (countIterations == limit * 3)
                {
                    break;
                }
            }
            //Console.WriteLine("!!! IN THE END I GOT ---> " + replicaID);
            return replicaID;
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

        public void exchangeTuples(ForwardTup tuple)
        {
            Console.WriteLine("Received the following tuple -> " + constructTuple(tuple.tup));
            lock(_operator.inputTuples){
                _operator.inputTuples.Add(tuple);
            }
            //Console.WriteLine("Before IF Freezing Condition");
            if (_operator.freeze)
            {
                _operator.eventPreventReturn.WaitOne();
            }
            //Console.WriteLine("I'm going to return a value bitchees");
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

        public void setSendAddresses(string operatorID, string sendAddress, string routing, int repFact)
        {
            lock (_operator.sendAddresses)
            {
                _operator.sendAddresses.Add(operatorID, sendAddress);
            }
            lock (_operator.aliveReplicas)
            {
                    lock (_operator.routingTable)
                    {
                        _operator.addToAliveReplicas_lockLess(operatorID, repFact);
                        _operator.addToRoutingTable(operatorID, routing);
                    }        
            }
        }
        
        private void addToAliveReplicas(string replicaID, int repFact)
        {
            string operatorID = replicaID.Split('-')[0];
            int rep = Int32.Parse(replicaID.Split('-')[1]);
            lock (aliveReplicas)
            {
                if (aliveReplicas.ContainsKey(operatorID))
                {
                    aliveReplicas[operatorID][rep] = true;
                }

                else
                {
                    bool[] reps = new bool[repFact];
                    reps[rep] = true;
                    aliveReplicas.Add(operatorID, reps);
                }
            }
        }


        private void addToAliveReplicas_lockLess(string replicaID, int repFact)
        {
            string operatorID = replicaID.Split('-')[0];
            int rep = Int32.Parse(replicaID.Split('-')[1]);
                if (aliveReplicas.ContainsKey(operatorID))
                {
                    aliveReplicas[operatorID][rep] = true;
                }

                else
                {
                    bool[] reps = new bool[repFact];
                    reps[rep] = true;
                    aliveReplicas.Add(operatorID, reps);
                }
        }



        private void addToRoutingTable(string replicaID, string routing)
        {
            string operatorID = replicaID.Split('-')[0];
            if (!routingTable.ContainsKey(operatorID))
            {
                routingTable.Add(operatorID, routing);
            }
        }

        public int ping(int m)
        {
            if (_operator.freeze)
            {
                _operator.eventPreventReturn.WaitOne();
            }

            if (m < _operator.masterID)
            {
                _operator.masterID = m;
                return MAX_REPLICAS;
            }
            else
            {
                return masterID;
            }
        }

        public void updateRepPing(int r)
        {
            if (_operator.freeze)
            {
                _operator.eventPreventReturn.WaitOne();
            }

            Console.WriteLine("A REPLICA " + r + "ACTUALIZOU O NEW_V E A updatedRep !" );

            _operator.updatedRep[r] = false;
            _operator.newRepPings[r]++;
        }

        public List<ForwardTup> getDeadReplicaTuples(string replicaID)
        {
            string[] s = replicaID.Split('-');
            string op = s[0];
            int rep = Int32.Parse(s[1]);
            lock (aliveReplicas)
            {
                _operator.aliveReplicas[op][rep] = false;
            }
            return _operator.bitStorm.getAllOpenTups(replicaID);
        }
    }
}