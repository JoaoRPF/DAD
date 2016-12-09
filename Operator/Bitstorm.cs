using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    public class Bitstorm
    {
        public Dictionary<string, List<ForwardTup>> forwardTups = new Dictionary<string, List<ForwardTup>>();

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
    }
}
