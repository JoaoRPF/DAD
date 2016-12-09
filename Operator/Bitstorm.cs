using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    public class Bitstorm
    {
        public Dictionary<string, List<ForwardTup>> forwardTups = new Dictionary<string, List<ForwardTup>>(); //key is in the format "OPx-y"

        public void addForwardTup(ForwardTup forwardTup, string sentTo)
        {
            lock (forwardTups) {
                if (!forwardTups.ContainsKey(sentTo))
                {
                    forwardTups.Add(sentTo, new List<ForwardTup>());
                }
                forwardTups[sentTo].Add(forwardTup);
            }
        }

        public List<ForwardTup> getAllTups(string sentTo)
        {
            lock (forwardTups)
            {
                return forwardTups[sentTo];
            }
        }

        public List<ForwardTup> getAllOpenTups(string sentTo)
        {
            List<ForwardTup> openTups = new List<ForwardTup>();
            lock (forwardTups)
            {
                if (!forwardTups.ContainsKey(sentTo))
                {
                    forwardTups.Add(sentTo, new List<ForwardTup>());
                }
                List<ForwardTup> allTups = forwardTups[sentTo];
                foreach (ForwardTup tup in allTups)
                {
                    if (!tup.closed)
                    {
                        openTups.Add(tup);
                    }
                }
            }
            return openTups;
        }

        public void closeAllTups(string replicaID)
        {
            ForwardTup tup;
            lock (forwardTups)
            {
                if (forwardTups.ContainsKey(replicaID))
                {
                    for (int i = 0; i < forwardTups[replicaID].Count; i++)
                    {
                        tup = forwardTups[replicaID][i];
                        ForwardTup tupClone = tup.Clone();
                        tupClone.closed = true;
                        forwardTups[replicaID][i] = tupClone;
                    }
                }
            }
        }

        public bool isTupleClosed(string whoAsked, int tupleToCheck) //whoAsked is the replica who asked if the tuple is closed. Is in the format "OPx-y"
        {
            ForwardTup tup;
            lock (forwardTups)
            {
                for (int i = 0; i < forwardTups[whoAsked].Count; i++)
                {
                    tup = forwardTups[whoAsked][i];
                    if(whoAsked.Equals("OP2-1"))
                        Console.WriteLine("FORWARD TUP: ID - " + tup.tupleID + "CONTEUDO- " + tup.tup + "CLOSED - " + tup.closed);
                    if (tup.tupleID == tupleToCheck)
                    {
                        if (tup.closed)
                        {
                            return true;
                        }
                        else
                        {
                            ForwardTup tupClone = tup.Clone();
                            tupClone.closed = true;
                            forwardTups[whoAsked][i] = tupClone;
                            return false;
                        }
                    }
                }
            }
            return false;
        }
    }
}
